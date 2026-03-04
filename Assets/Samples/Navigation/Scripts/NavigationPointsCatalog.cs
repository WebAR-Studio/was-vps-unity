using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NavigationPointsCatalog",
    menuName = "Samples/Navigation/Points Catalog")]
public class NavigationPointsCatalog : ScriptableObject
{
    [SerializeField] private List<NavigationPointData> _points = new();

    /// <summary>
    /// Returns all configured navigation points.
    /// </summary>
    public IReadOnlyList<NavigationPointData> Points => _points;

    /// <summary>
    /// Returns point by index or null when index is out of range.
    /// </summary>
    /// <param name="index">Point index.</param>
    public NavigationPointData GetByIndex(int index)
    {
        if (_points == null || index < 0 || index >= _points.Count)
        {
            return null;
        }

        return _points[index];
    }

    /// <summary>
    /// Finds point by unique point ID.
    /// </summary>
    /// <param name="id">Point ID.</param>
    /// <param name="point">Resolved point when found.</param>
    /// <returns>True when point with given ID exists; otherwise false.</returns>
    public bool TryGetById(string id, out NavigationPointData point)
    {
        if (_points != null)
        {
            for (var i = 0; i < _points.Count; i++)
            {
                var candidate = _points[i];
                if (candidate != null && string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    point = candidate;
                    return true;
                }
            }
        }

        point = null;
        return false;
    }

    /// <summary>
    /// Adds a new empty point item to the catalog.
    /// </summary>
    [ContextMenu("Add Empty Point")]
    public void AddEmptyPoint()
    {
        _points ??= new List<NavigationPointData>();
        _points.Add(new NavigationPointData
        {
            Id = $"point_{_points.Count + 1}",
            Title = "New Point",
            WorkingHours = "10:00-22:00",
            Address = "Aviapark Mall, Khodynsky Blvd 4, Moscow",
            RoomAddress = "Floor 1, sector A",
            Description = string.Empty,
            Position = Vector3.zero
        });
    }
}

[Serializable]
public class NavigationPointData
{
    public string Id = "point";
    public string Title = "Point";
    public string WorkingHours = "10:00-22:00";
    public string Address = "Aviapark Mall, Khodynsky Blvd 4, Moscow";
    public string RoomAddress = "Floor 1, sector A";
    [TextArea(2, 8)] public string Description;
    public Sprite Icon;
    public Sprite Image;
    public Vector3 Position;
}
