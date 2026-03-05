using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class NavigationRouteUiController : MonoBehaviour
{
    private const string LocalizingMessage = "Rotate your phone and pop all the balloons around you to start!";

    [Header("Route")]
    [SerializeField] private VpsRouteSequencer _routeSequencer;
    [SerializeField] private bool _forceMenuOnEnable = true;

    [Header("Views")]
    [SerializeField] private GameObject _menuView;
    [SerializeField] private GameObject _localizingView;
    [SerializeField] private GameObject _tripSummaryView;
    [SerializeField] private GameObject _arrivalActionsView;
    [SerializeField] private GameObject _aboutView;
    [SerializeField] private Camera _loaderFxCamera;

    private NavMenuView _menuViewPresenter;
    private NavLocalizingView _localizingViewPresenter;
    private NavTripSummaryView _tripSummaryViewPresenter;
    private NavArrivalView _arrivalViewPresenter;
    private NavAboutView _aboutViewPresenter;

    private VpsRouteSequencer.RoutePoint _currentPoint;
    private int _currentPointIndex = -1;
    private bool _awaitingArrivalAction;
    private float _lastRemainingTime;
    private Coroutine _startRouteRoutine;

    private void OnEnable()
    {
        if (_routeSequencer == null)
        {
            _routeSequencer = FindObjectOfType<VpsRouteSequencer>();
        }

        ResolveViewReferences();
        ResolveViewPresenters();
        ResolveLoaderFxCamera();
        SetLoaderFxCameraActive(false);
        BindViewPresenters();
        SubscribeToSequencer();
        RebuildPointList();

        if (_forceMenuOnEnable)
        {
            _routeSequencer?.StopRoute(stopVps: false);
            ShowPointMenu();
        }
    }

    private void Start()
    {
        RebuildPointList();
    }

    private void OnDisable()
    {
        StopStartRouteRoutine();
        SetLoaderFxCameraActive(false);
        _localizingViewPresenter?.SetLoadingActive(false);
        UnsubscribeFromSequencer();
        UnbindViewPresenters();
    }

    /// <summary>
    /// Rebuilds destination list in menu view from route sequencer points.
    /// </summary>
    public void RebuildPointList()
    {
        if (_routeSequencer == null)
        {
            _routeSequencer = FindObjectOfType<VpsRouteSequencer>();
        }

        if (_routeSequencer == null || _menuViewPresenter == null)
        {
            return;
        }

        _menuViewPresenter.BindPoints(_routeSequencer.GetRoutePoints());
    }

    /// <summary>
    /// Starts navigation to a selected destination index.
    /// </summary>
    /// <param name="pointIndex">Destination index in route points list.</param>
    public void NavigateToPoint(int pointIndex)
    {
        if (_routeSequencer == null)
        {
            return;
        }

        StopStartRouteRoutine();
        _startRouteRoutine = StartCoroutine(StartRouteWithLocalizingFirst(pointIndex));
    }

    /// <summary>
    /// Stops current route and returns to destination menu.
    /// </summary>
    public void ReturnToPointMenu()
    {
        StopStartRouteRoutine();
        _awaitingArrivalAction = false;
        _routeSequencer?.StopRoute();
        ShowPointMenu();
    }

    /// <summary>
    /// Opens detailed "about location" screen for currently reached point.
    /// </summary>
    public void ShowAboutLocation()
    {
        _aboutViewPresenter?.SetPoint(_currentPoint);
        SetViewState(menuVisible: false, localizingVisible: false, routeVisible: false, arrivalVisible: false, aboutVisible: true);
    }

    /// <summary>
    /// Closes "about location" screen and returns to arrival actions.
    /// </summary>
    public void CloseAboutLocation()
    {
        SetViewState(menuVisible: false, localizingVisible: false, routeVisible: false, arrivalVisible: true, aboutVisible: false);
    }

    /// <summary>
    /// Finishes post-arrival flow and returns to destination menu.
    /// </summary>
    public void FinishNavigation()
    {
        ReturnToPointMenu();
    }

    private void ResolveViewReferences()
    {
        _menuView = ResolveViewRoot<NavMenuView>(_menuView, "MenuView", "NavMenuView");
        _localizingView = ResolveViewRoot<NavLocalizingView>(_localizingView, "LocalizingView", "NavLocalizingView");
        _tripSummaryView = ResolveViewRoot<NavTripSummaryView>(_tripSummaryView, "TripSummaryView", "NavTripSummaryView");
        _arrivalActionsView = ResolveViewRoot<NavArrivalView>(_arrivalActionsView, "ArrivalActionsView", "NavArrivalView");
        _aboutView = ResolveViewRoot<NavAboutView>(_aboutView, "AboutView", "NavAboutView");
    }

    private void ResolveLoaderFxCamera()
    {
        if (_loaderFxCamera != null)
        {
            return;
        }

        var loaderCameraObject = GameObject.Find("LoaderFXCamera");
        if (loaderCameraObject != null)
        {
            _loaderFxCamera = loaderCameraObject.GetComponent<Camera>();
        }
    }

    private void ResolveViewPresenters()
    {
        _menuViewPresenter = ResolveViewPresenter<NavMenuView>(_menuView);
        _localizingViewPresenter = ResolveViewPresenter<NavLocalizingView>(_localizingView);
        _tripSummaryViewPresenter = ResolveViewPresenter<NavTripSummaryView>(_tripSummaryView);
        _arrivalViewPresenter = ResolveViewPresenter<NavArrivalView>(_arrivalActionsView);
        _aboutViewPresenter = ResolveViewPresenter<NavAboutView>(_aboutView);
    }

    private void BindViewPresenters()
    {
        if (_menuViewPresenter != null)
        {
            _menuViewPresenter.PointSelected += NavigateToPoint;
        }

        if (_tripSummaryViewPresenter != null)
        {
            _tripSummaryViewPresenter.CloseClicked += ReturnToPointMenu;
        }

        if (_arrivalViewPresenter != null)
        {
            _arrivalViewPresenter.AboutClicked += ShowAboutLocation;
            _arrivalViewPresenter.FinishClicked += FinishNavigation;
        }

        if (_aboutViewPresenter != null)
        {
            _aboutViewPresenter.CloseClicked += CloseAboutLocation;
        }
    }

    private void UnbindViewPresenters()
    {
        if (_menuViewPresenter != null)
        {
            _menuViewPresenter.PointSelected -= NavigateToPoint;
        }

        if (_tripSummaryViewPresenter != null)
        {
            _tripSummaryViewPresenter.CloseClicked -= ReturnToPointMenu;
        }

        if (_arrivalViewPresenter != null)
        {
            _arrivalViewPresenter.AboutClicked -= ShowAboutLocation;
            _arrivalViewPresenter.FinishClicked -= FinishNavigation;
        }

        if (_aboutViewPresenter != null)
        {
            _aboutViewPresenter.CloseClicked -= CloseAboutLocation;
        }
    }

    private void SubscribeToSequencer()
    {
        if (_routeSequencer == null)
        {
            return;
        }

        _routeSequencer.RoutePointSelected += OnRoutePointSelected;
        _routeSequencer.RoutePointReached += OnRoutePointReached;
        _routeSequencer.RouteStateChanged += OnRouteStateChanged;
        _routeSequencer.VpsLocalizationStateChanged += OnVpsLocalizationStateChanged;
        _routeSequencer.RemainingDistanceUpdated += OnRemainingDistanceUpdated;
        _routeSequencer.RemainingTimeUpdated += OnRemainingTimeUpdated;
        _routeSequencer.StatusMessageChanged += OnStatusMessageChanged;
    }

    private void UnsubscribeFromSequencer()
    {
        if (_routeSequencer == null)
        {
            return;
        }

        _routeSequencer.RoutePointSelected -= OnRoutePointSelected;
        _routeSequencer.RoutePointReached -= OnRoutePointReached;
        _routeSequencer.RouteStateChanged -= OnRouteStateChanged;
        _routeSequencer.VpsLocalizationStateChanged -= OnVpsLocalizationStateChanged;
        _routeSequencer.RemainingDistanceUpdated -= OnRemainingDistanceUpdated;
        _routeSequencer.RemainingTimeUpdated -= OnRemainingTimeUpdated;
        _routeSequencer.StatusMessageChanged -= OnStatusMessageChanged;
    }

    private void OnRoutePointSelected(VpsRouteSequencer.RoutePoint point, int pointIndex)
    {
        _currentPoint = point;
        _currentPointIndex = pointIndex;
        _awaitingArrivalAction = false;

        if (_routeSequencer != null && !_routeSequencer.IsVpsReady)
        {
            ShowLocalizing(LocalizingMessage);
            return;
        }

        var distance = _routeSequencer != null ? _routeSequencer.GetRemainingDistance() : 0f;
        UpdateRouteProgressViews(distance);
        ShowRouteProgress();
    }

    private void OnRoutePointReached(VpsRouteSequencer.RoutePoint point, int pointIndex)
    {
        _currentPoint = point;
        _currentPointIndex = pointIndex;
        _awaitingArrivalAction = true;

        _arrivalViewPresenter?.SetPoint(GetPointDisplayName(point, pointIndex));
        SetViewState(menuVisible: false, localizingVisible: false, routeVisible: false, arrivalVisible: true, aboutVisible: false);
    }

    private void OnRouteStateChanged(bool isActive)
    {
        if (isActive || _awaitingArrivalAction)
        {
            return;
        }

        ShowPointMenu();
    }

    private void OnVpsLocalizationStateChanged(bool isReady)
    {
        if (!isReady)
        {
            ShowLocalizing(LocalizingMessage);
            return;
        }

        var distance = _routeSequencer != null ? _routeSequencer.GetRemainingDistance() : 0f;
        UpdateRouteProgressViews(distance);
        ShowRouteProgress();
    }

    private void OnRemainingDistanceUpdated(float distance)
    {
        if (_routeSequencer == null || !_routeSequencer.IsRouteActive || !_routeSequencer.IsVpsReady)
        {
            return;
        }

        UpdateRouteProgressViews(distance);
        ShowRouteProgress();
    }

    private void OnRemainingTimeUpdated(float remainingTime)
    {
        _lastRemainingTime = Mathf.Max(0f, remainingTime);
        if (_routeSequencer == null || !_routeSequencer.IsRouteActive)
        {
            return;
        }

        UpdateRouteProgressViews(_routeSequencer.GetRemainingDistance());
    }

    private void OnStatusMessageChanged(string message)
    {
        if (_awaitingArrivalAction)
        {
            return;
        }

        if (_routeSequencer != null && _routeSequencer.IsRouteActive && !_routeSequencer.IsVpsReady)
        {
            ShowLocalizing(string.IsNullOrWhiteSpace(message) ? LocalizingMessage : message);
            return;
        }

        if (_routeSequencer != null && _routeSequencer.IsRouteActive && _routeSequencer.IsVpsReady)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(message) && _localizingViewPresenter != null)
        {
            ShowLocalizing(message);
        }
    }

    private static string GetPointDisplayName(VpsRouteSequencer.RoutePoint point, int pointIndex)
    {
        if (!string.IsNullOrWhiteSpace(point?.Title))
        {
            return point.Title;
        }

        if (!string.IsNullOrWhiteSpace(point?.Id))
        {
            return point.Id;
        }

        return $"Point {pointIndex + 1}";
    }

    private void ShowPointMenu()
    {
        _menuViewPresenter?.SetTitle(null);
        SetViewState(menuVisible: true, localizingVisible: false, routeVisible: false, arrivalVisible: false, aboutVisible: false);
    }

    private void ShowLocalizing(string message)
    {
        if (_localizingView == null)
        {
            ShowRouteProgress();
            return;
        }

        _localizingViewPresenter?.SetMessage(message);
        SetViewState(menuVisible: false, localizingVisible: true, routeVisible: false, arrivalVisible: false, aboutVisible: false);
    }

    private void ShowRouteProgress()
    {
        SetViewState(menuVisible: false, localizingVisible: false, routeVisible: true, arrivalVisible: false, aboutVisible: false);
    }

    private IEnumerator StartRouteWithLocalizingFirst(int pointIndex)
    {
        ShowLocalizing(LocalizingMessage);
        yield return null;

        if (_routeSequencer == null || !_routeSequencer.StartRouteToPoint(pointIndex))
        {
            ShowPointMenu();
            _startRouteRoutine = null;
            yield break;
        }

        _currentPointIndex = pointIndex;
        _currentPoint = null;
        _awaitingArrivalAction = false;

        if (_routeSequencer.IsVpsReady)
        {
            ShowRouteProgress();
        }

        _startRouteRoutine = null;
    }

    private void StopStartRouteRoutine()
    {
        if (_startRouteRoutine == null)
        {
            return;
        }

        StopCoroutine(_startRouteRoutine);
        _startRouteRoutine = null;
    }

    private void UpdateRouteProgressViews(float distance)
    {
        if (_tripSummaryViewPresenter == null)
        {
            return;
        }

        var pointTitle = GetPointDisplayName(_currentPoint, _currentPointIndex);
        _tripSummaryViewPresenter.SetSummary(pointTitle, distance, _lastRemainingTime);
    }

    private void SetViewState(bool menuVisible, bool localizingVisible, bool routeVisible, bool arrivalVisible, bool aboutVisible)
    {
        if (_menuView != null)
        {
            _menuView.SetActive(menuVisible);
        }

        if (_localizingView != null)
        {
            _localizingView.SetActive(localizingVisible);
        }

        if (_tripSummaryView != null)
        {
            _tripSummaryView.SetActive(routeVisible);
        }

        if (_arrivalActionsView != null)
        {
            _arrivalActionsView.SetActive(arrivalVisible);
        }

        if (_aboutView != null)
        {
            _aboutView.SetActive(aboutVisible);
        }

        SetLoaderFxCameraActive(localizingVisible);
        _localizingViewPresenter?.SetLoadingActive(localizingVisible);
    }

    private void SetLoaderFxCameraActive(bool isActive)
    {
        ResolveLoaderFxCamera();
        if (_loaderFxCamera != null)
        {
            _loaderFxCamera.enabled = isActive;
        }
    }

    private GameObject ResolveViewRoot<T>(GameObject assignedView, params string[] candidateNames) where T : Component
    {
        if (assignedView != null && assignedView.GetComponent<T>() != null)
        {
            return assignedView;
        }

        if (candidateNames != null)
        {
            for (var i = 0; i < candidateNames.Length; i++)
            {
                var candidateName = candidateNames[i];
                if (string.IsNullOrWhiteSpace(candidateName))
                {
                    continue;
                }

                var directChild = transform.Find(candidateName);
                if (directChild != null && directChild.TryGetComponent<T>(out _))
                {
                    return directChild.gameObject;
                }
            }
        }

        var nestedView = GetComponentInChildren<T>(true);
        return nestedView == null ? assignedView : nestedView.gameObject;
    }

    private static T ResolveViewPresenter<T>(GameObject viewRoot) where T : Component
    {
        if (viewRoot == null)
        {
            return null;
        }

        if (viewRoot.TryGetComponent<T>(out var presenter))
        {
            return presenter;
        }

        return viewRoot.AddComponent<T>();
    }
}
