using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavRouteInfoView : MonoBehaviour
{
    [SerializeField] private Text _routeInfoText;
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
    /// Sets route information text.
    /// </summary>
    /// <param name="message">Route information message.</param>
    public void SetMessage(string message)
    {
        if (_routeInfoText == null)
        {
            return;
        }

        _routeInfoText.text = message;
    }

    private void ResolveReferences()
    {
        _routeInfoText ??= transform.Find("Body/RouteInfoText")?.GetComponent<Text>();
        _closeButton ??= transform.Find("Body/CloseButton")?.GetComponent<Button>();
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
