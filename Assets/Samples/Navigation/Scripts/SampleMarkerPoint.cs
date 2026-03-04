using System;
using UnityEngine;

[Serializable]
public class SampleMarkerPoint
{
    public string Key;
    public Vector3 Point;
    public float? Radius;

    public void SetData(SampleMarkerPoint source)
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
