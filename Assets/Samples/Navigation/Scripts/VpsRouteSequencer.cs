using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WASVPS;

[DisallowMultipleComponent]
public class VpsRouteSequencer : MonoBehaviour
{
    [Serializable]
    public class RoutePoint
    {
        public string Id = "Point";
        public string Title = "Point";
        public string WorkingHours = "10:00-22:00";
        public string Address = "Aviapark Mall, Khodynsky Blvd 4, Moscow";
        public string RoomAddress = "Floor 1, sector A";
        [TextArea(2, 5)] public string Description;
        public Sprite Icon;
        public Sprite Image;
        public Vector3 Position;
        public Transform Point;
        [Min(0f)] public float ArrivalDistance = 2f;
    }

    private const string LocalizingMessage = "Rotate your phone and pop all the balloons around you to start!";
    private const string VpsFallbackMessage = "VPS is unavailable. Navigation will continue without VPS.";
    private const string RouteCompleteMessage = "Destination reached.";

    [SerializeField] private VPSLocalisationService _vpsLocalisationService;
    [SerializeField] private TargetManager _targetManager;
    [SerializeField] private NavigationPointsCatalog _pointsCatalog;
    [SerializeField] private List<RoutePoint> _routePoints = new();
    [SerializeField] private bool _syncRoutePointsFromCatalogOnEnable = true;
    [SerializeField] private bool _startOnEnable;
    [SerializeField] private int _defaultStartPointIndex;
    [SerializeField] private bool _waitForVpsReady = true;
    [SerializeField] private bool _buildRouteWithoutVps = true;
    [SerializeField] private float _vpsReadyTimeout = 6f;
    [Min(0f)]
    [SerializeField] private float _postLocalizationDelay = 3f;
    [Min(1)]
    [SerializeField] private int _requiredPositionUpdatesForLocalization = 1;
    [SerializeField] private bool _showPath = true;
    [SerializeField] private bool _stopVpsAfterLocalization = true;
    [SerializeField] private bool _stopVpsOnRouteComplete;
    [SerializeField] private float _switchDelay = 0.2f;
    [SerializeField] private float _distanceEventInterval = 0.2f;

    [Header("Legacy Info Panel")]
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private Text _infoTextView;
    [SerializeField] private bool _autoHideInfo;
    [SerializeField] private float _infoHideDelay = 2f;

    private int _currentIndex = -1;
    private int _pendingPointIndex = -1;
    private bool _routeActive;
    private bool _vpsReady;
    private bool _isSubscribed;
    private float _nextSwitchTime;
    private float _nextDistanceEventTime;
    private Coroutine _hideInfoRoutine;
    private Coroutine _vpsFallbackRoutine;
    private Coroutine _postLocalizationRoutine;
    private int _positionUpdatesSinceRouteStart;

    /// <summary>
    /// Invoked when a route point is selected and route drawing starts.
    /// </summary>
    public event Action<RoutePoint, int> RoutePointSelected;

    /// <summary>
    /// Invoked when the current route point is reached.
    /// </summary>
    public event Action<RoutePoint, int> RoutePointReached;

    /// <summary>
    /// Invoked when route state changes (true = active, false = inactive).
    /// </summary>
    public event Action<bool> RouteStateChanged;

    /// <summary>
    /// Invoked when VPS localization readiness changes.
    /// </summary>
    public event Action<bool> VpsLocalizationStateChanged;

    /// <summary>
    /// Invoked when remaining distance to target changes.
    /// </summary>
    public event Action<float> RemainingDistanceUpdated;

    /// <summary>
    /// Invoked when remaining time estimation to target changes.
    /// </summary>
    public event Action<float> RemainingTimeUpdated;

    /// <summary>
    /// Invoked when route status text should be updated in UI.
    /// </summary>
    public event Action<string> StatusMessageChanged;

    /// <summary>
    /// Invoked when navigation to the currently selected destination is complete.
    /// </summary>
    public event Action RouteCompleted;

    /// <summary>
    /// True when a route is currently active.
    /// </summary>
    public bool IsRouteActive => _routeActive;

    /// <summary>
    /// True when VPS is ready or route can continue without VPS.
    /// </summary>
    public bool IsVpsReady => _vpsReady;

