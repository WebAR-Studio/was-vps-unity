using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class NavMenuView : MonoBehaviour
{
    [SerializeField] private Text _titleText;
    [SerializeField] private RectTransform _pointListContainer;
    [FormerlySerializedAs("_pointButtonTemplate")]
    [SerializeField] private Button _pointButtonPrefab;
    [SerializeField] private string _defaultTitle = "Where are we going?";

    private readonly List<Button> _runtimeButtons = new();

    /// <summary>
    /// Invoked when a point button is pressed.
    /// </summary>
    public event Action<int> PointSelected;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    /// <summary>
    /// Sets menu view title text.
    /// </summary>
    /// <param name="title">Title to display.</param>
    public void SetTitle(string title)
    {
        if (_titleText == null)
        {
            return;
        }

        _titleText.text = string.IsNullOrWhiteSpace(title) ? _defaultTitle : title;
    }

    /// <summary>
    /// Rebuilds point list buttons from route points.
    /// </summary>
    /// <param name="points">Route points to display.</param>
    public void BindPoints(IReadOnlyList<VpsRouteSequencer.RoutePoint> points)
    {
        ResolveReferences();
        ClearPointButtons();
        DisableLegacyTemplateIfPresent();

        if (_pointListContainer == null || _pointButtonPrefab == null || points == null)
        {
            return;
        }

        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            if (point == null)
            {
                continue;
            }

            var pointIndex = i;
            var button = Instantiate(_pointButtonPrefab, _pointListContainer);
            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => PointSelected?.Invoke(pointIndex));

            ApplyButtonLabel(button, point, pointIndex);
            ApplyButtonIcon(button, point);

            _runtimeButtons.Add(button);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_pointListContainer);
    }

    /// <summary>
    /// Destroys all runtime-created point buttons.
    /// </summary>
    public void ClearPointButtons()
    {
        for (var i = 0; i < _runtimeButtons.Count; i++)
        {
            if (_runtimeButtons[i] != null)
            {
                Destroy(_runtimeButtons[i].gameObject);
            }
        }

        _runtimeButtons.Clear();
    }

    private void ResolveReferences()
    {
        _titleText ??= transform.Find("Title")?.GetComponent<Text>();
        _pointListContainer ??= transform.Find("CategoriesPanel/PointsScrollView/Viewport/Content") as RectTransform;
    }

    private void DisableLegacyTemplateIfPresent()
    {
        if (_pointListContainer == null)
        {
            return;
        }

        var legacyTemplate = _pointListContainer.Find("PointButtonTemplate");
        if (legacyTemplate != null)
        {
            legacyTemplate.gameObject.SetActive(false);
        }
    }

    private static void ApplyButtonLabel(Button button, VpsRouteSequencer.RoutePoint point, int index)
    {
        if (button == null)
        {
            return;
        }

        var label = button.transform.Find("Label")?.GetComponent<Text>();
        if (label == null)
        {
            label = button.GetComponentInChildren<Text>(true);
        }

        if (label == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(point.Title))
        {
            label.text = point.Title;
            return;
        }

        if (!string.IsNullOrWhiteSpace(point.Id))
        {
            label.text = point.Id;
            return;
        }

        label.text = $"Point {index + 1}";
    }

    private static void ApplyButtonIcon(Button button, VpsRouteSequencer.RoutePoint point)
    {
        if (button == null || point == null)
        {
            return;
        }

        var iconImage = button.transform.Find("Icon/Sprite")?.GetComponent<Image>();
        if (iconImage == null)
        {
            return;
        }

        if (point.Icon != null)
        {
            iconImage.sprite = point.Icon;
            iconImage.enabled = true;
            return;
        }

        iconImage.enabled = iconImage.sprite != null;
    }
}
