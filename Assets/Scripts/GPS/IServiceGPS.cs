namespace WASVPS
{
    /// <summary>
    /// Interface for GPS and compass services providing location and orientation data
    /// </summary>
    public interface IServiceGPS
    {
        /// <summary>
        /// Retrieves the current GPS location data including latitude, longitude, altitude and accuracy
        /// </summary>
        /// <returns>GPSData object containing current location information or null if GPS is unavailable</returns>
        GPSData GetGPSData();
        
        /// <summary>
        /// Retrieves the current compass orientation data including magnetic heading and true heading
        /// </summary>
        /// <returns>CompassData object containing current orientation information or null if compass is unavailable</returns>
        CompassData GetCompassData();
        
        /// <summary>
        /// Enables or disables the GPS and compass services
        /// </summary>
        /// <param name="enable">True to start GPS/compass services, false to stop them</param>
        void SetEnable(bool enable);
    }
}