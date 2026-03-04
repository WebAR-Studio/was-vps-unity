using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavAboutView : MonoBehaviour
{
    private const string EmptyDescription = "No information for this location yet.";
    private const string EmptyAddress = "Address is not specified.";
    private const string EmptyRoomAddress = "Indoor location is not specified.";
    private const string EmptyWorkingHours = "N/A";

    [SerializeField] private Text _titleText;
    [SerializeField] private Text _locationText;
    [SerializeField] private Text _addressText;
    [SerializeField] private Text _workingHoursText;
    [SerializeField] private Text _descriptionText;
    [SerializeField] private Image _aboutImage;
    [SerializeField] private Button _closeButton;

    [Header("Dynamic Layout")]
    [SerializeField] private Vector2 _descriptionNoImagePosition = new(32f, -222f);
    [SerializeField] private Vector2 _descriptionNoImageSize = new(328f, 386f);
    [SerializeField] private Vector2 _descriptionWithImagePosition = new(32f, -390f);
    [SerializeField] private Vector2 _descriptionWithImageSize = new(328f, 218f);
    [SerializeField] private Vector2 _imagePosition = new(32f, -222f);
    [SerializeField] private Vector2 _imageSize = new(328f, 160f);

    /// <summary>
    /// Invoked when close button is pressed.
    /// </summary>
    public event Action CloseClicked;

    private void Awake()
    {
        ResolveReferences();
        BindCloseButton();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        UnbindCloseButton();
    }

    /// <summary>
    /// Updates about view with point data.
    /// </summary>
    /// <param name="point">Current route point data.</param>
    public void SetPoint(VpsRouteSequencer.RoutePoint point)
    {
        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrWhiteSpace(point?.Title) ? "Point" : point.Title;
        }

        if (_locationText != null)
        {
            _locationText.text = string.IsNullOrWhiteSpace(point?.RoomAddress)
                ? EmptyRoomAddress
                : point.RoomAddress;
        }

        if (_addressText != null)
        {
            _addressText.text = string.IsNullOrWhiteSpace(point?.Address)
                ? EmptyAddress
                : point.Address;
        }

        if (_workingHoursText != null)
        {
            _workingHoursText.text = string.IsNullOrWhiteSpace(point?.WorkingHours)
                ? EmptyWorkingHours
                : point.WorkingHours;
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.IsNullOrWhiteSpace(point?.Description)
                ? EmptyDescription
                : point.Description;
        }

        ApplyImage(point?.Image);
    }

    private void ResolveReferences()
    {
        _titleText ??= transform.Find("AboutTitleText")?.GetComponent<Text>();
        _titleText ??= transform.Find("Content/Info/AboutTitleText")?.GetComponent<Text>();

        _locationText ??= transform.Find("AboutLocationText")?.GetComponent<Text>();
        _locationText ??= transform.Find("Content/Info/AboutLocationText")?.GetComponent<Text>();

        _addressText ??= transform.Find("AboutCategoriesText")?.GetComponent<Text>();
        _addressText ??= transform.Find("Content/Info/AboutCategoriesText")?.GetComponent<Text>();

        _workingHoursText ??= transform.Find("ScheduleBadge/TimeText")?.GetComponent<Text>();
        _workingHoursText ??= transform.Find("Content/ScheduleRow/ScheduleBadge/TimeText")?.GetComponent<Text>();

        _descriptionText ??= transform.Find("AboutDescriptionText")?.GetComponent<Text>();
        _descriptionText ??= transform.Find("Content/DescriptionSection/AboutDescriptionText")?.GetComponent<Text>();

        _aboutImage ??= transform.Find("AboutImage")?.GetComponent<Image>();
        _closeButton ??= transform.Find("AboutCloseButton")?.GetComponent<Button>();
    }

    private void BindCloseButton()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseClick);
        }
    }

    private void UnbindCloseButton()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(HandleCloseClick);
        }
    }

    private void HandleCloseClick()
    {
        CloseClicked?.Invoke();
    }

    private void ApplyImage(Sprite pointImage)
    {
        var hasPointImage = pointImage != null && pointImage.texture != null;
        if (!hasPointImage)
        {
            if (_aboutImage != null)
            {
                _aboutImage.sprite = null;
                _aboutImage.gameObject.SetActive(false);
            }

            ApplyDescriptionLayout(hasImage: false);
            return;
        }

        EnsureImageObject();

        if (_aboutImage != null)
        {
            _aboutImage.sprite = pointImage;
            _aboutImage.preserveAspect = true;
            _aboutImage.raycastTarget = false;
            _aboutImage.gameObject.SetActive(true);
        }

        ApplyDescriptionLayout(hasImage: true);
    }

    private void EnsureImageObject()
    {
        if (_aboutImage != null)
        {
            return;
        }

        var imageObject = new GameObject("AboutImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0f, 1f);
        imageRect.anchorMax = new Vector2(0f, 1f);
        imageRect.pivot = new Vector2(0f, 1f);
        imageRect.anchoredPosition = _imagePosition;
        imageRect.sizeDelta = _imageSize;

        _aboutImage = imageObject.GetComponent<Image>();
        _aboutImage.raycastTarget = false;
        _aboutImage.gameObject.SetActive(false);
    }

    private void ApplyDescriptionLayout(bool hasImage)
    {
        if (_descriptionText == null)
        {
            return;
        }

        var descriptionRect = _descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(0f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.anchoredPosition = hasImage ? _descriptionWithImagePosition : _descriptionNoImagePosition;
        descriptionRect.sizeDelta = hasImage ? _descriptionWithImageSize : _descriptionNoImageSize;
    }
}
