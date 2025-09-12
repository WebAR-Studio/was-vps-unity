namespace WASVPS
{
    public interface IWASVPSTracking
    {
        /// <summary>
        /// Retrieves the current tracking data from the device.
        /// Data is updated only when explicitly requested, not automatically.
        /// </summary>
        /// <returns>Current tracking data containing position, rotation and other tracking information.</returns>
        WASVPSTrackingData GetLocalTracking();
        
        /// <summary>
        /// Initiates localization process for the specified location.
        /// Sets the localization flag to true and attempts to localize the device.
        /// </summary>
        /// <param name="locationId">Unique identifier of the location to localize to.</param>
        /// <returns>True if the location ID was successfully changed, false otherwise.</returns>
        bool Localize(string locationId);
        
        /// <summary>
        /// Resets the current tracking state to initial values.
        /// Clears all tracking data and returns the system to an uninitialized state.
        /// </summary>
        void ResetTracking();
        
        /// <summary>
        /// Checks if the device has been successfully localized.
        /// Indicates whether the tracking system has established a valid position within a known location.
        /// </summary>
        /// <returns>True if the device is currently localized, false otherwise.</returns>
        bool IsLocalized();
    }
}
