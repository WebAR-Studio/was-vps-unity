using System;
using UnityEngine;
using Newtonsoft.Json;
using UuidExtensions;

namespace WASVPS.JSON
{
    /// <summary>
    /// Provides functionality for serialization and deserialization of JSON data for VPS (Visual Positioning System) requests and responses.
    /// Handles data collection from various service providers and converts them to appropriate JSON structures.
    /// </summary>
    public static class DataCollector
    {
        /// <summary>
        /// Collects data from various service providers and creates a request structure for VPS localization.
        /// </summary>
        /// <param name="provider">The service provider containing tracking, GPS, camera, and session information.</param>
        /// <param name="locationIds">Array of location IDs to include in the request.</param>
        /// <returns>A <see cref="RequestStruct"/> containing all collected data for VPS request.</returns>
        public static RequestStruct CollectData(ServiceProvider provider, string[] locationIds)
        {
            var pose = new Pose();
            var tracking = provider.GetTracking().GetLocalTracking();

            pose.position = tracking.Position;
            pose.rotation = tracking.Rotation;

            var gps = provider.GetGps();
            RequestLocation requestLocation = null;
            if (gps != null)
            {
                var gpsData = gps.GetGPSData();
                var gpsCompass = gps.GetCompassData();

                if (gpsData.Status == GPSStatus.Running && gpsCompass.Status == GPSStatus.Running && gpsData.Accuracy < 1000)
                {
                    var requstGps = new RequstGps
                    {
                        latitude = gpsData.Latitude,
                        longitude = gpsData.Longitude,
                        altitude = gpsData.Altitude,
                        accuracy = gpsData.Accuracy,
                        timestamp = gpsData.Timestamp
                    };
                    var requestCompass = new RequestCompass
                    {
                        heading = gpsCompass.Heading,
                        accuracy = gpsCompass.Accuracy,
                        timestamp = gpsCompass.Timestamp
                    };

                    requestLocation = new RequestLocation()
                    {
                        gps = requstGps,
                        compass = requestCompass
                    };
                }
            }

            var focalPixelLength = provider.GetCamera().GetFocalPixelLength();
            var principalPoint = provider.GetCamera().GetPrincipalPoint();

            const string userIdKey = "user_id";
            if (!PlayerPrefs.HasKey(userIdKey))
            {
                PlayerPrefs.SetString(userIdKey, Uuid7.Guid().ToString());
            }

            var attrib = new RequestAttributes
            {
                locationIds = locationIds,
                sessionId = provider.GetSessionInfo().Id,
                userId = PlayerPrefs.GetString(userIdKey),
                timestamp = new DateTimeOffset(DateTime.Now).ToUniversalTime().ToUnixTimeMilliseconds() / 1000d,

                location = requestLocation,

                clientCoordinateSystem = "unity",

                trackingPose = new TrackingPose
                {
                    x = pose.position.x,
                    y = pose.position.y,
                    z = pose.position.z,
                    rx = pose.rotation.eulerAngles.x,
                    ry = pose.rotation.eulerAngles.y,
                    rz = pose.rotation.eulerAngles.z
                },

                intrinsics = new Intrinsics
                {
                    width = provider.GetTextureRequirement().Width,
                    height = provider.GetTextureRequirement().Height,

                    fx = focalPixelLength.x,
                    fy = focalPixelLength.y,
                    cx = principalPoint.x,
                    cy = principalPoint.y
                }
            };


            var data = new RequestData
            {
                attributes = attrib
            };

            var communicationStruct = new RequestStruct
            {
                data = data
            };

            return communicationStruct;
        }

        /// <summary>
        /// Serializes a request structure to JSON format for transmission to VPS server.
        /// </summary>
        /// <param name="meta">The request structure to serialize.</param>
        /// <returns>A JSON string representation of the request structure.</returns>
        public static string Serialize(RequestStruct meta)
        {
            var json = JsonConvert.SerializeObject(meta);

            VPSLogger.LogFormat(LogLevel.DEBUG, "Json to send: {0}", json);
            return json;
        }

