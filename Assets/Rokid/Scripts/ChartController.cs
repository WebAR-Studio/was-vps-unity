using System.Collections.Generic;
using UnityEngine;

public class ChartController : MonoBehaviour
{
    [SerializeField] private ChartDrawer _chart;
    private List<ChartVector> _defaultValues = new List<ChartVector>();

    void Start()
    {
        SetDefaultValue();
        DrawDefault();
    }

    private void SetDefaultValue()
    {
        double xVal = -5;
        System.Random random = new System.Random();

        for (int i = 0; i < 50; i++)
        {
            float yVal = random.Next(0, 100) / 100f;
            _defaultValues.Add(new ChartVector(xVal, yVal, 0));
            xVal -= 5;
        }
    }

    private void DrawDefault()
    {
        _chart.ClearChart();
        if (_defaultValues == null || _defaultValues.Count < 1)
        {
            return;
        }
        _chart.AddLine("значения параметра", _defaultValues, true);
        _chart.DrawChart();
    }
}