    /// <summary>
    /// Current route point index or -1 when route is inactive.
    /// </summary>
    public int CurrentPointIndex => _currentIndex;

    private void OnEnable()
    {
        if (_syncRoutePointsFromCatalogOnEnable)
        {
            SyncRoutePointsFromCatalog();
        }

        SubscribeVpsEvents();

        if (_startOnEnable)
        {
            StartRoute();
        }
    }

    private void OnDisable()
    {
        UnsubscribeVpsEvents();
        StopRoute(stopVps: false);
    }

    private void Update()
    {
        if (!_routeActive || !_vpsReady || _targetManager == null || !TryGetPoint(_currentIndex, out var currentPoint) || !IsPointNavigable(currentPoint))
        {
            return;
        }

        var distance = GetDistanceToCurrentPoint(currentPoint);

        if (Time.time >= _nextDistanceEventTime)
        {
            RemainingDistanceUpdated?.Invoke(distance);
            RemainingTimeUpdated?.Invoke(_targetManager.GetTimeToTarget());
            _nextDistanceEventTime = Time.time + Mathf.Max(0.01f, _distanceEventInterval);
        }

        if (Time.time < _nextSwitchTime)
        {
            return;
        }

        var arrivalDistance = GetArrivalDistance(currentPoint);
        if (distance <= arrivalDistance)
        {
            OnPointReached(currentPoint);
            CompleteRoute();
        }
    }

    /// <summary>
    /// Starts route to the configured default point index.
    /// </summary>
    /// <returns>True when route start was accepted; otherwise false.</returns>
    public bool StartRoute()
    {
        return StartRouteToPoint(_defaultStartPointIndex);
    }

    /// <summary>
    /// Starts route to a specific point index.
    /// </summary>
    /// <param name="pointIndex">Index of destination point in route points list.</param>
    /// <returns>True when route start was accepted; otherwise false.</returns>
    public bool StartRouteToPoint(int pointIndex)
    {
        if (_targetManager == null)
        {
            Debug.LogWarning($"{nameof(VpsRouteSequencer)}: target manager is not assigned.");
            return false;
        }

        if (!TryGetPoint(pointIndex, out var point) || !IsPointNavigable(point))
        {
            Debug.LogWarning($"{nameof(VpsRouteSequencer)}: route point at index {pointIndex} is not valid.");
            return false;
        }

        SubscribeVpsEvents();

        _routeActive = true;
        _currentIndex = -1;
        _pendingPointIndex = pointIndex;
        _nextSwitchTime = 0f;
        _nextDistanceEventTime = 0f;
        _positionUpdatesSinceRouteStart = 0;

        RouteStateChanged?.Invoke(true);

        var hasActiveVpsService = _vpsLocalisationService != null && _vpsLocalisationService.isActiveAndEnabled;
        var shouldWaitForVps = _waitForVpsReady && hasActiveVpsService;

        _vpsReady = !shouldWaitForVps;
        VpsLocalizationStateChanged?.Invoke(_vpsReady);

        StopVpsFallbackTimer();
        StopPostLocalizationDelay();

        if (hasActiveVpsService)
        {
            _vpsLocalisationService.StartVPS();
        }

        if (!_vpsReady)
        {
            SetStatusMessage(LocalizingMessage);
            StartVpsFallbackTimer();
            return true;
        }

        if (_waitForVpsReady && !hasActiveVpsService)
        {
            SetStatusMessage(VpsFallbackMessage);
        }
        else
        {
            SetStatusMessage(string.Empty);
        }

        ActivatePendingPoint();
        return true;
    }

    /// <summary>
    /// Starts route to a specific point by point ID.
    /// </summary>
    /// <param name="pointId">Route point ID.</param>
    /// <returns>True when point exists and route start was accepted; otherwise false.</returns>
    public bool StartRouteToPoint(string pointId)
    {
        var index = GetPointIndex(pointId);
        if (index < 0)
        {
            Debug.LogWarning($"{nameof(VpsRouteSequencer)}: point ID '{pointId}' was not found.");
            return false;
        }

        return StartRouteToPoint(index);
    }

