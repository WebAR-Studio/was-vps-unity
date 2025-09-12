using System.Collections;
using UnityEngine;

namespace WASVPS
{
    /// <summary>
    /// Interface for VPS (Visual Positioning System) request handling
    /// Provides methods for sending visual positioning requests to the server
    /// </summary>
    public interface IWASVPSRequest
    {
        /// <summary>
        /// Sets the URL endpoint for VPS requests
        /// </summary>
        /// <param name="url">The server URL where VPS requests will be sent</param>
        void SetUrl(string url);
        
        /// <summary>
        /// Sets the API key for VPS server authentication
        /// </summary>
        /// <param name="apiKey">The API key string for server authentication</param>
        void SetApiKey(string apiKey);
        
        /// <summary>
        /// Sends a VPS request with captured image and metadata
        /// </summary>
        /// <param name="image">The captured camera image as Texture2D</param>
        /// <param name="meta">Metadata string containing additional request information</param>
        /// <param name="callback">Callback function to execute when request completes</param>
        /// <returns>Coroutine enumerator for async request handling</returns>
        IEnumerator SendVpsRequest(Texture2D image, string meta, System.Action callback);
        
        /// <summary>
        /// Gets the current location state from the VPS system
        /// </summary>
        /// <returns>Current LocationState containing positioning information</returns>
        LocationState GetLocationState();
    }
}