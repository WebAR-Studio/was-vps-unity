using System;
using UnityEngine;

[Serializable]
public class MarkerPoint
{
    public string Key;
    public Vector3 Point;
    public float? Radius;

    /// <summary>
    /// Copies point data from another marker point instance.
    /// </summary>
    /// <param name="source">Source marker point to copy values from.</param>
    public void SetData(MarkerPoint source)
    {
        if (source == null)
        {
            return;
        }

        Key = source.Key;
        Point = source.Point;
        Radius = source.Radius;
    }
}
