using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetManager : MonoBehaviour
{
    [SerializeField] private PathShowerArrows _pathShower;
    [SerializeField] private List<MarkerPoint> _targets = new();
    [SerializeField] private float _defaultArrivalDistance = 2f;

    private readonly MarkerPoint _runtimeTarget = new MarkerPoint
    {
        Key = "__runtime_target"
    };

    private MarkerPoint _currentTarget;

    /// <summary>
    /// Current target marker used for active route calculations.
    /// </summary>
    public MarkerPoint CurrentTarget => _currentTarget;

    /// <summary>
    /// Default arrival radius in meters used when route point radius is not specified.
    /// </summary>
    public float DefaultArrivalDistance => _defaultArrivalDistance;

    /// <summary>
    /// Adds a marker point or updates an existing marker with the same key.
    /// </summary>
    /// <param name="targetPoint">Marker point to store.</param>
    public void AddOrUpdateTarget(MarkerPoint targetPoint)
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

    /// <summary>
    /// Shows a path to a predefined marker by key.
    /// </summary>
    /// <param name="targetKey">Unique marker key.</param>
    /// <param name="isVisible">Whether route arrows should be visible.</param>
    /// <returns>True when marker is found and route is shown; otherwise false.</returns>
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

    /// <summary>
    /// Shows a path to a world position.
    /// </summary>
    /// <param name="worldPoint">Target position in world coordinates.</param>
    /// <param name="arrivalDistance">Optional arrival radius override.</param>
    /// <param name="isVisible">Whether route arrows should be visible.</param>
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

    /// <summary>
    /// Hides active route visualization.
    /// </summary>
    public void HidePath()
    {
        _pathShower?.HidePath();
    }

    /// <summary>
    /// Returns current route distance to target.
    /// </summary>
    /// <returns>Distance in meters or zero when path is unavailable.</returns>
    public float GetDistanceToTarget()
    {
        return _pathShower == null ? 0f : _pathShower.GetDistanceToTarget();
    }

    /// <summary>
    /// Returns estimated travel time to target based on configured average speed.
    /// </summary>
    /// <returns>Estimated travel time in seconds.</returns>
    public float GetTimeToTarget()
    {
        return _pathShower == null ? 0f : _pathShower.GetTimeToTarget();
    }

    /// <summary>
    /// Returns arrival radius for current target or fallback default value.
    /// </summary>
    /// <returns>Arrival radius in meters.</returns>
    public float GetArrivalDistance()
    {
        return _currentTarget?.Radius ?? _defaultArrivalDistance;
    }

    /// <summary>
    /// Returns route start position used by path renderer.
    /// </summary>
    /// <returns>Start position in world coordinates.</returns>
    public Vector3 GetStartPosition()
    {
        if (_pathShower != null && _pathShower.StartPoint != null)
        {
            return _pathShower.StartPoint.position;
        }

        return transform.position;
    }
}
