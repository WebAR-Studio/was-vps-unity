using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public struct ChartVector
{
    public double x;
    public float y;
    public float z;

    public ChartVector(double x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override string ToString()
    {
        return $"{x}, {y}, {z}";
    }
}

public class ChartDrawer : MonoBehaviour
{
    #region Variables
    [BoxGroup("Borders")]
    [SerializeField] private UseCase _useСases;
    [BoxGroup("Borders")]
    [SerializeField] private bool _addThirdAxis;
    [BoxGroup("Borders")]
    [SerializeField] private bool _isXAxisTimeData;

    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private double _minX;
    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private double _maxX;
    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private double _stepsX;
    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private float _minY;
    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private float _maxY;
    [BoxGroup("Borders")]
    [ShowIf("IsManualBorder")]
    [SerializeField] private float _stepsY;
    [BoxGroup("Borders")]
    [ShowIf(EConditionOperator.And, "IsManualBorder", "_addThirdAxis")]
    [HideIf("_addThirdAxis")]
    [SerializeField] private float _minZ;
    [BoxGroup("Borders")]
    [ShowIf(EConditionOperator.And, "IsManualBorder", "_addThirdAxis")]
    [HideIf("_addThirdAxis")]
    [SerializeField] private float _maxZ;
    [BoxGroup("Borders")]
    [ShowIf(EConditionOperator.And, "IsManualBorder", "_addThirdAxis")]
    [SerializeField] private float _stepsZ;

    [BoxGroup("Borders")]
    [SerializeField] private int _dashThickness;

    [BoxGroup("Borders")]
    [HideIf("IsManualBorder")]
    [SerializeField] private bool _isIntergerAxisX = true;
    [BoxGroup("Borders")]
    [HideIf(EConditionOperator.Or, "IsManualBorder", "_isIntergerAxisX")]
    [SerializeField] private int _numberOfDecimalPlacesX;
    [BoxGroup("Borders")]
    [HideIf("IsManualBorder")]
    [SerializeField] private bool _isIntergerAxisY = true;
    [BoxGroup("Borders")]
    [HideIf(EConditionOperator.Or, "IsManualBorder", "_isIntergerAxisY")]
    [SerializeField] private int _numberOfDecimalPlacesY;
    [BoxGroup("Borders")]
    [HideIf(EConditionOperator.Or, "IsManualBorder", "NotNeedThirdAxis")]
    [SerializeField] private bool _isIntergerAxisZ = true;
    [BoxGroup("Borders")]
    [HideIf(EConditionOperator.Or, "IsManualBorder", "_isIntergerAxisZ", "NotNeedThirdAxis")]
    [SerializeField] private int _numberOfDecimalPlacesZ;

    [BoxGroup("Title customize")]
    [SerializeField] private string _chartTitle;
    [BoxGroup("Title customize")]
    [SerializeField] private string _axisXTitle;
    [BoxGroup("Title customize")]
    [SerializeField] private string _axisYTitle;
    [BoxGroup("Title customize")]
    [ShowIf("_addThirdAxis")]
    [SerializeField] private string _axisZTitle;

    [BoxGroup("Points Customize")]
    [SerializeField] private bool _hidePoint = false;
    [BoxGroup("Points Customize")]
    [HideIf("_hidePoint")]
    [SerializeField] private Sprite _pointImage;
    [HideIf("_hidePoint")]
    [BoxGroup("Points Customize")]
    [SerializeField] private Vector2 _pointSize = new Vector2(10, 10);

    [BoxGroup("Lines Customize")]
    [SerializeField] private bool _hideLine = false;
    [BoxGroup("Lines Customize")]
    [HideIf("_hideLine")]
    [SerializeField] private float _lineWidth = 2;
    [BoxGroup("Lines Customize")]
    [HideIf("_hideLine")]
    [SerializeField] private List<Color32> _colorsForLines = new List<Color32>();

    [BoxGroup("Referenses")]
    [SerializeField] private Transform _content;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _labelTemplateX;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _labelTemplateY;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _labelTemplateZ;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _DashTemplateZ;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _DashTemplateX;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _DashTemplateY;
    [BoxGroup("Referenses")]
    [SerializeField] private TextMeshProUGUI _chartTitleText;
    [BoxGroup("Referenses")]
    [SerializeField] private TextMeshProUGUI _axisXTitleText;
    [BoxGroup("Referenses")]
    [SerializeField] private TextMeshProUGUI _axisYTitleText;
    [BoxGroup("Referenses")]
    [SerializeField] private TextMeshProUGUI _axisZTitleText;
    [BoxGroup("Referenses")]
    [SerializeField] private RectTransform _signatureTemplate;

    private Dictionary<string, Chart> _rectPositions = new Dictionary<string, Chart>();
    private List<GameObject> _points;
    private List<GameObject> _lines;
    private float _graphSizeX;
    private float _graphSizeY;
    private List<GameObject> _lablesObjects = new List<GameObject>();
    private List<GameObject> _chartsSignatures = new List<GameObject>();

    #region NaughtyAttributes helper functions
    private enum UseCase : byte
    {
        ManualBorder = 1,
        AutomaticBorder
    }

    private bool IsManualBorder => _useСases == UseCase.ManualBorder;

    private bool NeedToSHowNumberOfDecimalPlacesZ => !IsManualBorder && _isIntergerAxisZ && _addThirdAxis;
    private bool NotNeedThirdAxis => !_addThirdAxis;
    #endregion
    #endregion

    [SerializeField] private bool _showSignature;
    [SerializeField] private int _layer;

    public static DateTime StartingPoint = DateTime.Now;

    private void Start()
    {
        StartingPoint = DateTime.Now;
    }

    private void SetLabel()
    {
        double oldNumber = 0f;
        double newNumber = 0f;

        for (int i = 0; i * _stepsX + _minX <= _maxX; i++)
        {
            newNumber = i * _stepsX + _minX;

            Vector2 tmpPosition = new Vector2(_graphSizeX / (float)(_maxX - _minX) * (float)_stepsX * i, -2);
            if (!(_minX == 0 && i == 0))
            {
                if (tmpPosition.x > _graphSizeX || tmpPosition.x < 0)
                {
                    continue;
                }

                RectTransform labelX = Instantiate(_labelTemplateX, _content);
                labelX.gameObject.SetActive(true);
                labelX.anchoredPosition = tmpPosition - new Vector2(0, -tmpPosition.y * 2); ;
                if (!_isIntergerAxisX)
                {
                    labelX.GetComponent<TextMeshProUGUI>().text = (_stepsX * i + _minX).ToString();
                }
                else
                {
                    labelX.GetComponent<TextMeshProUGUI>().text = Math.Round(_stepsX * i + _minX).ToString();
                }

                if (_isXAxisTimeData)
                {
                    labelX.GetComponent<TextMeshProUGUI>().text = GetTimeString(_stepsX * i + _minX);
                }

                _lablesObjects.Add(labelX.gameObject);
            }

            if (newNumber > 0 && oldNumber < 0)
            {
                RectTransform zeroDash = Instantiate(_DashTemplateX, _content);
                zeroDash.gameObject.SetActive(true);
                zeroDash.anchoredPosition = GetPointPosition(new ChartVector(0, 0 + _minY, 0));
                zeroDash.sizeDelta = new Vector2(_dashThickness * 2, _graphSizeY);
                zeroDash.GetComponent<Image>().color -= new Color32(30, 30, 30, 0);
            }

            RectTransform dashX = Instantiate(_DashTemplateX, _content);
            dashX.gameObject.SetActive(true);
            dashX.anchoredPosition = tmpPosition - new Vector2(0, -tmpPosition.y);
            if (_stepsX * i + _minX == 0)
            {
                dashX.sizeDelta = new Vector2(_dashThickness * 2, _graphSizeY - tmpPosition.y * 2);
                dashX.GetComponent<Image>().color -= new Color32(30, 30, 30, 0);
            }
            else
            {
                dashX.sizeDelta = new Vector2(_dashThickness, _graphSizeY - tmpPosition.y * 2);
            }

            _lablesObjects.Add(dashX.gameObject);

            oldNumber = newNumber;
        }

        oldNumber = 0;


        for (int i = 0; i * _stepsY + _minY <= _maxY; i++)
        {
            newNumber = i * _stepsY + _minX;

            Vector2 tmpPosition = new Vector2(-2, _graphSizeY / (float)(_maxY - _minY) * (float)_stepsY * i);

            if (tmpPosition.y > _graphSizeY || tmpPosition.y < 0)
            {
                continue;
            }

            if (!(_minY == 0 && i == 0))
            {
                RectTransform labelY = Instantiate(_labelTemplateY, _content);
                labelY.gameObject.SetActive(true);
                labelY.anchoredPosition = tmpPosition - new Vector2(-tmpPosition.x * 2, 0); ;
                if (!_isIntergerAxisY)
                {
                    labelY.GetComponent<TextMeshProUGUI>().text = (_stepsY * i + _minY).ToString();
                }
                else
                {
                    labelY.GetComponent<TextMeshProUGUI>().text = Mathf.Round(_stepsY * i + _minY).ToString();
                }

                _lablesObjects.Add(labelY.gameObject);
            }

            if (newNumber > 0 && oldNumber < 0)
            {
                RectTransform zeroDash = Instantiate(_DashTemplateY, _content);
                zeroDash.gameObject.SetActive(true);
                zeroDash.anchoredPosition = GetPointPosition(new ChartVector(0 + _minX, 0, 0));
                zeroDash.sizeDelta = new Vector2(_graphSizeX, _dashThickness * 2);
                zeroDash.GetComponent<Image>().color -= new Color32(30, 30, 30, 0);
            }

            RectTransform dashY = Instantiate(_DashTemplateY, _content);
            dashY.gameObject.SetActive(true);
            dashY.anchoredPosition = tmpPosition - new Vector2(-tmpPosition.x, 0);

            float width = _graphSizeX - tmpPosition.x * 2;
            if (_stepsY * i + _minY == 0)
            {
                dashY.sizeDelta = new Vector2(width, _dashThickness * 2);
                dashY.GetComponent<Image>().color -= new Color32(30, 30, 30, 0);
            }
            else
            {
                dashY.sizeDelta = new Vector2(width, _dashThickness);
            }

            _lablesObjects.Add(dashY.gameObject);

            oldNumber = newNumber;
        }

        if (_addThirdAxis)
        {
            for (int i = 0; i * _stepsZ + _minZ <= _maxZ; i++)
            {
                newNumber = i * _stepsZ + _minZ;

                Vector2 tmpPosition = new Vector2(0, _graphSizeY / (float)(_maxZ - _minZ) * (float)_stepsZ * i);

                if (tmpPosition.y > _graphSizeY || tmpPosition.y < 0)
                {
                    continue;
                }

                RectTransform dashZ = Instantiate(_DashTemplateZ, _content);
                dashZ.gameObject.SetActive(true);
                dashZ.anchoredPosition = tmpPosition;
                float width = dashZ.sizeDelta.x;
                dashZ.sizeDelta = new Vector2(width, _dashThickness);

                _lablesObjects.Add(dashZ.gameObject);

                if (!(_minY == 0 && i == 0))
                {
                    RectTransform labelZ = Instantiate(_labelTemplateZ, _content);
                    labelZ.gameObject.SetActive(true);
                    TextMeshProUGUI labelText = labelZ.GetComponent<TextMeshProUGUI>();
                    labelZ.anchoredPosition = tmpPosition + new Vector2(width + (labelText.fontSize / 2f), 0);
                    if (!_isIntergerAxisZ)
                    {
                        labelText.text = (_stepsZ * i + _minZ).ToString();
                    }
                    else
                    {
                        labelText.text = Mathf.Round(_stepsZ * i + _minZ).ToString();
                    }

                    _lablesObjects.Add(labelZ.gameObject);
                }

                oldNumber = newNumber;
            }
        }
    }

    private string GetTimeString(double dataSeconds)
    {
        var pointTime = StartingPoint + TimeSpan.FromSeconds(dataSeconds);

        return pointTime.ToString(@"hh\:mm\:ss");
    }

    private void AddSignature(string chartID, Color32 color)
    {
        if (!_showSignature)
        {
            return;
        }

        RectTransform signatureTransform = Instantiate(_signatureTemplate, transform);
        signatureTransform.GetComponent<SignatureManager>().SetSignature(chartID, color);
        signatureTransform.gameObject.SetActive(true);
        float offset;
        if (_addThirdAxis)
        {
            offset = signatureTransform.rect.width + _axisZTitleText.rectTransform.sizeDelta.y;
        }
        else
        {
            offset = signatureTransform.rect.width;
        }

        signatureTransform.anchoredPosition = new Vector2(offset, -(_chartsSignatures.Count * signatureTransform.sizeDelta.y * 2));
        _chartsSignatures.Add(signatureTransform.gameObject);
    }

    private void UpdateTitlesTexts()
    {
        if (!String.IsNullOrEmpty(_chartTitle))
        {
            _chartTitleText.gameObject.SetActive(true);
            _chartTitleText.text = _chartTitle;
        }

        if (!String.IsNullOrEmpty(_axisXTitle))
        {
            _axisXTitleText.gameObject.SetActive(true);
            _axisXTitleText.text = _axisXTitle;
        }

        if (!String.IsNullOrEmpty(_axisYTitle))
        {
            _axisYTitleText.gameObject.SetActive(true);
            _axisYTitleText.text = _axisYTitle;
        }

        if (!String.IsNullOrEmpty(_axisZTitle) && _addThirdAxis)
        {
            _axisZTitleText.gameObject.SetActive(true);
            _axisZTitleText.text = _axisZTitle;
        }
        else
        {
            _axisZTitleText.gameObject.SetActive(false);
        }

    }

    public void AddLine(string lineName, List<ChartVector> positions, bool useAdditionalAxis = false)
    {
        _rectPositions.Add(lineName, new Chart(positions, GetLineColor(_rectPositions.Count + 1), useAdditionalAxis));
    }

    public void RemoveChartLines()
    {
        _rectPositions.Clear();
    }

    private Vector3 GetPointPosition(ChartVector point, bool useAdditionalAxis = false)
    {
        Vector3 pointPosition;
        if (!useAdditionalAxis)
        {
            pointPosition = new Vector3((float)((point.x - _minX) * (_graphSizeX / (_maxX - _minX))), (point.y - _minY) * (_graphSizeY / (_maxY - _minY)), 0);
        }
        else
        {
            pointPosition = new Vector3((float)((point.x - _minX) * (_graphSizeX / (_maxX - _minX))), (point.y - _minZ) * (_graphSizeY / (_maxZ - _minZ)), 0);
        }

        return pointPosition;
    }

    private void SetChartScale()
    {
        _graphSizeX = _content.GetComponent<RectTransform>().rect.width;
        _graphSizeY = _content.GetComponent<RectTransform>().rect.height;

        if (_useСases == UseCase.AutomaticBorder)
        {
            double maxPointX = float.NegativeInfinity;
            float maxPointY = float.NegativeInfinity;
            float maxPointZ = float.NegativeInfinity;
            double minPointX = float.PositiveInfinity;
            float minPointY = float.PositiveInfinity;
            float minPointZ = float.PositiveInfinity;

            double NumberMaxCountOfSymbolsX = 0f;

            foreach (var chart in _rectPositions)
            {
                foreach (var point in chart.Value.PointPositions)
                {
                    if (point.x > maxPointX)
                    {
                        maxPointX = point.x;
                    }
                    if (point.x < minPointX)
                    {
                        minPointX = point.x;
                    }

                    if (chart.Value.AreUseAdditionalAxis)
                    {
                        if (point.y > maxPointZ)
                        {
                            maxPointZ = point.y;
                        }
                        if (point.y < minPointZ)
                        {
                            minPointZ = point.y;
                        }
                    }
                    else
                    {
                        if (point.y > maxPointY)
                        {
                            maxPointY = point.y;
                        }
                        if (point.y < minPointY)
                        {
                            minPointY = point.y;
                        }
                    }

                    if (point.x.ToString().Length > NumberMaxCountOfSymbolsX.ToString().Length)
                    {
                        NumberMaxCountOfSymbolsX = point.x;
                    }
                }
            }

            float deltaY = maxPointY - minPointY;
            float deltaZ = maxPointZ - minPointZ;
            _maxX = maxPointX;
            _maxY = maxPointY + deltaY / 10f;
            _maxZ = maxPointZ + deltaZ / 10f;
            _minX = minPointX;
            _minY = minPointY - deltaY / 10f;
            _minZ = minPointZ - deltaZ / 10f;

            if ((_maxY - _minY) > 9)
            {
                _isIntergerAxisY = true;
            }
            if ((_maxX - _minX) > 9)
            {
                _isIntergerAxisX = true;
            }
            if ((_maxZ - _minZ) > 9)
            {
                _isIntergerAxisZ = true;
            }

            if (_isIntergerAxisX)
            {
                int countSymbols = Math.Round(NumberMaxCountOfSymbolsX).ToString().Length;
                int countOfStepsX = (int)Mathf.Floor(_graphSizeX / (_labelTemplateX.GetComponent<TextMeshProUGUI>().fontSize * countSymbols * 1.3f)) + 1;
                _minX = GetRoundNumber(_minX, _numberOfDecimalPlacesX, true);
                _stepsX = Math.Floor((_maxX - _minX) / (float)countOfStepsX);
            }
            else
            {
                int countSymbols = NumberMaxCountOfSymbolsX.ToString("F" + _numberOfDecimalPlacesX).Length;
                int countOfStepsX = (int)Mathf.Floor(_graphSizeX / (_labelTemplateX.GetComponent<TextMeshProUGUI>().fontSize * countSymbols * 1.3f)) + 1;
                _minX = GetRoundNumber(_minX, _numberOfDecimalPlacesX, true);
                _stepsX = (_maxX - _minX) / (float)countOfStepsX;
                _stepsX = GetRoundNumber(_stepsX, _numberOfDecimalPlacesX);
            }

            int countOfStepsY = (int)Mathf.Floor(_graphSizeY / (_labelTemplateY.GetComponent<TextMeshProUGUI>().fontSize * 3)) + 1;
            if (_isIntergerAxisY)
            {
                _minY = GetRoundNumber(_minY, _numberOfDecimalPlacesY, true);
                _stepsY = Mathf.Floor((_maxY - _minY) / (float)countOfStepsY);
            }
            else
            {
                _minY = GetRoundNumber(_minY, _numberOfDecimalPlacesY, true);
                _stepsY = (_maxY - _minY) / (float)countOfStepsY;
                _stepsY = GetRoundNumber(_stepsY, _numberOfDecimalPlacesY);
            }

            int countOfStepsZ = (int)Mathf.Floor(_graphSizeY / (_labelTemplateZ.GetComponent<TextMeshProUGUI>().fontSize * 3)) + 1;
            if (_isIntergerAxisZ)
            {
                _minZ = GetRoundNumber(_minZ, _numberOfDecimalPlacesZ, true);
                _stepsZ = Mathf.Floor((_maxZ - _minZ) / (float)countOfStepsZ);
            }
            else
            {
                _minZ = GetRoundNumber(_minZ, _numberOfDecimalPlacesZ, true);
                _stepsZ = (_maxZ - _minZ) / (float)countOfStepsZ;
                _stepsZ = GetRoundNumber(_stepsZ, _numberOfDecimalPlacesZ);
            }

            if (_stepsX == 0)
            {
                _stepsX = 1;
            }
            if (_stepsY == 0)
            {
                _stepsY = 1;
            }
            if (_stepsZ == 0)
            {
                _stepsZ = 1;
            }
        }
    }

    private float GetRoundNumber(double value, int digits, bool toFloor = false)
    {
        if (toFloor)
        {
            return (float)Math.Floor(value * Math.Pow(10, digits)) / (float)Mathf.Pow(10, digits);
        }
        else
        {
            return (float)Math.Round(value * Math.Pow(10, digits)) / (float)Mathf.Pow(10, digits);
        }

    }

    public void DrawChart()
    {
        if (_rectPositions.Count == 0)
            return;

        SetChartScale();

        SetLabel();
        UpdateTitlesTexts();

        _points = new List<GameObject>();
        _lines = new List<GameObject>();

        foreach (var line in _rectPositions)
        {
            if (line.Value.AreUseAdditionalAxis && !_addThirdAxis)
            {
                continue;
            }

            AddSignature(line.Key, line.Value.ChartColor);
            List<GameObject> tmpPointsList = new List<GameObject>();

            for (int i = 0; i < line.Value.PointPositions.Count; i++)
            {
                Vector3 normalizePointPosition = GetPointPosition(line.Value.PointPositions[i], line.Value.AreUseAdditionalAxis);
                GameObject pointObject = InstantiatePoint(normalizePointPosition, i);
                _points.Add(pointObject);
                tmpPointsList.Add(pointObject);
            }

            for (int i = 0; i < tmpPointsList.Count - 1; i++)
            {
                DrawLine(tmpPointsList[i], tmpPointsList[i + 1], line.Value.ChartColor);
            }

            foreach (GameObject item in tmpPointsList)
            {
                item.transform.SetAsLastSibling();
            }
        }
    }

    public void ClearChart()
    {
        if (_points != null)
        {
            foreach (var item in _points)
            {
                Destroy(item.gameObject);
            }

            _points.Clear();
        }

        if (_lines != null)
        {
            foreach (var item in _lines)
            {
                Destroy(item.gameObject);
            }
            _lines.Clear();
        }

        if (_lablesObjects != null)
        {
            foreach (var item in _lablesObjects)
            {
                Destroy(item.gameObject);
            }
            _lablesObjects.Clear();
        }

        if (_chartsSignatures != null)
        {
            foreach (var item in _chartsSignatures)
            {
                Destroy(item.gameObject);
            }
            _chartsSignatures.Clear();
        }

        RemoveChartLines();
    }

    // for worls space canvas
    private void DrawLine(GameObject startPoint, GameObject endPoint, Color32 lineColor)
    {
        Vector3 startPos = startPoint.transform.localPosition;
        Vector3 endPos = endPoint.transform.localPosition;

        Vector2 dir = endPos - startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        GameObject line = new GameObject("line");
        line.transform.SetParent(_content, false);

        RectTransform rectTransform = line.AddComponent<RectTransform>();
        Image image = line.AddComponent<Image>();

        rectTransform.localPosition = (startPos + endPos) * 0.5f;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

        rectTransform.sizeDelta = new Vector2(dir.magnitude, _lineWidth);

        image.color = lineColor;
        image.enabled = !_hideLine;

        _lines.Add(line);
    }

    //private void DrawLine(GameObject startPoint, GameObject endPoint, Color32 lineColor)
    //{
    //    Vector3 dir = startPoint.transform.position - endPoint.transform.position;
    //    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    //    Quaternion q = Quaternion.Euler(0f, 0f, angle);

    //    GameObject line = new GameObject("line");
    //    line.AddComponent<RectTransform>().anchorMax = new Vector2(0, 0);
    //    line.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
    //    line.AddComponent<Image>();
    //    var lineObject = Instantiate(line, GetCenterOfTwoObject(startPoint, endPoint), q, _content);
    //    lineObject.GetComponent<RectTransform>().sizeDelta = GetDeltaSize(startPoint, endPoint);
    //    lineObject.GetComponent<Image>().color = lineColor;
    //    lineObject.GetComponent<Image>().enabled = !_hideLine;
    //    Destroy(line);
    //    _lines.Add(lineObject);
    //}

    private Color32 GetLineColor(int chartNum)
    {
        if (chartNum <= _colorsForLines.Count)
        {
            return _colorsForLines[chartNum - 1];
        }
        else
        {
            var rand = new System.Random();
            return new Color32((byte)rand.Next(0, 255), (byte)rand.Next(0, 255), (byte)rand.Next(0, 255), 255);
        }
    }

    private Vector3 GetDeltaSize(GameObject startPoint, GameObject endPoint)
    {
        return new Vector2(Vector2.Distance(startPoint.GetComponent<RectTransform>().anchoredPosition, endPoint.GetComponent<RectTransform>().anchoredPosition), _lineWidth);
    }

    private Vector3 GetCenterOfTwoObject(GameObject startPoint, GameObject endPoint)
    {
        return new Vector3((startPoint.transform.position.x + endPoint.transform.position.x) / 2,
             (startPoint.transform.position.y + endPoint.transform.position.y) / 2, 0);
    }

    private GameObject InstantiatePoint(Vector3 vector3, int i)
    {
        if (_content == null)
        {
            throw new NullReferenceException("Content in null!");
        }
        GameObject pointObject = new GameObject("Point " + i);
        GameObject point = Instantiate(pointObject, _content);
        point.layer = _layer;
        point.AddComponent<RectTransform>().sizeDelta = _pointSize;
        point.GetComponent<RectTransform>().anchoredPosition = vector3;
        point.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
        point.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        point.AddComponent<Image>().sprite = _pointImage;
        point.GetComponent<Image>().enabled = !_hidePoint;
        Destroy(pointObject);
        return point;
    }

    public void SetAxisXTitle(string title)
    {
        _axisXTitle = title;
        if (!string.IsNullOrEmpty(_axisXTitle))
        {
            _axisXTitleText.gameObject.SetActive(true);
            _axisXTitleText.text = _axisXTitle;
        }
        else
        {
            _axisXTitleText.gameObject.SetActive(false);
        }
    }

    public void SetAxisYTitle(string title)
    {
        _axisYTitle = title;
        if (!string.IsNullOrEmpty(_axisYTitle))
        {
            _axisYTitleText.gameObject.SetActive(true);
            _axisYTitleText.text = _axisYTitle;
        }
        else
        {
            _axisYTitleText.gameObject.SetActive(false);
        }
    }
}