    /// <summary>
    /// Stops the active route and hides current path.
    /// </summary>
    /// <param name="stopVps">True to stop VPS service as part of route stop.</param>
    public void StopRoute(bool stopVps = true)
    {
        _routeActive = false;
        _currentIndex = -1;
        _pendingPointIndex = -1;
        _nextSwitchTime = 0f;
        _nextDistanceEventTime = 0f;
        _positionUpdatesSinceRouteStart = 0;

        _targetManager?.HidePath();
        StopVpsFallbackTimer();
        StopPostLocalizationDelay();
        SetStatusMessage(string.Empty);

        RouteStateChanged?.Invoke(false);

        if (stopVps)
        {
            _vpsLocalisationService?.StopVps();
        }
    }

    /// <summary>
    /// Returns readonly collection of configured route points.
    /// </summary>
    /// <returns>Configured route points list.</returns>
    public IReadOnlyList<RoutePoint> GetRoutePoints()
    {
        return _routePoints;
    }

    /// <summary>
    /// Rebuilds route points list from the configured points catalog.
    /// </summary>
    [ContextMenu("Sync Route Points From Catalog")]
    public void SyncRoutePointsFromCatalog()
    {
        if (_pointsCatalog == null || _pointsCatalog.Points == null)
        {
            return;
        }

        var synced = new List<RoutePoint>(_pointsCatalog.Points.Count);
        for (var i = 0; i < _pointsCatalog.Points.Count; i++)
        {
            var sourcePoint = _pointsCatalog.Points[i];
            if (sourcePoint == null || string.IsNullOrWhiteSpace(sourcePoint.Id))
            {
                continue;
            }

            var arrivalDistance = 2f;
            if (TryFindRoutePoint(sourcePoint.Id, out var existingPoint) && existingPoint != null && existingPoint.ArrivalDistance > 0f)
            {
                arrivalDistance = existingPoint.ArrivalDistance;
            }
            else if (_targetManager != null && _targetManager.DefaultArrivalDistance > 0f)
            {
                arrivalDistance = _targetManager.DefaultArrivalDistance;
            }

            synced.Add(new RoutePoint
            {
                Id = sourcePoint.Id,
                Title = sourcePoint.Title,
                WorkingHours = sourcePoint.WorkingHours,
                Address = sourcePoint.Address,
                RoomAddress = sourcePoint.RoomAddress,
                Description = sourcePoint.Description,
                Icon = sourcePoint.Icon,
                Image = sourcePoint.Image,
                Position = sourcePoint.Position,
                Point = null,
                ArrivalDistance = arrivalDistance
            });
        }

        _routePoints = synced;
    }

