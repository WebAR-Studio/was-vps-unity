using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignatureManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _signatureText;
    [SerializeField] private Image _signatureImage;

    public void SetSignature(string text, Color32 color)
    {
        _signatureText.text = text;
        _signatureImage.color = color;
    }
}
