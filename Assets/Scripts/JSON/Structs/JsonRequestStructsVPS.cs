using System;
using Newtonsoft.Json;
using UnityEngine;

namespace WASVPS.JSON
{
    /// <summary>
    /// Root structure for VPS request containing all necessary data
    /// </summary>
    [Serializable]
    public class RequestStruct
    {
        public RequestData data;
    }

    /// <summary>
    /// Container for request attributes and metadata
    /// </summary>
    [Serializable]
    public class RequestData
    {
        public RequestAttributes attributes;
    }

    /// <summary>
    /// Main attributes for VPS localization request including location, pose, and camera data
    /// </summary>
    [Serializable]
    public class RequestAttributes
    {
        [JsonProperty("location_ids")]
        public string[] locationIds;
        [JsonProperty("session_id")]
        public string sessionId;
        [JsonProperty("user_id")]
        public string userId;
        public double timestamp;
        public RequestLocation location;
        [JsonProperty("client_coordinate_system")]
        public string clientCoordinateSystem;
        [JsonProperty("tracking_pose")]
        public TrackingPose trackingPose;
        public Intrinsics intrinsics;
    }

    /// <summary>
    /// Location data including GPS coordinates and compass heading
    /// </summary>
    [Serializable]
    public class RequestLocation
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RequstGps gps;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public RequestCompass compass;
    }

    /// <summary>
    /// GPS location data with accuracy and timestamp information
    /// </summary>
    [Serializable]
    public class RequstGps
    {
        public double latitude;
        public double longitude;
        public double altitude;
        public double accuracy;
        public double timestamp;
    }

    /// <summary>
    /// 6DOF tracking pose containing position (x,y,z) and rotation (rx,ry,rz) data
    /// </summary>
    [Serializable]
    public class TrackingPose
    {
        public float x;
        public float y;
        public float z;

        public float rx;
        public float ry;
        public float rz;
    }

    /// <summary>
    /// Compass heading data with accuracy and timestamp
    /// </summary>
    [Serializable]
    public class RequestCompass
    {
        public float heading;
        public float accuracy;
        public double timestamp;
    }

    /// <summary>
    /// Camera intrinsic parameters for image processing and 3D reconstruction
    /// </summary>
    [Serializable]
    public class Intrinsics
    {
        public int width;
        public int height;

        public float fx;
        public float fy;
        public float cx;
        public float cy;
    }
}
