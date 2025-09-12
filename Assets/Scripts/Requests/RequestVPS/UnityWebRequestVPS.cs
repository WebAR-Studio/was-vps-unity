using System;
using System.Collections;
using System.IO;
using WASVPS.JSON;
using UnityEngine;
using UnityEngine.Networking;

namespace WASVPS
{
    /// <summary>
    /// Unity Web Request implementation for VPS (Visual Positioning System) server communication.
    /// Handles sending image data and metadata to VPS server for localization purposes.
    /// </summary>
    public class UnityWebRequestVPS : IWASVPSRequest
    {
        private string _serverUrl;
        private string _apiKey;
        // api for one photo localisation
        private readonly string _apiPathSession = "vps/api/v3";

        private readonly int timeout = 0;

        private LocationState _locationState = new LocationState();

        #region Metrics

        private const string ImageVpsRequest = "ImageVPSRequest";

        #endregion

        /// <summary>
        /// Sets the VPS server URL for requests.
        /// </summary>
        /// <param name="url">The base URL of the VPS server.</param>
        public void SetUrl(string url)
        {
            _serverUrl = url;
        }

        /// <summary>
        /// Sets the API key for VPS server authentication.
        /// </summary>
        /// <param name="apiKey">The API key string for server authentication.</param>
        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        /// <summary>
        /// Sends a VPS request with image data and metadata to the server.
        /// </summary>
        /// <param name="image">The Texture2D image to send for localization.</param>
        /// <param name="meta">JSON metadata string containing additional information.</param>
        /// <param name="callback">Callback action to execute after request completion.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        public IEnumerator SendVpsRequest(Texture2D image, string meta, Action callback)
        {
            var uri = Path.Combine(_serverUrl, _apiPathSession).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            var form = new WWWForm();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            form.AddBinaryData("image", binaryImage, CreateFileName(), "image/jpeg");

            form.AddField("json", meta);

            // Log request details
            Debug.LogFormat("[VPS Request] Image size: {0} bytes", binaryImage.Length);
            Debug.LogFormat("[VPS Request] Image filename: {0}", CreateFileName());
            Debug.LogFormat("[VPS Request] JSON metadata being sent: {0}", meta);

            MetricsCollector.Instance.StartStopwatch(ImageVpsRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(ImageVpsRequest);

            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", ImageVpsRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(ImageVpsRequest));

            callback();
        }

        /// <summary>
        /// Gets the current location state containing the latest VPS response data.
        /// </summary>
        /// <returns>The current LocationState with status, error information, and localization result.</returns>
        public LocationState GetLocationState()
        {
            return _locationState;
        }

        /// <summary>
        /// Creates a filename for the image based on current date and time.
        /// </summary>
        /// <returns>A string filename in format "yyyy-MM-dd-HH-mm-ss.jpg".</returns>
        private static string CreateFileName()
        {
            var dateTime = DateTime.Now;
            var file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
            file += ".jpg";
            return file;
        }

        /// <summary>
        /// Converts a Texture2D image to a byte array in JPEG format.
        /// </summary>
        /// <param name="image">The Texture2D image to convert.</param>
        /// <returns>A byte array containing the JPEG-encoded image data, or null if conversion fails.</returns>
        private static byte[] GetByteArrayFromImage(Texture2D image)
        {
            var bytesOfImage = image.EncodeToJPG(100);
            return bytesOfImage;
        }

        /// <summary>
        /// Updates the latest response data with the provided status, error, and localization information.
        /// </summary>
        /// <param name="status">The localization status from the VPS response.</param>
        /// <param name="error">Error information if the request failed.</param>
        /// <param name="localisation">Localization result data if the request was successful.</param>
        private void UpdateLocalisationState(LocalisationStatus status, ErrorInfo error, LocalisationResult localisation)
        {
            _locationState.Status = status;
            _locationState.Error = error;
            _locationState.Localisation = localisation;
        }

        /// <summary>
        /// Sends an HTTP POST request to the specified URI with the provided form data.
        /// </summary>
        /// <param name="uri">The target URI for the request.</param>
        /// <param name="form">The WWWForm containing the data to send.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator SendRequest(string uri, WWWForm form)
        {
            using var www = UnityWebRequest.Post(uri, form);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.timeout = timeout;

            // Add API key header if available
            if (!string.IsNullOrEmpty(_apiKey))
            {
                www.SetRequestHeader("x-vps-apikey", _apiKey);
                Debug.LogFormat("[VPS Request] Added API key header: x-vps-apikey");
            }

            // Log request headers
            Debug.LogFormat("[VPS Request] Sending request to: {0}", uri);
            
            // Log form data info
            Debug.LogFormat("[VPS Request] Request form data size: {0} bytes", form.data.Length);
            Debug.LogFormat("[VPS Request] Request form data headers count: {0}", form.headers.Count);
            
            // Log form headers
            if (form.headers != null && form.headers.Count > 0)
            {
                Debug.Log("[VPS Request] Form headers:");
                foreach (var header in form.headers)
                {
                    Debug.LogFormat("[VPS Request]   {0}: {1}", header.Key, header.Value);
                }
            }
            
            www.SendWebRequest();
            while (!www.isDone)
            {
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                ErrorInfo errorStruct = new ErrorInfo(ErrorCode.NO_INTERNET, "Network is not available");
                UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                VPSLogger.LogFormat(LogLevel.ERROR, "Network error: {0}", errorStruct.LogDescription());
                yield break;
            }

            Debug.LogFormat("[VPS Response] Request finished with code: {0}", www.responseCode);

            string xRequestId;
            var responseHeader = www.GetResponseHeaders();
            if (responseHeader == null)
                xRequestId = "Response header is null";
            else
                xRequestId = "x-request-id: " + responseHeader["x-request-id"];

            var downloadHandler = www.downloadHandler;
            var response = downloadHandler == null ? "Response download handler is null" : downloadHandler.text;

            // Log response body
            Debug.LogFormat("[VPS Response] Response body: {0}", response);
            
            // Log all response headers
            if (responseHeader != null)
            {
                Debug.Log("[VPS Response] All response headers:");
                foreach (var header in responseHeader)
                {
                    Debug.LogFormat("[VPS Response]   {0}: {1}", header.Key, header.Value);
                }
            }

            Debug.LogFormat("[VPS Response] {0}\n{1}\n{2}",
                www.responseCode == 200
                    ? "Request Finished Successfully!"
                    : "Request Finished with error!", xRequestId, response);

            LocationState deserialized = null;
            try
            {
                deserialized = DataCollector.Deserialize(response, www.responseCode);
            }
            catch
            {
                var errorStruct = new ErrorInfo(ErrorCode.DESERIALIZED_ERROR, "Can't deserialize server response");
                VPSLogger.Log(LogLevel.ERROR, errorStruct.LogDescription());
                UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                yield break;
            }

            if (deserialized != null)
            {
                VPSLogger.LogFormat(LogLevel.DEBUG, "Server status {0}", deserialized.Status);
                _locationState = deserialized;
            }
            else
            {
                var errorStruct = new ErrorInfo(ErrorCode.DESERIALIZED_ERROR, "There is no data come from server");
                UpdateLocalisationState(LocalisationStatus.GPS_ONLY, errorStruct, null);
                VPSLogger.Log(LogLevel.ERROR, errorStruct.LogDescription());
                yield break;
            }
        }
    }
}