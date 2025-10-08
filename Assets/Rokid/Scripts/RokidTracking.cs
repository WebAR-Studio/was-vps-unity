using UnityEngine;
using WASVPS;

public class RokidTracking : MonoBehaviour, IWASVPSTracking
{
    [SerializeField] private Transform _camera;
    private WASVPSTrackingData _trackingData = new WASVPSTrackingData();

    private void UpdateTrackingData()
    {
        if (_camera != null)
        {
            _trackingData.Position = _camera.localPosition;
            _trackingData.Rotation = _camera.localRotation;
        }
    }

    public WASVPSTrackingData GetLocalTracking()
    {
        if (_camera == null)
        {
            VPSLogger.Log(LogLevel.ERROR, "Camera is not available, returning last known tracking data");
            return _trackingData;
        }

        UpdateTrackingData();
        return _trackingData;
    }

    public bool IsLocalized()
    {
        return !string.IsNullOrEmpty(_trackingData.LocationId);
    }

    public bool Localize(string locationId)
    {
        if (_trackingData.LocationId == locationId) return false;
        _trackingData.LocationId = locationId;
        return true;
    }

    public void ResetTracking()
    {
        Debug.Log("[RokidTracking] Reset tracking.");
        _trackingData = new WASVPSTrackingData();
    }
}
