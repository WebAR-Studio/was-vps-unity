using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SampleTargetManager : MonoBehaviour
{
    [SerializeField] private SamplePathShowerArrows _pathShower;
    [SerializeField] private List<SampleMarkerPoint> _targets = new();
    [SerializeField] private float _defaultArrivalDistance = 2f;

    private readonly SampleMarkerPoint _runtimeTarget = new SampleMarkerPoint
    {
        Key = "__sample_runtime_target"
    };

    private SampleMarkerPoint _currentTarget;

    public SampleMarkerPoint CurrentTarget => _currentTarget;
    public float DefaultArrivalDistance => _defaultArrivalDistance;

    public void AddOrUpdateTarget(SampleMarkerPoint targetPoint)
    {
        if (targetPoint == null || string.IsNullOrEmpty(targetPoint.Key))
        {
            return;
        }

        var existing = _targets.FirstOrDefault(t => t.Key == targetPoint.Key);
        if (existing != null)
        {
            existing.SetData(targetPoint);
            return;
        }

        _targets.Add(targetPoint);
    }

    public bool ShowPath(string targetKey, bool isVisible = true)
    {
        if (_pathShower == null || string.IsNullOrEmpty(targetKey))
        {
            return false;
        }

        var point = _targets.FirstOrDefault(t => t.Key == targetKey);
        if (point == null)
        {
            return false;
        }

        ShowPath(point.Point, point.Radius, isVisible);
        _currentTarget = point;
        return true;
    }

    public void ShowPath(Vector3 worldPoint, float? arrivalDistance = null, bool isVisible = true)
    {
        if (_pathShower == null)
        {
            return;
        }

        _runtimeTarget.Point = worldPoint;
        _runtimeTarget.Radius = arrivalDistance;
        _currentTarget = _runtimeTarget;

        _pathShower.SetTarget(worldPoint);
        _pathShower.ShowPath(isVisible);
    }

    public void HidePath()
    {
        _pathShower?.HidePath();
    }

    public float GetDistanceToTarget()
    {
        return _pathShower == null ? 0f : _pathShower.GetDistanceToTarget();
    }

    public float GetTimeToTarget()
    {
        return _pathShower == null ? 0f : _pathShower.GetTimeToTarget();
    }

    public float GetArrivalDistance()
    {
        return _currentTarget?.Radius ?? _defaultArrivalDistance;
    }

    public Vector3 GetStartPosition()
    {
        if (_pathShower != null && _pathShower.StartPoint != null)
        {
            return _pathShower.StartPoint.position;
        }

        return transform.position;
    }
}