        /// <summary>
        /// Deserializes a JSON response from the VPS server into a LocationState object.
        /// Handles different HTTP response codes and error scenarios.
        /// </summary>
        /// <param name="json">The JSON string response from the server.</param>
        /// <param name="resultCode">The HTTP result code (200, 422, 404, 500). Defaults to 200.</param>
        /// <returns>A <see cref="LocationState"/> object containing the parsed response or error information.</returns>
        public static LocationState Deserialize(string json, long resultCode = 200)
        {
            LocationState request = new LocationState();
            switch(resultCode)
            {
                case 200:
                    {
                        var communicationStruct = JsonConvert.DeserializeObject<ResponseStruct>(json);
                        request.Status = GetStatusFromString(communicationStruct.data.status);

                        if (request.Status == LocalisationStatus.VPS_READY)
                        {
                            request.Error = null;
                            request.Localisation = new LocalisationResult
                            {
                                VpsPosition = new Vector3(communicationStruct.data.attributes.vpsPose.x,
                                            communicationStruct.data.attributes.vpsPose.y,
                                            communicationStruct.data.attributes.vpsPose.z),
                                VpsRotation = new Vector3(communicationStruct.data.attributes.vpsPose.rx,
                                            communicationStruct.data.attributes.vpsPose.ry,
                                            communicationStruct.data.attributes.vpsPose.rz),
                                TrackingPosition = new Vector3(communicationStruct.data.attributes.trackingPose.x,
                                            communicationStruct.data.attributes.trackingPose.y,
                                            communicationStruct.data.attributes.trackingPose.z),
                                TrackingRotation = new Vector3(communicationStruct.data.attributes.trackingPose.rx,
                                            communicationStruct.data.attributes.trackingPose.ry,
                                            communicationStruct.data.attributes.trackingPose.rz),
                                LocationId = communicationStruct.data.attributes.locationId
                            };
                        }
                        else
                        {
                            request.Localisation = null;
                            request.Error = new ErrorInfo(ErrorCode.LOCALISATION_FAIL, communicationStruct.data.statusDescription);
                        }
                        break;
                    }
                case 422:
                    {
                        var failDetail = JsonConvert.DeserializeObject<FailDetails>(json);
                        request.Localisation = null;

                        var errorField = "";
                        for (int i = 0; i < failDetail.detail[0].loc.Length; i++)
                        {
                            errorField += failDetail.detail[0].loc[i];
                            if (i != failDetail.detail[0].loc.Length - 1)
                                errorField += "/";
                        }
                        request.Error = new ErrorInfo()
                        {
                            Code = ErrorCode.VALIDATION_ERROR,
                            Message = failDetail.detail[0].msg,
                            JsonErrorField = errorField
                        };
                        break;
                    }
                case 500:
                case 404:
                    {
                        var failDetail = JsonConvert.DeserializeObject<FailStringDetail>(json);
                        request.Localisation = null;

                        request.Error = new ErrorInfo()
                        {
                            Code = ErrorCode.SERVER_INTERNAL_ERROR,
                            Message = failDetail.detail
                        };
                    }
                    break;
            }

            return request;
        }

        /// <summary>
        /// Converts a string status from the server response to a LocalisationStatus enum value.
        /// </summary>
        /// <param name="status">The status string from the server response ("done", "fail", etc.).</param>
        /// <returns>The corresponding <see cref="LocalisationStatus"/> enum value.</returns>
        private static LocalisationStatus GetStatusFromString(string status)
        {
            return status switch
            {
                "done" => LocalisationStatus.VPS_READY,
                "fail" => LocalisationStatus.GPS_ONLY,
                _ => LocalisationStatus.GPS_ONLY
            };
        }
    }
}