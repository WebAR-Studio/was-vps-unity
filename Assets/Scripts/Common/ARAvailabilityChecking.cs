using System.Collections;
using UnityEngine.XR.ARFoundation;

namespace WASVPS
{
    /// <summary>
    /// Utility class for checking AR (Augmented Reality) availability on the current device.
    /// This class provides asynchronous checking functionality to determine if AR features
    /// are supported and ready to use on the target platform.
    /// </summary>
    public static class ARAvailabilityChecking
    {
        /// <summary>
        /// Initiates an asynchronous check for AR availability on the device.
        /// This method continuously monitors the AR session state until a definitive
        /// result is obtained (either supported or unsupported).
        /// </summary>
        /// <param name="onStatusReceived">Callback function that will be invoked with the result.
        /// True indicates AR is available and ready, false indicates AR is not supported.</param>
        /// <returns>IEnumerator for coroutine execution</returns>
        public static IEnumerator StartChecking(System.Action<bool> onStatusReceived)
        {
            // Continue checking until we get a definitive result
            while (true)
            {
                // Evaluate the current state of the AR session
                switch (ARSession.state)
                {
                    // Initial states that require availability checking
                    case ARSessionState.None:
                    case ARSessionState.CheckingAvailability:
                        // Trigger availability check and wait for completion
                        yield return ARSession.CheckAvailability();
                        // Continue the loop to check the new state
                        continue;
                    
                    // State indicating AR support needs to be installed
                    case ARSessionState.NeedsInstall:
                        // Attempt to install AR support and wait for completion
                        yield return ARSession.Install();
                        // Continue the loop to check the new state
                        continue;
                    
                    // State indicating AR is not supported on this device
                    case ARSessionState.Unsupported:
                        // Notify caller that AR is not available
                        onStatusReceived?.Invoke(false);
                        // Exit the checking loop
                        yield break;
                    
                    // States indicating AR is ready for use
                    case ARSessionState.Ready:
                    case ARSessionState.SessionTracking:
                        // Notify caller that AR is available and ready
                        onStatusReceived?.Invoke(true);
                        // Exit the checking loop
                        yield break;
                    
                    // Transitional states that require waiting
                    case ARSessionState.Installing:
                    case ARSessionState.SessionInitializing:
                        // Wait one frame before checking again
                        yield return null;
                        // Continue the loop to check the new state
                        continue;
                }
            }
        }
    }
}