using UnityEngine;

[DisallowMultipleComponent]
public class NavWorldSpaceFaceCamera : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private bool _useMainCamera = true;

    /// <summary>
    /// Assigns explicit camera used by billboard rotation.
    /// </summary>
    /// <param name="targetCamera">Camera that view should face.</param>
    public void SetTargetCamera(Camera targetCamera)
    {
        _targetCamera = targetCamera;
    }

    private void LateUpdate()
    {
        var cameraToFace = ResolveCamera();
        if (cameraToFace == null)
        {
            return;
        }

        var cameraTransform = cameraToFace.transform;
        transform.LookAt(
            transform.position + cameraTransform.rotation * Vector3.forward,
            cameraTransform.rotation * Vector3.up);
    }

    private Camera ResolveCamera()
    {
        if (_targetCamera != null)
        {
            return _targetCamera;
        }

        if (!_useMainCamera)
        {
            return null;
        }

        return Camera.main;
    }
}
