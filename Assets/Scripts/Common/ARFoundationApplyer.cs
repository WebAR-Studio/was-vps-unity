using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace WASVPS
{
    public class ARFoundationApplyer : MonoBehaviour
    {
        private XROrigin _xrOrigin;

        [Tooltip("Max distance for interpolation")]
        public float MaxInterpolationDistance = 5;

        [Tooltip("Interpolation speed")]
        public float LerpSpeed = 2.0f;

        [Tooltip("Override only North direction or entire phone rotation")]
        public bool RotateOnlyY = true;

        [Tooltip("Freeze Y position")]
        public bool FreezeYPos = false;

        /// <summary>
        /// Initializes the ARFoundation applyer by finding the XROrigin component
        /// </summary>
        private void Start()
        {
            _xrOrigin = FindObjectOfType<XROrigin>();
            if (_xrOrigin == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "ARSessionOrigin is not found");
            }
        }

        /// <summary>
        /// Applies the VPS transform to the ARFoundation localization and returns the adjusted result
        /// </summary>
        /// <param name="localisation">The localization result from VPS</param>
        /// <param name="instantly">If true, applies the transform immediately without interpolation</param>
        /// <returns>The adjusted VPS transform result</returns>
        public LocalisationResult ApplyVpsTransform(LocalisationResult localisation, bool instantly = false)
        {
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization position: {0}", localisation.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization rotation: {0}", localisation.VpsRotation);
            var correctedResult = (LocalisationResult)localisation.Clone();

            if (FreezeYPos)
                correctedResult.VpsPosition.y = 0;

            // Calculate camera offset at the time of VPS request to compensate for camera movement
            var cameraOffset = _xrOrigin.Camera.transform.localPosition - localisation.TrackingPosition;

            // Adjust VPS position/rotation by subtracting tracking values since XROrigin already contains them
            correctedResult.VpsPosition -= correctedResult.TrackingPosition;
            correctedResult.VpsRotation -= correctedResult.TrackingRotation;

            StopAllCoroutines();
            StartCoroutine(UpdatePosAndRot(correctedResult.VpsPosition, correctedResult.VpsRotation, cameraOffset, instantly));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization position: {0}", correctedResult.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization rotation: {0}", correctedResult.VpsRotation);

            return correctedResult;
        }

        /// <summary>
        /// Updates the XROrigin position and rotation with smooth interpolation
        /// </summary>
        /// <param name="newPosition">Target position for the XROrigin</param>
        /// <param name="newRotation">Target rotation for the XROrigin</param>
        /// <param name="cameraOffset">Camera offset from the tracking position</param>
        /// <param name="instantly">If true, applies the transform immediately without interpolation</param>
        /// <returns>Coroutine that handles the smooth transition</returns>
        private IEnumerator UpdatePosAndRot(Vector3 newPosition, Vector3 newRotation, Vector3 cameraOffset, bool instantly)
        {
            if (RotateOnlyY)
            {
                newRotation.x = 0;
                newRotation.z = 0;
            }

            var startPosition = _xrOrigin.transform.position;
            var startRotation = _xrOrigin.transform.rotation;

            _xrOrigin.transform.position = newPosition;
            // Reset parent rotation to identity before applying VPS rotation around camera
            _xrOrigin.transform.rotation = Quaternion.identity;
            // Calculate camera world position without offset to use as rotation center
            var cameraPosWithoutOffet = _xrOrigin.Camera.transform.position - cameraOffset;
            // Apply VPS rotation by rotating XROrigin around camera position
            RotateAroundThreeAxes(newRotation, cameraPosWithoutOffet);

            var targetPosition = _xrOrigin.transform.position;
            var targetRotation = _xrOrigin.transform.rotation;

            // Skip interpolation if distance is too large or instant mode is requested
            if (Vector3.Distance(startPosition, targetPosition) > MaxInterpolationDistance || instantly)
                yield break;

            // Smoothly interpolate from current position to target position
            float interpolant = 0;
            while (interpolant < 1)
            {
                interpolant += LerpSpeed * Time.deltaTime;
                _xrOrigin.transform.position = Vector3.Lerp(startPosition, targetPosition, interpolant);
                _xrOrigin.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, interpolant);
                yield return null;
            }
        }

        /// <summary>
        /// Rotates the XROrigin around the camera position on all three axes
        /// </summary>
        /// <param name="rotateVector">Rotation vector containing angles for X, Y, and Z axes</param>
        /// <param name="cameraPosWithoutOffet">Camera position without offset to use as rotation center</param>
        private void RotateAroundThreeAxes(Vector3 rotateVector, Vector3 cameraPosWithoutOffet)
        {
            _xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.forward, rotateVector.z);
            _xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.right, rotateVector.x);
            _xrOrigin.transform.RotateAround(cameraPosWithoutOffet, Vector3.up, rotateVector.y);
        }

        /// <summary>
        /// Resets the tracking by stopping all coroutines and resetting XROrigin and camera transforms to default values
        /// </summary>
        public void ResetTracking()
        {
            StopAllCoroutines();
            if (_xrOrigin == null) return;
            
            _xrOrigin.transform.position = Vector3.zero;
            _xrOrigin.transform.rotation = Quaternion.identity;
            _xrOrigin.Camera.transform.position = Vector3.zero;
            _xrOrigin.Camera.transform.rotation = Quaternion.identity;
        }
    }
}