using Unity.XR.CoreUtils;
using UnityEngine;

namespace WASVPS
{
    /// <summary>
    /// AR Foundation implementation of tracking interface for VPS (Visual Positioning System)
    /// </summary>
    public class ARFoundationTracking : MonoBehaviour, IWASVPSTracking
    {
        private Transform _arCamera;
        private WASVPSTrackingData _trackingData = new WASVPSTrackingData();


        private void Awake()
        {
            var xrOrigin = FindObjectOfType<XROrigin>();
            if (xrOrigin?.Camera != null)
            {
                _arCamera = xrOrigin.Camera.transform;
            }
            else
            {
                VPSLogger.Log(LogLevel.ERROR, "XROrigin or Camera is not available for tracking");
            }
        }

        /// <summary>
        /// Updates the tracking data with current camera position and rotation
        /// </summary>
        private void UpdateTrackingData()
        {
            if (_arCamera != null)
            {
                _trackingData.Position = _arCamera.localPosition;
                _trackingData.Rotation = _arCamera.localRotation;
            }
        }

        /// <summary>
        /// Gets the current tracking data including position, rotation and location ID
        /// </summary>
        /// <returns>Current tracking data structure</returns>
        public WASVPSTrackingData GetLocalTracking()
        {
            if (_arCamera == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available, returning last known tracking data");
                return _trackingData;
            }
            
            UpdateTrackingData();
            return _trackingData;
        }

        /// <summary>
        /// Sets the location ID for tracking data
        /// </summary>
        /// <param name="locationId">The location identifier to set</param>
        /// <returns>True if location was changed, false if it was already set to the same value</returns>
        public bool Localize(string locationId)
        {
            if (_trackingData.LocationId == locationId) return false;
            _trackingData.LocationId = locationId;
            return true;
        }

        /// <summary>
        /// Resets the tracking data to initial state
        /// </summary>
        public void ResetTracking()
        {
            _trackingData = new WASVPSTrackingData();
        }

        /// <summary>
        /// Checks if the tracking system is currently localized to a specific location
        /// </summary>
        /// <returns>True if localized, false otherwise</returns>
        public bool IsLocalized()
        {
            return !string.IsNullOrEmpty(_trackingData.LocationId);
        }
    }
}
