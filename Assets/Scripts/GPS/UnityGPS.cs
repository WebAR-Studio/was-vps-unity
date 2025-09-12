using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace WASVPS
{
    /// <summary>
    /// Unity implementation of GPS service for getting coordinates and compass data
    /// </summary>
    public class UnityGPS : MonoBehaviour, IServiceGPS
    {
        /// <summary>
        /// GPS data container
        /// </summary>
        private GPSData _gpsData;
        
        /// <summary>
        /// Compass data container
        /// </summary>
        private CompassData _compassData;

        /// <summary>
        /// Maximum initialization time in seconds
        /// </summary>
        private int _maxWait = 20;
        
        /// <summary>
        /// GPS update interval in seconds
        /// </summary>
        private const float TimeToUpdate = 3;
        
        /// <summary>
        /// GPS accuracy threshold in meters (0 = best accuracy)
        /// </summary>
        private const float GpsAccuracyThreshold = 0f;

        /// <summary>
        /// Service enabled state
        /// </summary>
        private bool _isServiceEnabled = true;

        /// <summary>
        /// Initialize GPS and compass data containers
        /// </summary>
        private void Start()
        {
            if (Application.isEditor)
                return;

            _gpsData = new GPSData();
            _compassData = new CompassData();
        }

        /// <summary>
        /// Start GPS service and continuously update location and compass data
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator StartGps()
        {
            if (Application.isEditor)
                yield break;

            // check gps available
            if (!Input.location.isEnabledByUser)
            {
                SetGpsStatusFailed("GPS is not available");
                yield break;
            }

            // start gps 
            Input.location.Start(GpsAccuracyThreshold, GpsAccuracyThreshold);
            Input.compass.enabled = true;

            // initialization
            while (Input.location.status == LocationServiceStatus.Initializing && _maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                _maxWait--;
            }

            // timeout exit
            if (_maxWait < 1)
            {
                SetGpsStatusFailed("GPS is timed out");
                yield break;
            }

            // check connection
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                SetGpsStatusFailed("GPS: Unable to determine device location");
                yield break;
            }
            else
            {
                while (Input.location.status == LocationServiceStatus.Running)
                {
                    UpdateGpsData();
                    UpdateCompassData();

                    yield return new WaitForSeconds(TimeToUpdate);
                }
            }
        }

        /// <summary>
        /// Stop GPS service and disable compass
        /// </summary>
        void StopGPS()
        {
            StopAllCoroutines();
            
            if (Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }
            
            Input.compass.enabled = false;
        }

        /// <summary>
        /// Get current compass data
        /// </summary>
        /// <returns>Current compass data or null if not initialized</returns>
        public CompassData GetCompassData() => _compassData;

        /// <summary>
        /// Get current GPS data
        /// </summary>
        /// <returns>Current GPS data or null if not initialized</returns>
        public GPSData GetGPSData() => _gpsData;

        /// <summary>
        /// Enable or disable GPS service
        /// </summary>
        /// <param name="enable">True to enable GPS service, false to disable</param>
        public void SetEnable(bool enable)
        {
            _isServiceEnabled = enable;
            if (_isServiceEnabled)
            {
#if UNITY_ANDROID
                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                    Permission.RequestUserPermission(Permission.FineLocation);
#endif
                StartCoroutine(StartGps());
            }
            else
            {
                StopGPS();
            }
        }

        /// <summary>
        /// Set GPS status to failed and log error message
        /// </summary>
        /// <param name="errorMessage">Error message to log</param>
        private void SetGpsStatusFailed(string errorMessage)
        {
            if (_gpsData != null)
            {
                _gpsData.Status = GPSStatus.Failed;
            }
            if (_compassData != null)
            {
                _compassData.Status = GPSStatus.Failed;
            }
            VPSLogger.Log(LogLevel.ERROR, errorMessage);
        }

        /// <summary>
        /// Update GPS data from Unity Input.location
        /// </summary>
        private void UpdateGpsData()
        {
            if (_gpsData == null) return;
            
            _gpsData.Status = GPSStatus.Running;
            _gpsData.Latitude = Input.location.lastData.latitude;
            _gpsData.Longitude = Input.location.lastData.longitude;
            _gpsData.Altitude = Input.location.lastData.altitude;
            _gpsData.Accuracy = Input.location.lastData.horizontalAccuracy;
            _gpsData.Timestamp = Input.location.lastData.timestamp;
        }

        /// <summary>
        /// Update compass data from Unity Input.compass
        /// </summary>
        private void UpdateCompassData()
        {
            if (_compassData == null) return;
            
            _compassData.Status = GPSStatus.Running;
            _compassData.Heading = Input.compass.trueHeading;
            _compassData.Accuracy = Input.compass.headingAccuracy;
            _compassData.Timestamp = Input.compass.timestamp;
        }
    }
}