    /// <summary>
    /// Finds point index by route point ID.
    /// </summary>
    /// <param name="pointId">Route point ID.</param>
    /// <returns>Point index or -1 when not found.</returns>
    public int GetPointIndex(string pointId)
    {
        if (string.IsNullOrWhiteSpace(pointId) || _routePoints == null)
        {
            return -1;
        }

        for (var i = 0; i < _routePoints.Count; i++)
        {
            var point = _routePoints[i];
            if (point != null && string.Equals(point.Id, pointId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets route point by index.
    /// </summary>
    /// <param name="pointIndex">Point index in route list.</param>
    /// <param name="point">Resolved route point when found.</param>
    /// <returns>True when point index is valid; otherwise false.</returns>
    public bool TryGetPoint(int pointIndex, out RoutePoint point)
    {
        if (_routePoints != null && pointIndex >= 0 && pointIndex < _routePoints.Count)
        {
            point = _routePoints[pointIndex];
            return point != null;
        }

        point = null;
        return false;
    }

    /// <summary>
    /// Finds route point by point ID.
    /// </summary>
    /// <param name="pointId">Point ID.</param>
    /// <param name="point">Resolved point when found.</param>
    /// <returns>True when point was found; otherwise false.</returns>
    public bool TryFindRoutePoint(string pointId, out RoutePoint point)
    {
        point = null;
        if (string.IsNullOrWhiteSpace(pointId) || _routePoints == null)
        {
            return false;
        }

        for (var i = 0; i < _routePoints.Count; i++)
        {
            var candidate = _routePoints[i];
            if (candidate != null && string.Equals(candidate.Id, pointId, StringComparison.OrdinalIgnoreCase))
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves point world position from scene transform or point position.
    /// </summary>
    /// <param name="point">Route point data.</param>
    public static Vector3 GetPointWorldPosition(RoutePoint point)
    {
        if (point != null && point.Point != null)
        {
            return point.Point.position;
        }

        return point == null ? Vector3.zero : point.Position;
    }

    /// <summary>
    /// Returns remaining route distance to currently active point.
    /// </summary>
    /// <returns>Distance in meters or zero when route is inactive.</returns>
    public float GetRemainingDistance()
    {
        if (!_routeActive || _targetManager == null || !TryGetPoint(_currentIndex, out var point) || !IsPointNavigable(point))
        {
            return 0f;
        }

        return GetDistanceToCurrentPoint(point);
    }

    private void SubscribeVpsEvents()
    {
        if (_isSubscribed || _vpsLocalisationService == null)
        {
            return;
        }

        _vpsLocalisationService.OnPositionUpdated += OnVpsPositionUpdated;
        _isSubscribed = true;
    }

    private void UnsubscribeVpsEvents()
    {
        if (!_isSubscribed || _vpsLocalisationService == null)
        {
            return;
        }

        _vpsLocalisationService.OnPositionUpdated -= OnVpsPositionUpdated;
        _isSubscribed = false;
    }

    private void OnVpsPositionUpdated(LocationState location)
    {
        if (_vpsReady || !_routeActive || _pendingPointIndex < 0 || location == null || location.Localisation == null)
        {
            return;
        }

        _positionUpdatesSinceRouteStart++;
        if (_positionUpdatesSinceRouteStart < Mathf.Max(1, _requiredPositionUpdatesForLocalization))
        {
            return;
        }

        StopVpsFallbackTimer();
        StartPostLocalizationDelay();
    }

    private void ActivatePendingPoint()
    {
        if (!_routeActive || _pendingPointIndex < 0 || _targetManager == null)
        {
            return;
        }

        _currentIndex = _pendingPointIndex;
        _pendingPointIndex = -1;

        if (!TryGetPoint(_currentIndex, out var point) || !IsPointNavigable(point))
        {
            CompleteRoute();
            return;
        }

        var arrivalDistance = point.ArrivalDistance > 0f ? point.ArrivalDistance : _targetManager.DefaultArrivalDistance;
        _targetManager.ShowPath(GetPointWorldPosition(point), arrivalDistance, _showPath);
        _nextSwitchTime = Time.time + Mathf.Max(0f, _switchDelay);
        _nextDistanceEventTime = 0f;

        RoutePointSelected?.Invoke(point, _currentIndex);
    }

    private float GetDistanceToCurrentPoint(RoutePoint point)
    {
        var navDistance = _targetManager.GetDistanceToTarget();
        if (navDistance > 0f)
        {
            return navDistance;
        }

        return Vector3.Distance(_targetManager.GetStartPosition(), GetPointWorldPosition(point));
    }

    private float GetArrivalDistance(RoutePoint point)
    {
        if (point != null && point.ArrivalDistance > 0f)
        {
            return point.ArrivalDistance;
        }

        return _targetManager == null ? 0f : _targetManager.GetArrivalDistance();
    }

    private static bool IsPointNavigable(RoutePoint point)
    {
        return point != null &&
               (point.Point != null ||
                (!float.IsNaN(point.Position.x) && !float.IsNaN(point.Position.y) && !float.IsNaN(point.Position.z)));
    }

    private void StartVpsFallbackTimer()
    {
        StopVpsFallbackTimer();

        if (!_routeActive || _vpsReady || !_waitForVpsReady || !_buildRouteWithoutVps || _vpsReadyTimeout <= 0f)
        {
            return;
        }

        _vpsFallbackRoutine = StartCoroutine(VpsFallbackTimer());
    }

    private void StopVpsFallbackTimer()
    {
        if (_vpsFallbackRoutine == null)
        {
            return;
        }

        StopCoroutine(_vpsFallbackRoutine);
        _vpsFallbackRoutine = null;
    }

    private IEnumerator VpsFallbackTimer()
    {
        yield return new WaitForSeconds(_vpsReadyTimeout);

        if (!_routeActive || _vpsReady)
        {
            _vpsFallbackRoutine = null;
            yield break;
        }

        _vpsReady = true;
        VpsLocalizationStateChanged?.Invoke(true);
        SetStatusMessage(VpsFallbackMessage);
        Debug.LogWarning($"{nameof(VpsRouteSequencer)}: VPS was not ready in {_vpsReadyTimeout:F1}s. Navigation continues without VPS.");

        if (_pendingPointIndex >= 0)
        {
            ActivatePendingPoint();
        }

        _vpsFallbackRoutine = null;
    }

    private void StartPostLocalizationDelay()
    {
        if (_postLocalizationRoutine != null)
        {
            return;
        }

        if (_postLocalizationDelay <= 0f)
        {
            FinalizeLocalizationAndStartRoute();
            return;
        }

        _postLocalizationRoutine = StartCoroutine(PostLocalizationDelayRoutine());
    }

    private void StopPostLocalizationDelay()
    {
        if (_postLocalizationRoutine == null)
        {
            return;
        }

        StopCoroutine(_postLocalizationRoutine);
        _postLocalizationRoutine = null;
    }

    private IEnumerator PostLocalizationDelayRoutine()
    {
        yield return new WaitForSeconds(_postLocalizationDelay);
        _postLocalizationRoutine = null;

        if (!_routeActive || _vpsReady)
        {
            yield break;
        }

        FinalizeLocalizationAndStartRoute();
    }

    private void FinalizeLocalizationAndStartRoute()
    {
        _vpsReady = true;
        VpsLocalizationStateChanged?.Invoke(true);
        SetStatusMessage(string.Empty);

        if (_routeActive && _pendingPointIndex >= 0)
        {
            ActivatePendingPoint();
        }

        if (_stopVpsAfterLocalization)
        {
            _vpsLocalisationService?.StopVps();
        }
    }

    private void OnPointReached(RoutePoint point)
    {
        RoutePointReached?.Invoke(point, _currentIndex);
    }

    private void CompleteRoute()
    {
        _routeActive = false;
        _pendingPointIndex = -1;

        _targetManager?.HidePath();
        StopVpsFallbackTimer();
        StopPostLocalizationDelay();

        RouteStateChanged?.Invoke(false);
        RouteCompleted?.Invoke();

        SetStatusMessage(RouteCompleteMessage);

        if (_stopVpsOnRouteComplete)
        {
            _vpsLocalisationService?.StopVps();
        }
    }

    private void SetStatusMessage(string message)
    {
        StatusMessageChanged?.Invoke(message);

        if (string.IsNullOrWhiteSpace(message))
        {
            HideInfo();
            return;
        }

        ShowInfo(message);
    }

    private void ShowInfo(string message)
    {
        if (_infoTextView != null)
        {
            _infoTextView.text = message;
            _infoTextView.gameObject.SetActive(true);
        }

        if (_infoPanel != null)
        {
            _infoPanel.SetActive(true);
        }

        if (_infoTextView == null && _infoPanel == null)
        {
            Debug.Log($"{nameof(VpsRouteSequencer)}: {message}");
        }

        if (_hideInfoRoutine != null)
        {
            StopCoroutine(_hideInfoRoutine);
            _hideInfoRoutine = null;
        }

        if (_autoHideInfo && _infoHideDelay > 0f)
        {
            _hideInfoRoutine = StartCoroutine(HideInfoDelayed(_infoHideDelay));
        }
    }

    private IEnumerator HideInfoDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInfo();
    }

    private void HideInfo()
    {
        if (_hideInfoRoutine != null)
        {
            StopCoroutine(_hideInfoRoutine);
            _hideInfoRoutine = null;
        }

        if (_infoTextView != null)
        {
            _infoTextView.text = string.Empty;
            if (_infoPanel == null)
            {
                _infoTextView.gameObject.SetActive(false);
            }
        }

        if (_infoPanel != null)
        {
            _infoPanel.SetActive(false);
        }
    }
}
