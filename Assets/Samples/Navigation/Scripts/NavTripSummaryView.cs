using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavTripSummaryView : MonoBehaviour
{
    [SerializeField] private Text _durationText;
    [SerializeField] private Text _distanceText;
    [SerializeField] private Text _placeText;
    [SerializeField] private Button _closeButton;

    /// <summary>
    /// Invoked when close button is pressed.
    /// </summary>
    public event Action CloseClicked;

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        UnbindButton();
    }

    /// <summary>
    /// Updates summary values for active route.
    /// </summary>
    /// <param name="pointTitle">Destination title.</param>
    /// <param name="remainingDistanceMeters">Remaining distance in meters.</param>
    /// <param name="remainingTimeSeconds">Estimated remaining time in seconds.</param>
    public void SetSummary(string pointTitle, float remainingDistanceMeters, float remainingTimeSeconds)
    {
        if (_placeText != null && !string.IsNullOrWhiteSpace(pointTitle))
        {
            _placeText.text = pointTitle;
        }

        if (_distanceText != null)
        {
            var meters = Mathf.Max(0, Mathf.RoundToInt(remainingDistanceMeters));
            _distanceText.text = $"{meters} m";
        }

        if (_durationText != null)
        {
            var minutes = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, remainingTimeSeconds) / 60f));
            _durationText.text = $"{minutes} min";
        }
    }

    private void ResolveReferences()
    {
        _durationText ??= transform.Find("TextGroup/DurationText")?.GetComponent<Text>();
        _distanceText ??= transform.Find("TextGroup/DistanceText")?.GetComponent<Text>();
        _placeText ??= transform.Find("TextGroup/PlaceText")?.GetComponent<Text>();
        _closeButton ??= transform.Find("CloseButton")?.GetComponent<Button>();
    }

    private void BindButton()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(HandleCloseClick);
        }
    }

    private void UnbindButton()
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
}
