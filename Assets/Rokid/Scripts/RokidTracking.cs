using UnityEngine;
using WASVPS;

public class RokidTracking : MonoBehaviour, IWASVPSTracking
{
    [SerializeField] private Transform _camera;
    private bool _isLocalized = false;

    public WASVPSTrackingData GetLocalTracking()
    {
        return new WASVPSTrackingData
        {
            Position = _camera.localPosition,
            Rotation = _camera.localRotation
        };
    }

    public bool IsLocalized()
    {
        return _isLocalized;
    }

    public bool Localize(string locationId)
    {
        Debug.Log($"[RokidTracking] Localized to location: {locationId}");
        _isLocalized = true;
        return true;
    }

    public void ResetTracking()
    {
        Debug.Log("[RokidTracking] Reset tracking.");
        _isLocalized = false;
    }
}
