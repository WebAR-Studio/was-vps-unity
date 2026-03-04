using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WASVPS;

[DisallowMultipleComponent]
public class SampleVpsRouteSequencer : MonoBehaviour
{
    [Serializable]
    public class RoutePoint
    {
        public string Id = "Point";
        public Transform Point;
        [Min(0f)] public float ArrivalDistance = 2f;
    }

    private const string LocalizingMessage = "\u041B\u043E\u043A\u0430\u043B\u0438\u0437\u0430\u0446\u0438\u044F...";
    private const string VpsFallbackMessage = "VPS \u043D\u0435\u0434\u043E\u0441\u0442\u0443\u043F\u0435\u043D. \u041C\u0430\u0440\u0448\u0440\u0443\u0442 \u0431\u0435\u0437 VPS.";
    private const string PointReachedTemplate = "\u0414\u043E\u0448\u043B\u0438 \u0434\u043E \u0442\u043E\u0447\u043A\u0438 {0}";
    private const string RouteCompleteMessage = "\u041C\u0430\u0440\u0448\u0440\u0443\u0442 \u043F\u0440\u043E\u0439\u0434\u0435\u043D!";

    [SerializeField] private VPSLocalisationService _vpsLocalisationService;
    [SerializeField] private SampleTargetManager _targetManager;
    [SerializeField] private List<RoutePoint> _routePoints = new();
    [SerializeField] private bool _startOnEnable = true;
    [SerializeField] private bool _waitForVpsReady = false;
    [SerializeField] private bool _buildRouteWithoutVps = true;
    [SerializeField] private float _vpsReadyTimeout = 6f;
    [SerializeField] private bool _showPath = true;
    [SerializeField] private bool _stopVpsOnRouteComplete = false;
    [SerializeField] private float _switchDelay = 0.2f;

    [Header("Info Panel")]
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private Text _infoTextView;
    [SerializeField] private bool _autoHideInfo = false;
    [SerializeField] private float _infoHideDelay = 2f;

    private int _currentIndex = -1;
    private bool _routeActive;
    private bool _vpsReady;
    private bool _isSubscribed;
    private float _nextSwitchTime;
    private Coroutine _hideInfoRoutine;
    private Coroutine _vpsFallbackRoutine;

    private void OnEnable()
    {
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
        HideInfo();
    }

    private void Update()
    {
        if (!_routeActive || !_vpsReady || _currentIndex < 0 || _currentIndex >= _routePoints.Count || _targetManager == null)
        {
            return;
        }

        if (Time.time < _nextSwitchTime)
        {
            return;
        }

        var point = _routePoints[_currentIndex];
        if (point == null || point.Point == null)
        {
            MoveToNextPoint();
            return;
        }

        var navDistance = _targetManager.GetDistanceToTarget();
        var worldDistance = Vector3.Distance(_targetManager.GetStartPosition(), point.Point.position);
        var distance = navDistance > 0f ? navDistance : worldDistance;
        var arrivalDistance = point.ArrivalDistance > 0f ? point.ArrivalDistance : _targetManager.GetArrivalDistance();

        if (distance <= arrivalDistance)
        {
            OnPointReached(point);
            MoveToNextPoint();
        }
    }

    public void StartRoute()
    {
        if (_targetManager == null || _routePoints == null || _routePoints.Count == 0)
        {
            Debug.LogWarning($"{nameof(SampleVpsRouteSequencer)}: target manager or route points are not assigned.");
            return;
        }

        SubscribeVpsEvents();

        _routeActive = true;
        _currentIndex = -1;
        _nextSwitchTime = 0f;
        var hasActiveVpsService = _vpsLocalisationService != null && _vpsLocalisationService.isActiveAndEnabled;
        _vpsReady = !_waitForVpsReady || !hasActiveVpsService;

        if (_waitForVpsReady)
        {
            ShowInfo(LocalizingMessage);
        }

        if (hasActiveVpsService)
        {
            _vpsLocalisationService.StartVPS();
        }
        else if (_waitForVpsReady)
        {
            ShowInfo(VpsFallbackMessage);
        }
        StartVpsFallbackTimer();

        if (_vpsReady)
        {
            MoveToNextPoint();
        }
    }

    public void StopRoute(bool stopVps = true)
    {
        _routeActive = false;
        _currentIndex = -1;
        _nextSwitchTime = 0f;
        _targetManager?.HidePath();
        StopVpsFallbackTimer();

        if (stopVps)
        {
            _vpsLocalisationService?.StopVps();
        }
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
        if (_vpsReady || location.Status != LocalisationStatus.VPS_READY)
        {
            return;
        }

        StopVpsFallbackTimer();
        _vpsReady = true;

        if (_routeActive && _currentIndex < 0)
        {
            MoveToNextPoint();
        }
    }

    private void MoveToNextPoint()
    {
        _currentIndex++;

        while (_currentIndex < _routePoints.Count)
        {
            var point = _routePoints[_currentIndex];
            if (point != null && point.Point != null)
            {
                var arrivalDistance = point.ArrivalDistance > 0f ? point.ArrivalDistance : _targetManager.DefaultArrivalDistance;
                _targetManager.ShowPath(point.Point.position, arrivalDistance, _showPath);
                _nextSwitchTime = Time.time + _switchDelay;
                return;
            }

            _currentIndex++;
        }

        CompleteRoute();
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
        ShowInfo(VpsFallbackMessage);
        Debug.LogWarning($"{nameof(SampleVpsRouteSequencer)}: VPS was not ready in {_vpsReadyTimeout:F1}s. Navigation continues without VPS.");

        if (_currentIndex < 0)
        {
            MoveToNextPoint();
        }

        _vpsFallbackRoutine = null;
    }

    private void CompleteRoute()
    {
        _routeActive = false;
        _targetManager?.HidePath();
        ShowInfo(RouteCompleteMessage);

        if (_stopVpsOnRouteComplete)
        {
            _vpsLocalisationService?.StopVps();
        }

        Debug.Log($"{nameof(SampleVpsRouteSequencer)}: route complete.");
    }

    private void OnPointReached(RoutePoint point)
    {
        var targetId = point == null || string.IsNullOrWhiteSpace(point.Id) ? (_currentIndex + 1).ToString() : point.Id;
        ShowInfo(string.Format(PointReachedTemplate, targetId));
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
            Debug.Log($"{nameof(SampleVpsRouteSequencer)}: {message}");
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
