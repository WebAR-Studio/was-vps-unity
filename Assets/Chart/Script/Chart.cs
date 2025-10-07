using System.Collections.Generic;
using UnityEngine;

public class Chart
{
    public List<ChartVector> PointPositions;
    public Color32 ChartColor;
    public bool AreUseAdditionalAxis;

    public Chart(List<ChartVector> points, Color32 color, bool additionalAxis)
    {
        PointPositions = points;
        ChartColor = color;
        AreUseAdditionalAxis = additionalAxis;
    }
}
