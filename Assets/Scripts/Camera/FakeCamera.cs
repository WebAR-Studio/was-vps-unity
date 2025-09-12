using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WASVPS
{
    public enum FakeTextureLoadingType { Texture, ImagePath };
    /// <summary>
    /// Return FakeTexture image
    /// </summary>
    public class FakeCamera : MonoBehaviour, IWASVPSCamera
    {
        private Vector2Int _cameraResolution = new(1920, 1080);
        public FakeTextureLoadingType LoadingType;

        [Tooltip("Texture for sending")]
        public Texture2D FakeTexture;
        [Tooltip("Texture for sending")]
        public string ImageLocalPath;

        private Texture2D _ppFakeTexture;
        private Texture2D _convertTexture;
        private Dictionary<WASVPSTextureRequirement, NativeArray<byte>> _buffers;

        private Image _mockImage;
        private float _resizeCoef = 1.0f;
        private Camera _mainCamera;

        private const float FakeFocalPixelLength = 1444.24768066f;

        /// <summary>
        /// Initialize the main camera reference on start
        /// </summary>
        private void Start()
        {
            _mainCamera = Camera.main;
        }

        /// <summary>
        /// Initialize the fake camera with texture requirements
        /// </summary>
        /// <param name="requirements">Array of texture requirements for different formats and sizes</param>
        public void Init(WASVPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequirements = requirements.Distinct().ToList();
            _buffers = distinctRequirements.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height * r.ChannelsCount(), Allocator.Persistent));

            InitBuffers();
            _resizeCoef = (float)_buffers.FirstOrDefault().Key.Width / (float)_cameraResolution.y;
            SetCameraFov();
        }

        /// <summary>
        /// Validate and update texture loading when inspector values change
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (LoadingType == FakeTextureLoadingType.Texture)
            {
                if (FakeTexture == null)
                    return;
            }
            else
            {
                var fullName = Directory.GetParent(Application.dataPath)?.FullName;
                if (fullName != null)
                {
                    var fullPath = Path.Combine(fullName, ImageLocalPath);
                    FakeTexture.LoadImage(File.ReadAllBytes(fullPath));
                }
            }

            InitBuffers();

#if UNITY_EDITOR
            EditorApplication.delayCall = () => ShowMockFrame(FakeTexture);
