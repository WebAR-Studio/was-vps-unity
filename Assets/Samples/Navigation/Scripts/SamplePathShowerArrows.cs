using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class SamplePathShowerArrows : MonoBehaviour
{
    [SerializeField] private Transform _startPoint;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private float _arrowSpacing = 0.5f;
    [SerializeField] private float _arrowYOffset = 0.05f;
    [SerializeField] private float _updateDelay = 0.15f;
    [SerializeField] private float _navMeshSampleRadius = 3f;
    [SerializeField] private float _navMeshFallbackSampleRadius = 50f;
    [SerializeField] private bool _drawDirectPathIfNavMeshUnavailable = true;
    [SerializeField] private float _middleSpeed = 3.5f;

    private readonly List<GameObject> _arrows = new();
    private NavMeshPath _path;
    private Coroutine _updateRoutine;
    private Vector3 _targetPosition;
    private bool _hasTarget;
    private bool _isVisible;
    private bool _isActive;
    private bool _wasOnNavMesh;

    public event Action OnLeftNavMesh;
    public bool IsVisible => _isVisible;
    public Transform StartPoint => _startPoint;

    private void Awake()
    {
        EnsurePath();
    }

    private void OnDisable()
    {
        HidePath();
    }

    public void SetTarget(Vector3 worldPosition)
    {
        _targetPosition = worldPosition;
        _hasTarget = true;
    }

    public void ShowPath(bool visible = true)
    {
        EnsurePath();
        _isVisible = visible;
        _isActive = true;
        _wasOnNavMesh = false;

        if (!_isVisible)
        {
            ClearArrows();
        }

        if (_updateRoutine == null)
        {
            _updateRoutine = StartCoroutine(UpdateRoutine());
        }
    }

    public void HidePath()
    {
        _isActive = false;
        _isVisible = false;
        _hasTarget = false;

        if (_updateRoutine != null)
        {
            StopCoroutine(_updateRoutine);
            _updateRoutine = null;
        }

        ClearArrows();
    }

    public float GetDistanceToTarget()
    {
        EnsurePath();

        if (_path == null || _path.corners == null || _path.corners.Length < 2)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 0; i < _path.corners.Length - 1; i++)
        {
            total += Vector3.Distance(_path.corners[i], _path.corners[i + 1]);
        }

        return total;
    }

    public float GetTimeToTarget()
    {
        if (_middleSpeed <= 0.01f)
        {
            return float.PositiveInfinity;
        }

        return GetDistanceToTarget() / _middleSpeed;
    }

    private IEnumerator UpdateRoutine()
    {
        EnsurePath();
        WaitForSeconds wait = _updateDelay > 0f ? new WaitForSeconds(_updateDelay) : null;

        while (_isActive)
        {
            if (_startPoint == null || _arrowPrefab == null || !_hasTarget)
            {
                ClearArrows();
                yield return wait ?? null;
                continue;
            }

            var onNavMesh = TryGetNavMeshPointWithFallback(_startPoint.position, out var navStart);
            if (!_wasOnNavMesh && onNavMesh)
            {
                _wasOnNavMesh = true;
            }
            else if (_wasOnNavMesh && !onNavMesh)
            {
                _wasOnNavMesh = false;
                OnLeftNavMesh?.Invoke();
            }

            var hasTargetOnNavMesh = TryGetNavMeshPointWithFallback(_targetPosition, out var navTarget);

            if (onNavMesh && hasTargetOnNavMesh && NavMesh.CalculatePath(navStart, navTarget, NavMesh.AllAreas, _path))
            {
                if (_isVisible)
                {
                    UpdateArrows(_path);
                }
                else
                {
                    ClearArrows();
                }
            }
            else if (_isVisible && _drawDirectPathIfNavMeshUnavailable)
            {
                UpdateDirectArrows(_startPoint.position, _targetPosition);
            }
            else
            {
                ClearArrows();
            }

            yield return wait ?? null;
        }

        _updateRoutine = null;
    }

    private void UpdateArrows(NavMeshPath navPath)
    {
        ClearArrows();

        for (var i = 0; i < navPath.corners.Length - 1; i++)
        {
            var start = navPath.corners[i];
            var end = navPath.corners[i + 1];
            var distance = Vector3.Distance(start, end);
            var count = Mathf.FloorToInt(distance / _arrowSpacing);
            var direction = (end - start).normalized;

            if (count <= 0)
            {
                continue;
            }

            for (var j = 0; j < count; j++)
            {
                var position = start + direction * (j * _arrowSpacing);
                var rotation = Quaternion.LookRotation(direction, Vector3.up);
                var arrow = Instantiate(_arrowPrefab, position + Vector3.up * _arrowYOffset, rotation);
                _arrows.Add(arrow);
            }
        }
    }

    private void UpdateDirectArrows(Vector3 start, Vector3 end)
    {
        ClearArrows();

        var vector = end - start;
        var distance = vector.magnitude;
        if (distance <= 0.01f)
        {
            return;
        }

        var direction = vector / distance;
        var count = Mathf.Max(1, Mathf.FloorToInt(distance / _arrowSpacing));
        var rotation = Quaternion.LookRotation(direction, Vector3.up);

        for (var i = 0; i < count; i++)
        {
            var position = start + direction * (i * _arrowSpacing);
            var arrow = Instantiate(_arrowPrefab, position + Vector3.up * _arrowYOffset, rotation);
            _arrows.Add(arrow);
        }
    }

    private void ClearArrows()
    {
        for (var i = 0; i < _arrows.Count; i++)
        {
            if (_arrows[i] != null)
            {
                Destroy(_arrows[i]);
            }
        }

        _arrows.Clear();
    }

    private static bool TryGetNavMeshPoint(Vector3 origin, float maxDistance, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(origin, out var hit, maxDistance, NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        navPoint = origin;
        return false;
    }

    private bool TryGetNavMeshPointWithFallback(Vector3 origin, out Vector3 navPoint)
    {
        if (TryGetNavMeshPoint(origin, _navMeshSampleRadius, out navPoint))
        {
            return true;
        }

        if (_navMeshFallbackSampleRadius <= _navMeshSampleRadius)
        {
            return false;
        }

        return TryGetNavMeshPoint(origin, _navMeshFallbackSampleRadius, out navPoint);
    }

    private void EnsurePath()
    {
        if (_path == null)
        {
            _path = new NavMeshPath();
        }
    }
}
