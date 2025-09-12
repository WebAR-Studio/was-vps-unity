using System;

namespace WASVPS
{
    /// <summary>
    /// Represents the status of GPS service
    /// </summary>
    public enum GPSStatus 
    { 
        /// <summary>
        /// GPS service is initializing
        /// </summary>
        Initializing, 
        
        /// <summary>
        /// GPS service is running and providing data
        /// </summary>
        Running, 
        
        /// <summary>
        /// GPS service has failed or is unavailable
        /// </summary>
        Failed 
    }

    /// <summary>
    /// Contains GPS location data including coordinates, altitude, accuracy and timestamp
    /// </summary>
    [Serializable]
    public class GPSData
    {
        /// <summary>
        /// Current status of GPS service
        /// </summary>
        public GPSStatus Status { get; set; }
        
        /// <summary>
        /// Latitude coordinate in decimal degrees
        /// </summary>
        public double Latitude { get; set; }
        
        /// <summary>
        /// Longitude coordinate in decimal degrees
        /// </summary>
        public double Longitude { get; set; }
        
        /// <summary>
        /// Altitude above sea level in meters
        /// </summary>
        public double Altitude { get; set; }
        
        /// <summary>
        /// GPS accuracy in meters
        /// </summary>
        public double Accuracy { get; set; }
        
        /// <summary>
        /// Timestamp when GPS data was obtained (Unix timestamp)
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of GPSData with default values
        /// </summary>
        public GPSData()
        {
            Status = GPSStatus.Initializing;
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
            Accuracy = 0.0;
            Timestamp = 0.0;
        }

        /// <summary>
        /// Initializes a new instance of GPSData with specified values
        /// </summary>
        /// <param name="latitude">Latitude coordinate in decimal degrees</param>
        /// <param name="longitude">Longitude coordinate in decimal degrees</param>
        /// <param name="altitude">Altitude above sea level in meters</param>
        /// <param name="accuracy">GPS accuracy in meters</param>
        /// <param name="timestamp">Timestamp when GPS data was obtained</param>
        public GPSData(double latitude, double longitude, double altitude, double accuracy, double timestamp)
        {
            Status = GPSStatus.Running;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Accuracy = accuracy;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Returns a string representation of the GPS data
        /// </summary>
        /// <returns>Formatted string with GPS coordinates and status</returns>
        public override string ToString()
        {
            return $"GPSData: Lat={Latitude:F6}, Lon={Longitude:F6}, Alt={Altitude:F2}m, Acc={Accuracy:F2}m, Status={Status}";
        }
    }

    /// <summary>
    /// Contains compass heading data including direction, accuracy and timestamp
    /// </summary>
    [Serializable]
    public class CompassData
    {
        /// <summary>
        /// Current status of compass service
        /// </summary>
        public GPSStatus Status { get; set; }
        
        /// <summary>
        /// Compass accuracy in degrees
        /// </summary>
        public float Accuracy { get; set; }
        
        /// <summary>
        /// Magnetic heading in degrees (0-360)
        /// </summary>
        public float Heading { get; set; }
        
        /// <summary>
        /// Timestamp when compass data was obtained (Unix timestamp)
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of CompassData with default values
        /// </summary>
        public CompassData()
        {
            Status = GPSStatus.Initializing;
            Heading = 0.0f;
            Accuracy = 0.0f;
            Timestamp = 0.0;
        }

        /// <summary>
        /// Initializes a new instance of CompassData with specified values
        /// </summary>
        /// <param name="heading">Magnetic heading in degrees (0-360)</param>
        /// <param name="accuracy">Compass accuracy in degrees</param>
        /// <param name="timestamp">Timestamp when compass data was obtained</param>
        public CompassData(float heading, float accuracy, double timestamp)
        {
            Status = GPSStatus.Running;
            Heading = NormalizeHeading(heading);
            Accuracy = accuracy;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Normalizes heading to 0-360 degrees range
        /// </summary>
        /// <param name="heading">Raw heading value</param>
        /// <returns>Normalized heading in 0-360 degrees</returns>
        private static float NormalizeHeading(float heading)
        {
            while (heading < 0) heading += 360;
            while (heading >= 360) heading -= 360;
            return heading;
        }

        /// <summary>
        /// Returns a string representation of the compass data
        /// </summary>
        /// <returns>Formatted string with compass heading and status</returns>
        public override string ToString()
        {
            return $"CompassData: Heading={Heading:F1}°, Accuracy={Accuracy:F1}°, Status={Status}";
        }
    }
}