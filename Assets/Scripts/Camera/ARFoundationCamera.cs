using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace WASVPS
{
    public class ARFoundationCamera : MonoBehaviour, IWASVPSCamera
    {
        private ARCameraManager _cameraManager;
        private Texture2D _texture;

        private Dictionary<WASVPSTextureRequirement, NativeArray<byte>> _buffers;

        private NativeArray<XRCameraConfiguration> _configurations;
        private bool _configurationsDisposed = false;

        private SimpleJob _job;
        private RotateJob _rotateJob;
        private NativeArray<Color> _colorArray;

        public static Semaphore semaphore = new Semaphore(1);

        private bool _isReady = false;
        private DeviceOrientation _currentOrientation;
        private bool _isPortraitOrientation;

        #region Metrics

        private const string CopyImageFrameChannelRunTime = "CopyImageFrameChannelRunTime";

        #endregion

        /// <summary>
        /// Initialize AR camera manager and subscribe to frame events
        /// </summary>
        private void Awake()
        {
            _cameraManager = FindObjectOfType<ARCameraManager>();
            if (!_cameraManager)
            {
                VPSLogger.Log(LogLevel.ERROR, "ARCameraManager is not found");
                return;
            }
            _cameraManager.frameReceived += UpdateFrame;
        }

        /// <summary>
        /// Initialize the AR camera with texture requirements
        /// </summary>
        /// <param name="requirements">Array of texture requirements for different formats and sizes</param>
        public void Init(WASVPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequirements = requirements.Distinct().ToList();
            _buffers = distinctRequirements.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height * r.ChannelsCount(), Allocator.Persistent));

            _isReady = false;
            semaphore.Free();
        }

        /// <summary>
        /// Initialize camera configuration and wait for available resolutions
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator Start()
        {   
            _job = new SimpleJob();
            _rotateJob = new RotateJob();

            while (_configurations.Length == 0)
            {
                if ((_cameraManager == null) || _cameraManager.subsystem is not { running: true })
                {
                    yield return null;
                    continue;
                }

                // Try to get available resolutions
                if (!_configurationsDisposed)
                {
                    _configurations.Dispose();
                }
                _configurations = _cameraManager.GetConfigurations(Allocator.Temp);
                _configurationsDisposed = false;

                if (!_configurations.IsCreated || (_configurations.Length <= 0))
                {
                    yield return null;
                    continue;
                }

                // Try to get 1920x1080 resolution
                var hdConfig = _configurations.FirstOrDefault(a => a is { width: 1920, height: 1080 });
                if (hdConfig == default)
                {
                    VPSLogger.Log(LogLevel.DEBUG, "Can't take HD resolution. The best available one will be chosen");
                    // Get the best resolution
                    hdConfig = _configurations.OrderByDescending(a => a.width * a.height).FirstOrDefault();
                }

                _cameraManager.currentConfiguration = hdConfig;
            } 
        }

        /// <summary>
        /// Update texture from AR camera frame
        /// </summary>
        /// <param name="args">AR camera frame event arguments</param>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            if (!IsValidState())
                return;

            if (!semaphore.CheckState())
                return;
            semaphore.TakeOne();

            // Get latest camera image
            if (!_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't take camera image");
                _isReady = false;
                semaphore.Free();
                return;
            }

            try
            {
                // Convert XRCpuImage to texture
                foreach (var req in _buffers.Keys)
                {
                    if (_isPortraitOrientation)
                    {
                        image.Convert(req.GetConversionParams(image, req.Height, req.Width), new IntPtr(_buffers[req].GetUnsafePtr()), _buffers[req].Length);
                    }
                    else
                    {
                        image.Convert(req.GetConversionParams(image, req.Width, req.Height), new IntPtr(_buffers[req].GetUnsafePtr()), _buffers[req].Length);
                    }
                    RotateImage(req);
                }
            }
            catch (Exception ex)
            {
                VPSLogger.Log(LogLevel.ERROR, ex);
                _isReady = false;
            }
            finally
            {
                // Free memory
                image.Dispose();
            }
            semaphore.Free();
            _isReady = true;
        }

        /// <summary>
        /// Get a texture frame based on the specified requirement
        /// </summary>
        /// <param name="requirement">Texture requirement specifying width, height, and format</param>
        /// <returns>Texture2D containing the processed frame data</returns>
        public Texture2D GetFrame(WASVPSTextureRequirement requirement)
        {
            MetricsCollector.Instance.StartStopwatch(CopyImageFrameChannelRunTime);
            
            if (_texture == null || _texture.width != requirement.Width || _texture.height != requirement.Height || _texture.format != requirement.Format)
            {
                if (_texture != null)
                    DestroyImmediate(_texture);
                _texture = new Texture2D(requirement.Width, requirement.Height, requirement.Format, false);
            }
            
            _texture.LoadRawTextureData(GetBuffer(requirement));
            _texture.Apply();

            // need to copy the red channel to green and blue
            ProcessColorChannels(requirement);

            MetricsCollector.Instance.StopStopwatch(CopyImageFrameChannelRunTime);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", CopyImageFrameChannelRunTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(CopyImageFrameChannelRunTime));
            return _texture;
        }

        /// <summary>
        /// Get the focal pixel length for camera calibration
        /// </summary>
        /// <returns>Vector2 containing focal pixel length for X and Y axes</returns>
        public Vector2 GetFocalPixelLength()
        {
            if (!IsValidState() || !_cameraManager.currentConfiguration.HasValue)
                return Vector2.zero;

            if (!_cameraManager.TryGetIntrinsics(out var intrins)) 
                return Vector2.zero;
            
            var req = _buffers.FirstOrDefault().Key;
            var config = _cameraManager.currentConfiguration.Value;
            var resizeCoef = CalculateResizeCoefficient(req, config);

            return new Vector2(intrins.focalLength.x * resizeCoef, intrins.focalLength.y * resizeCoef);
        }

        /// <summary>
        /// Get the principal point coordinates for camera calibration
        /// </summary>
        /// <returns>Vector2 containing the principal point coordinates</returns>
        public Vector2 GetPrincipalPoint()
        {
            if (!IsValidState())
                return Vector2.zero;

            var req = _buffers.FirstOrDefault().Key;
            return new Vector2(req.Width / 2f, req.Height / 2f);
        }

        /// <summary>
        /// Check if the AR camera is ready to provide frames
        /// </summary>
        /// <returns>True if the camera is ready, false otherwise</returns>
        public bool IsCameraReady()
        {
            return _isReady;
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
        /// Rotate image based on current device orientation
        /// </summary>
        /// <param name="requirement">Texture requirement to rotate</param>
        private void RotateImage(WASVPSTextureRequirement requirement)
        {
            if (_currentOrientation == DeviceOrientation.LandscapeRight)
                return; // No rotation needed

            _rotateJob.Width = requirement.Height;
            _rotateJob.Height = requirement.Width;
            _rotateJob.Orientation = _currentOrientation;
            _rotateJob.Input = _buffers[requirement];
            _rotateJob.Output = new NativeArray<byte>(_buffers[requirement].Length, Allocator.TempJob);

            var handle = _rotateJob.Schedule(_buffers[requirement].Length, 64);
            handle.Complete();

            if (handle.IsCompleted)
            {
                _buffers[requirement].CopyFrom(_rotateJob.Output);
            }

            _rotateJob.Output.Dispose();
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

            if (_colorArray.IsCreated)
            {
                _colorArray.Dispose();
            }

            if (_configurations.IsCreated && !_configurationsDisposed)
            {
                _configurations.Dispose();
                _configurationsDisposed = true;
            }
        }

        /// <summary>
        /// Clean up resources when the object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        /// <summary>
        /// Job for copying red channel to green and blue channels
        /// </summary>
        private struct SimpleJob : IJobParallelFor
        {
            public NativeArray<Color> Array;
            private Color _color;

            /// <summary>
            /// Execute the job for a single pixel
            /// </summary>
            /// <param name="i">Pixel index</param>
            public void Execute(int i)
            {
                _color.r = Array[i].r;
                _color.g = Array[i].r;
                _color.b = Array[i].r;
                Array[i] = _color;
            }
        }

        /// <summary>
        /// Job for rotating image based on device orientation
        /// </summary>
        private struct RotateJob : IJobParallelFor
        {
            public int Width, Height;
            public NativeArray<byte> Input;
            public NativeArray<byte> Output;
            public DeviceOrientation Orientation;

            private int x, y;

            /// <summary>
            /// Execute the rotation job for a single pixel
            /// </summary>
            /// <param name="i">Pixel index</param>
            public void Execute(int i)
            {
                switch (Orientation)
                {
                    case DeviceOrientation.LandscapeRight:
                        // don't rotate
                        Output[i] = Input[i];
                        break;
                    case DeviceOrientation.LandscapeLeft:
                        // rotate 180
                        Output[Width * Height - i - 1] = Input[i];
                        break;
                    case DeviceOrientation.Portrait:
                        // rotete 90 clockwise
                        x = i / Width;
                        y = i % Width;
                        Output[y * Height + Height - x - 1] = Input[i];
                        break;
                    case DeviceOrientation.PortraitUpsideDown:
                        // rotete 90 anticlockwise
                        x = i / Width;
                        y = i % Width;
                        Output[(Width - y - 1) * Height + x] = Input[i];
                        break;
                }
            }
        }

        /// <summary>
        /// Update device orientation for image rotation
        /// </summary>
        private void Update()
        {
            var newOrientation = Input.deviceOrientation;
            if (newOrientation != DeviceOrientation.FaceDown
                && newOrientation != DeviceOrientation.FaceUp
                && newOrientation != DeviceOrientation.Unknown)
            {
                _currentOrientation = newOrientation;
            }
            else
            {
                _currentOrientation = DeviceOrientation.Portrait;
            }

            _isPortraitOrientation = _currentOrientation is DeviceOrientation.Portrait or DeviceOrientation.PortraitUpsideDown;
        }

        /// <summary>
        /// Check if the current state is valid for operations
        /// </summary>
        /// <returns>True if state is valid, false otherwise</returns>
        private bool IsValidState()
        {
            return _buffers != null && _buffers.Count > 0;
        }

        /// <summary>
        /// Calculate resize coefficient based on orientation and configuration
        /// </summary>
        /// <param name="req">Texture requirement</param>
        /// <param name="config">Camera configuration</param>
        /// <returns>Resize coefficient</returns>
        private float CalculateResizeCoefficient(WASVPSTextureRequirement req, XRCameraConfiguration config)
        {
            return _isPortraitOrientation 
                ? req.Height / (float)config.width 
                : req.Height / (float)config.height;
        }

        /// <summary>
        /// Process color channels by copying red to green and blue
        /// </summary>
        /// <param name="requirement">Texture requirement</param>
        private void ProcessColorChannels(WASVPSTextureRequirement requirement)
        {
            var pixelCount = requirement.Width * requirement.Height;
            
            if (!_colorArray.IsCreated || _colorArray.Length != pixelCount)
            {
                if (_colorArray.IsCreated)
                    _colorArray.Dispose();
                _colorArray = new NativeArray<Color>(pixelCount, Allocator.Persistent);
            }

            var pixels = _texture.GetPixels();
            _colorArray.CopyFrom(pixels);
            
            _job.Array = _colorArray;
            var handle = _job.Schedule(pixelCount, 64);
            handle.Complete();

            if (handle.IsCompleted)
            {
                _texture.SetPixels(_colorArray.ToArray());
                _texture.Apply();
            }
        }
    }
}