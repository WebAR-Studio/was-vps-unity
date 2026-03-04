using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavRouteEndWorldUiController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private VpsRouteSequencer _routeSequencer;
    [SerializeField] private Sprite _pinSprite;
    [SerializeField] private Camera _targetCamera;

    [Header("Layout")]
    [SerializeField] private Vector3 _worldOffset = new(0f, 2f, 0f);
    [SerializeField] private Vector2 _designSize = new(255f, 329f);
    [SerializeField] private float _worldScale = 0.0025f;

    private GameObject _worldViewRoot;

    private void OnEnable()
    {
        if (_routeSequencer == null)
        {
            _routeSequencer = FindObjectOfType<VpsRouteSequencer>();
        }

        if (_routeSequencer == null)
        {
            return;
        }

        _routeSequencer.RoutePointSelected += OnRoutePointSelected;
        _routeSequencer.RouteStateChanged += OnRouteStateChanged;
    }

    private void OnDisable()
    {
        if (_routeSequencer != null)
        {
            _routeSequencer.RoutePointSelected -= OnRoutePointSelected;
            _routeSequencer.RouteStateChanged -= OnRouteStateChanged;
        }

        SetWorldViewActive(false);
    }

    /// <summary>
    /// Removes runtime-created world-space destination view.
    /// </summary>
    public void DestroyWorldView()
    {
        if (_worldViewRoot == null)
        {
            return;
        }

        Destroy(_worldViewRoot);
        _worldViewRoot = null;
    }

    private void OnRoutePointSelected(VpsRouteSequencer.RoutePoint point, int pointIndex)
    {
        if (point == null)
        {
            SetWorldViewActive(false);
            return;
        }

        EnsureWorldView();

        if (_worldViewRoot == null)
        {
            return;
        }

        _worldViewRoot.transform.position = VpsRouteSequencer.GetPointWorldPosition(point) + _worldOffset;
        SetWorldViewActive(true);
    }

    private void OnRouteStateChanged(bool isRouteActive)
    {
        if (!isRouteActive)
        {
            SetWorldViewActive(false);
        }
    }

    private void EnsureWorldView()
    {
        if (_worldViewRoot != null)
        {
            return;
        }

        var root = new GameObject(
            "NavRouteEndWorldView",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(NavWorldSpaceFaceCamera));

        root.transform.SetParent(transform, false);

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = _designSize;
        rect.localScale = Vector3.one * Mathf.Max(0.0001f, _worldScale);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = ResolveCamera();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        var imageObject = new GameObject("PinImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(root.transform, false);

        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = Vector2.zero;

        var image = imageObject.GetComponent<Image>();
        image.sprite = _pinSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        image.enabled = _pinSprite != null;

        var billboard = root.GetComponent<NavWorldSpaceFaceCamera>();
        billboard.SetTargetCamera(ResolveCamera());

        _worldViewRoot = root;
        _worldViewRoot.SetActive(false);
    }

    private Camera ResolveCamera()
    {
        return _targetCamera != null ? _targetCamera : Camera.main;
    }

    private void SetWorldViewActive(bool isActive)
    {
        if (_worldViewRoot != null)
        {
            _worldViewRoot.SetActive(isActive);
        }
    }
}
