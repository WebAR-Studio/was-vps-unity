using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavArrivalView : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private Text _locationText;
    [SerializeField] private Button _aboutButton;
    [SerializeField] private Button _finishButton;
    [SerializeField] private string _defaultTitle = "You have arrived";

    /// <summary>
    /// Invoked when "About" button is pressed.
    /// </summary>
    public event Action AboutClicked;

    /// <summary>
    /// Invoked when "Finish" button is pressed.
    /// </summary>
    public event Action FinishClicked;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    /// <summary>
    /// Updates arrival view with point data.
    /// </summary>
    /// <param name="pointTitle">Reached point title.</param>
    public void SetPoint(string pointTitle)
    {
        if (_titleText != null)
        {
            _titleText.text = _defaultTitle;
        }

        if (_locationText != null)
        {
            _locationText.text = string.IsNullOrWhiteSpace(pointTitle) ? string.Empty : pointTitle;
        }
    }

    private void ResolveReferences()
    {
        _titleText ??= transform.Find("TextGroup/ArrivalTitleText")?.GetComponent<Text>();
        _locationText ??= transform.Find("TextGroup/ArrivalLocationText")?.GetComponent<Text>();
        _aboutButton ??= transform.Find("ButtonsRow/AboutLocationButton")?.GetComponent<Button>();
        _finishButton ??= transform.Find("ButtonsRow/FinishButton")?.GetComponent<Button>();
    }

    private void BindButtons()
    {
        if (_aboutButton != null)
        {
            _aboutButton.onClick.AddListener(HandleAboutClick);
        }

        if (_finishButton != null)
        {
            _finishButton.onClick.AddListener(HandleFinishClick);
        }
    }

    private void UnbindButtons()
    {
        if (_aboutButton != null)
        {
            _aboutButton.onClick.RemoveListener(HandleAboutClick);
        }

        if (_finishButton != null)
        {
            _finishButton.onClick.RemoveListener(HandleFinishClick);
        }
    }

    private void HandleAboutClick()
    {
        AboutClicked?.Invoke();
    }

    private void HandleFinishClick()
    {
        FinishClicked?.Invoke();
    }
}
