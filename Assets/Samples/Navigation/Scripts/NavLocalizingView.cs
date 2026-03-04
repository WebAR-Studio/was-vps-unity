using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavLocalizingView : MonoBehaviour
{
    [SerializeField] private Text _messageText;
    [SerializeField] private CircleLoadEffect _circleLoadEffect;

    private bool _isLoadingActive;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        SetLoadingActive(false);
    }

    /// <summary>
    /// Sets localizing status message.
    /// </summary>
    /// <param name="message">Status message text.</param>
    public void SetMessage(string message)
    {
        if (_messageText == null)
        {
            return;
        }

        _messageText.text = message;
    }

    /// <summary>
    /// Enables or disables circular loading effect for VPS localization.
    /// </summary>
    /// <param name="isActive">True to spawn circles; false to clear them.</param>
    public void SetLoadingActive(bool isActive)
    {
        ResolveReferences();

        if (_circleLoadEffect == null || _isLoadingActive == isActive)
        {
            return;
        }

        _isLoadingActive = isActive;

        if (_isLoadingActive)
        {
            _circleLoadEffect.SpawnCircles();
            return;
        }

        _circleLoadEffect.ClearCircles();
    }

    private void ResolveReferences()
    {
        _messageText ??= transform.Find("LocalizingText")?.GetComponent<Text>();
        _circleLoadEffect ??= GetComponentInChildren<CircleLoadEffect>(true);
    }
}
