namespace WASVPS
{
    /// <summary>
    /// Configuration settings for VPS (Visual Positioning System) localization.
    /// Controls timing, location targets, failure handling, and device orientation constraints.
    /// </summary>
    public class SettingsWASVPS
    {
        /// <summary>
        /// Array of location IDs where VPS can perform localization.
        /// Used by DataCollector to send location context to the server.
        /// </summary>
        public string[] LocationIds;
        
        /// <summary>
        /// Delay in seconds between localization requests when actively localizing.
        /// Prevents excessive server requests during continuous tracking.
        /// Default: 1 second
        /// </summary>
        public float LocalizationTimeout = 1;
        
        /// <summary>
        /// Delay in seconds between calibration requests when not yet localized.
        /// Longer delay allows for better initial positioning attempts.
        /// Default: 2.5 seconds
        /// </summary>
        public float CalibrationTimeout = 2.5f;
        
        /// <summary>
        /// Number of consecutive failed localizations before resetting the VPS session.
        /// When reached, a new session ID is generated and tracking is reset.
        /// </summary>
        public int FailsCountToResetSession;

        /// <summary>
        /// Maximum allowed X-axis rotation (pitch) in degrees for photo capture.
        /// Used by CheckTakePhotoConditions to ensure stable device orientation.
        /// </summary>
        public readonly float MaxAngleX = 30;
        
        /// <summary>
        /// Maximum allowed Z-axis rotation (roll) in degrees for photo capture.
        /// Used by CheckTakePhotoConditions to ensure stable device orientation.
        /// </summary>
        public readonly float MaxAngleZ = 30;

        /// <summary>
        /// API key for VPS server authentication.
        /// Used in x-vps-apikey header for server requests.
        /// </summary>
        public string ApiKey;

        /// <summary>
        /// Initializes VPS settings with required parameters.
        /// </summary>
        /// <param name="locationIds">Array of location IDs where VPS can localize</param>
        /// <param name="failsCountToResetSession">Number of failures before session reset</param>
        public SettingsWASVPS(string[] locationIds, int failsCountToResetSession)
        {
            LocationIds = locationIds;
            FailsCountToResetSession = failsCountToResetSession;
        }
    }
}