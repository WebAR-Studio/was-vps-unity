using System;
using Newtonsoft.Json;

namespace WASVPS.JSON
{
    /// <summary>
    /// Root structure for VPS response containing status and localization data
    /// </summary>
    [Serializable]
    public class ResponseStruct
    {
        public ResponseData data;
    }

    /// <summary>
    /// Container for response status and attributes
    /// </summary>
    [Serializable]
    public class ResponseData
    {
        public string status;
        [JsonProperty("status_description")]
        public string statusDescription;
        public ResponseAttributes attributes;
    }

    /// <summary>
    /// VPS localization response attributes including location and pose data
    /// </summary>
    [Serializable]
    public class ResponseAttributes
    {
        [JsonProperty("location_id")]
        public string locationId;
        public ResponseLocation location;
        [JsonProperty("tracking_pose")]
        public TrackingPose trackingPose;
        [JsonProperty("vps_pose")]
        public TrackingPose vpsPose;
    }

    /// <summary>
    /// Location data returned from VPS service
    /// </summary>
    [Serializable]
    public class ResponseLocation
    {
        public RequstGps gps;
        public RequestCompass compass;
    }

    /// <summary>
    /// Container for multiple failure details
    /// </summary>
    [Serializable]
    public class FailDetails
    {
        public FailDetail[] detail;
    }

    /// <summary>
    /// Individual failure detail with location, message, and error type
    /// </summary>
    [Serializable]
    public class FailDetail
    {
        public string[] loc;
        public string msg;
        public string type;
    }

    /// <summary>
    /// Simple failure detail with string message
    /// </summary>
    [Serializable]
    public class FailStringDetail
    {
        public string detail;
    }
}
