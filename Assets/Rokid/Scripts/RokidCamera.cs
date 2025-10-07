using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using WASVPS;

public class RokidCamera : MonoBehaviour, IWASVPSCamera
{
    [SerializeField] private RawImage _webTextureField;
    [SerializeField] private bool _isNeedRotate = false;

    private WebCamTexture _webcam;
    private Texture2D _frameTex;
    private bool _isReady;

    private int _width = 540;
    private int _height = 960;
    private TextureFormat _textureFormat;

    private const int _targetW = 540;
    private const int _targetH = 960;

    public void Init(WASVPSTextureRequirement[] requirements)
    {
        StartCoroutine(InitCoroutine(requirements));
    }

    private IEnumerator InitCoroutine(WASVPSTextureRequirement[] requirements)
    {
        yield return PermissionService.Instance.RequestAsync(Permission.Camera);
        yield return PermissionService.Instance.RequestAsync(Permission.FineLocation);

        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("[RokidCamera] Камера не найдена на устройстве.");
            _isReady = false;
            yield break;
        }

        if (requirements != null && requirements.Length > 0)
        {
            var req = requirements[0];
            _width = req.Width > 0 ? req.Width : _width;
            _height = req.Height > 0 ? req.Height : _height;
            _textureFormat = req.Format;
        }

        _webcam = new WebCamTexture(WebCamTexture.devices[0].name, _width, _height, 30);
        _webcam.Play();

        _frameTex = new Texture2D(_width, _height, _textureFormat, false);
        
        _isReady = true;
        Debug.Log($"[RokidCamera] Камера инициализирована: {_width}x{_height}");
    }

    public bool IsCameraReady()
    {
        return _isReady && _webcam != null && _webcam.isPlaying;
    }

    public Texture2D GetFrame(WASVPSTextureRequirement requirement)
    {
        if (!IsCameraReady()) return null;

        int srcW = _webcam.width;
        int srcH = _webcam.height;

        Color32[] pixels = _webcam.GetPixels32();

        if (srcW > srcH && _isNeedRotate)
        {
            pixels = Rotate90(pixels, srcW, srcH);
            int tmp = srcW;
            srcW = srcH;
            srcH = tmp;
        }

        float targetAspect = (float)_targetW / _targetH;
        float srcAspect = (float)srcW / srcH;

        int cropW = srcW;
        int cropH = srcH;

        if (Mathf.Abs(srcAspect - targetAspect) > 0.001f)
        {
            if (srcAspect > targetAspect)
            {
                cropW = Mathf.RoundToInt(srcH * targetAspect);
                cropH = srcH;
            }
            else
            {
                cropW = srcW;
                cropH = Mathf.RoundToInt(srcW / targetAspect);
            }

            pixels = CropCenter(pixels, srcW, srcH, cropW, cropH);
            srcW = cropW;
            srcH = cropH;
        }

        Texture2D croppedTex = new Texture2D(srcW, srcH, TextureFormat.RGBA32, false);
        croppedTex.SetPixels32(pixels);
        croppedTex.Apply(false);

        Texture2D finalTex = new Texture2D(_targetW, _targetH, TextureFormat.RGBA32, false);
        ScaleTexture(croppedTex, finalTex);
        _webTextureField.texture = finalTex;
        Debug.Log($"Final frame: {finalTex.width}x{finalTex.height}");
        return finalTex;
    }

    private Color32[] Rotate90(Color32[] src, int width, int height)
    {
        Color32[] dst = new Color32[src.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcIndex = y * width + x;
                int newX = y;
                int newY = (width - 1) - x;
                dst[newY * height + newX] = src[srcIndex];
            }
        }

        return dst;
    }

    private Color32[] CropCenter(Color32[] src, int srcW, int srcH, int cropW, int cropH)
    {
        Color32[] dst = new Color32[cropW * cropH];

        int offsetX = (srcW - cropW) / 2;
        int offsetY = (srcH - cropH) / 2;

        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                dst[y * cropW + x] = src[(y + offsetY) * srcW + (x + offsetX)];
            }
        }
        return dst;
    }

    private void ScaleTexture(Texture2D src, Texture2D dst)
    {
        float ratioX = (float)src.width / dst.width;
        float ratioY = (float)src.height / dst.height;

        for (int y = 0; y < dst.height; y++)
        {
            for (int x = 0; x < dst.width; x++)
            {
                float gx = (x + 0.5f) * ratioX - 0.5f;
                float gy = (y + 0.5f) * ratioY - 0.5f;
                dst.SetPixel(x, y, src.GetPixelBilinear(gx / src.width, gy / src.height));
            }
        }
        dst.Apply(false);
    }

    public Vector2 GetFocalPixelLength()
    {
        // TODO Заглушка
        return new Vector2(900f, 900f);
    }

    public Vector2 GetPrincipalPoint()
    {
        // TODO Заглушка
        return new Vector2(_width / 2f, _height / 2f);
    }
}