#endif
        }

        /// <summary>
        /// Init all buffers from image by requrements
        /// </summary>
        private void InitBuffers()
        {
            if (_buffers == null || _buffers.Count == 0)
                return;

            foreach (var req in _buffers.Keys)
            {
                _convertTexture = Preprocess(req.Format);
                if (_convertTexture.width != req.Width || _convertTexture.height != req.Height)
                {
                    var inputRect = WASVPSTextureRequirement.GetCropRect(_convertTexture.width, _convertTexture.height, ((float)req.Height) / ((float)req.Width));
                    _convertTexture = CropScale.CropTexture(_convertTexture, new Vector2(inputRect.height, inputRect.width), req.Format, CropOptions.CUSTOM, inputRect.x, inputRect.y);
                    _convertTexture = CropScale.ScaleTexture(_convertTexture, req.Width, req.Height, req.Format);
                }
                _buffers[req].CopyFrom(_convertTexture.GetRawTextureData());
            }
        }

        /// <summary>
        /// Get the focal pixel length for camera calibration
        /// </summary>
        /// <returns>Vector2 containing focal pixel length for X and Y axes</returns>
        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(FakeFocalPixelLength * _resizeCoef, FakeFocalPixelLength * _resizeCoef);
        }

        /// <summary>
        /// Get a texture frame based on the specified requirement
        /// </summary>
        /// <param name="requirement">Texture requirement specifying width, height, and format</param>
        /// <returns>Texture2D containing the processed frame data</returns>
        public Texture2D GetFrame(WASVPSTextureRequirement requirement)
        {
            if (!_ppFakeTexture || _ppFakeTexture.width != requirement.Width || _ppFakeTexture.height != requirement.Height || _ppFakeTexture.format != requirement.Format)
            {
                _ppFakeTexture = new Texture2D(requirement.Width, requirement.Height, requirement.Format, false);
            }

            _ppFakeTexture.LoadRawTextureData(_buffers[requirement]);
            _ppFakeTexture.Apply();
            return _ppFakeTexture;
        }

        /// <summary>
        /// Get the raw byte buffer for the specified texture requirement
        /// </summary>
        /// <param name="requirement">Texture requirement to get buffer for</param>
        /// <returns>NativeArray of bytes containing the raw texture data</returns>
        public NativeArray<byte> GetBuffer(WASVPSTextureRequirement requirement)
        {
            return _buffers[requirement];
        }

        /// <summary>
        /// Get the principal point coordinates for camera calibration
        /// </summary>
        /// <returns>Vector2 containing the principal point coordinates</returns>
        public Vector2 GetPrincipalPoint()
        {
            var req = _buffers.FirstOrDefault().Key;
            return new Vector2(req.Width * _resizeCoef, req.Height * _resizeCoef);
        }

        /// <summary>
        /// Check if the fake camera is ready to provide frames
        /// </summary>
        /// <returns>True if the fake texture is available, false otherwise</returns>
        public bool IsCameraReady()
        {
            return FakeTexture;
        }

        /// <summary>
        /// Clean up resources when the object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        /// <summary>
        /// Free all buffers
        /// </summary>
        private void FreeBufferMemory()
        {
            if (_buffers == null)
                return;

            foreach (var buffer in _buffers.Values.Where(buffer => buffer.IsCreated))
            {
                buffer.Dispose();
            }
            _buffers.Clear();
        }

        /// <summary>
        /// Rotate FakeTexture and copy the red channel to green and blue
        /// </summary>
        private Texture2D Preprocess(TextureFormat format)
        {
            var w = FakeTexture.width;
            var h = FakeTexture.height;
            
            var rotatedTexture = new Texture2D(w, h, format, false);
            rotatedTexture.SetPixels(FakeTexture.GetPixels());
            rotatedTexture.Apply();
            return rotatedTexture;
        }

        /// <summary>
        /// Create mock frame on the background
        /// </summary>
        private void ShowMockFrame(Texture mockTexture)
        {
            if (!gameObject.activeSelf)
                return;

            if (!_mockImage)
            {
                var canvasGo = new GameObject("FakeCamera");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

                var worldCamera = FindObjectOfType<Camera>();
                if (!worldCamera)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Virtual camera is not found");
                    return;
                }

                canvas.worldCamera = worldCamera;
                canvas.planeDistance = worldCamera.farClipPlane - 10f;

                var imgGo = new GameObject("FakeFrame");
                var imgTransform = imgGo.AddComponent<RectTransform>();
                imgTransform.SetParent(canvasGo.transform, false);

                imgTransform.anchorMin = Vector2.zero;
                imgTransform.anchorMax = Vector2.one;

                _mockImage = imgGo.AddComponent<Image>();
                _mockImage.preserveAspect = true;
            }

            _mockImage.sprite = Sprite.Create((Texture2D)mockTexture, new Rect(0, 0, mockTexture.width, mockTexture.height), Vector2.zero);
        }

        /// <summary>
        /// Enable or disable the mock image display
        /// </summary>
        /// <param name="enable">True to show the mock image, false to hide it</param>
        public void SetMockImageEnable(bool enable)
        {
            _mockImage.gameObject.SetActive(enable);
        }

        /// <summary>
        /// Set camera fov for correct rendering 
        /// </summary>
        private void SetCameraFov()
        {
            var h = _cameraResolution.x;
            var fy = FakeFocalPixelLength;

            var fovY = (float)(2 * Mathf.Atan(h / 2 / fy) * 180 / Mathf.PI);

            if (_mainCamera) _mainCamera.fieldOfView = fovY;
        }
    }
}