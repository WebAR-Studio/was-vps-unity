using UnityEngine;

namespace WASVPS
{
    public class ServiceProvider : MonoBehaviour
    {
        [Header("AR Foundation")]
        [Tooltip("To apply resulting localization")]
        public ARFoundationApplyer arFoundationApplyer;

        [Header("Camera Settings")]
        [Tooltip("Target photo resolution")]
        public Vector2Int desiredResolution = new Vector2Int(540, 960);
        
        [Tooltip("Texture format for camera frames")]
        public TextureFormat format = TextureFormat.R8;

        // Core service dependencies
        private IWASVPSCamera _camera;
        private IServiceGPS _gps;
        private IWASVPSTracking _tracking;
        private WASVPSTextureRequirement _textureRequirement;
        private SessionInfo _currentSession;

        /// <summary>
        /// Gets the camera service instance
        /// </summary>
        /// <returns>Camera service or null if not initialized</returns>
        public IWASVPSCamera GetCamera() => _camera;

        /// <summary>
        /// Gets the texture requirements for camera frames
        /// </summary>
        /// <returns>Texture requirements based on inspector settings</returns>
        public WASVPSTextureRequirement GetTextureRequirement() => _textureRequirement;

        private void Awake()
        {
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Get required components from the same GameObject
            _camera = GetComponent<IWASVPSCamera>();
            _tracking = GetComponent<IWASVPSTracking>();
            
            // Create texture requirements based on inspector settings
            _textureRequirement = new WASVPSTextureRequirement(desiredResolution.x, desiredResolution.y, format);
        }

        /// <summary>
        /// Initializes GPS service if available
        /// </summary>
        /// <param name="useGps">Whether to enable GPS functionality</param>
        public void InitGps(bool useGps)
        {
            // GPS is optional and initialized separately from other services
            _gps = useGps ? GetComponent<IServiceGPS>() : null;
        }

        /// <summary>
        /// Gets the GPS service instance
        /// </summary>
        /// <returns>GPS service or null if not initialized</returns>
        public IServiceGPS GetGps() => _gps;

        /// <summary>
        /// Gets the tracking service instance
        /// </summary>
        /// <returns>Tracking service or null if not initialized</returns>
        public IWASVPSTracking GetTracking() => _tracking;

        /// <summary>
        /// Gets the AR Foundation applyer instance
        /// </summary>
        /// <returns>AR Foundation applyer assigned in inspector</returns>
        public ARFoundationApplyer GetARFoundationApplyer() => arFoundationApplyer;

        /// <summary>
        /// Gets the current session information
        /// </summary>
        /// <returns>Current session info or null if not initialized</returns>
        public SessionInfo GetSessionInfo() => _currentSession;

        /// <summary>
        /// Resets the current session with a new unique ID
        /// </summary>
        public void ResetSessionId()
        {
            // Create new session with unique ID for tracking localization attempts
            _currentSession = new SessionInfo();
            VPSLogger.Log(LogLevel.VERBOSE, $"New session: {_currentSession.Id}");
        }

        /// <summary>
        /// Gets the total number of localization responses
        /// </summary>
        /// <returns>Total responses count or 0 if no session</returns>
        public int GetTotalResponsesCount()
        {
            return _currentSession?.ResponsesCount ?? 0;
        }

        /// <summary>
        /// Gets the number of successful localizations
        /// </summary>
        /// <returns>Success count or 0 if no session</returns>
        public int GetSuccessLocalizationCount()
        {
            return _currentSession?.SuccessLocalizationCount ?? 0;
        }

        /// <summary>
        /// Gets the number of failed localizations
        /// </summary>
        /// <returns>Fail count or 0 if no session</returns>
        public int GetFailLocalizationCount()
        {
            return _currentSession?.FailLocalizationCount ?? 0;
        }

        /// <summary>
        /// Gets the number of successful localizations in a row
        /// </summary>
        /// <returns>Success in row count or 0 if no session</returns>
        public int GetSuccessLocalizationInRow()
        {
            return _currentSession?.SuccessLocalizationInRow ?? 0;
        }

        /// <summary>
        /// Gets the success rate as a percentage
        /// </summary>
        /// <returns>Success rate percentage or 0 if no responses</returns>
        public float GetSuccessRate()
        {
            if (_currentSession?.ResponsesCount == 0)
                return 0f;
            
            return (float)_currentSession.SuccessLocalizationCount / _currentSession.ResponsesCount * 100f;
        }
    }
}
