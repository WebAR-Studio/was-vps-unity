using Unity.Collections;
using UnityEngine;

namespace WASVPS
{
    public interface IWASVPSCamera
    {
        /// <summary>
        /// Initializes camera with texture requirements. Must be called first.
        /// Sets up buffers and prepares camera for image capture.
        /// </summary>
        /// <param name="requirements">Array of texture requirements (width, height, format)</param>
        void Init(WASVPSTextureRequirement[] requirements);
        
        /// <summary>
        /// Checks if camera is ready to capture images.
        /// </summary>
        /// <returns>True if camera is initialized and receiving data</returns>
        bool IsCameraReady();
        
        /// <summary>
        /// Gets current camera frame as Texture2D with processing applied.
        /// </summary>
        /// <param name="requirement">Texture requirement (width, height, format)</param>
        /// <returns>Processed camera frame or null if not ready</returns>
        Texture2D GetFrame(WASVPSTextureRequirement requirement);
        
        /// <summary>
        /// Gets camera focal length in pixels for computer vision.
        /// </summary>
        /// <returns>Focal length as Vector2 (fx, fy)</returns>
        Vector2 GetFocalPixelLength();
        
        /// <summary>
        /// Gets camera principal point coordinates in pixels.
        /// </summary>
        /// <returns>Principal point as Vector2 (cx, cy)</returns>
        Vector2 GetPrincipalPoint();
    }
}
