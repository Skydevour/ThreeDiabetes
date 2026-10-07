using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchSpoolCapacityBadge
{
    private readonly TMP_Text _label;
    private int _capacity = -1;

    internal YarnMatchSpoolCapacityBadge(Transform parent, float cellSize)
    {
        float diameter = Mathf.Min(cellSize * 0.42f, Mathf.Clamp(cellSize * 0.34f, 18f, 22f));
        float inset = cellSize * 0.5f - diameter * 0.5f - 1f;
        Image background = YarnMatchUiPrimitives.CreateImage(
            "Capacity Badge", parent, YarnMatchVisualFactory.GetCircleSprite(), Color.white,
            new Vector2(diameter, diameter), new Vector2(inset, -inset), false);
        Outline edge = background.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(0.18f, 0.22f, 0.30f, 0.62f);
        edge.effectDistance = new Vector2(0.9f, -0.9f);
        edge.useGraphicAlpha = true;

        _label = YarnMatchUiPrimitives.CreateText(
            "Capacity", background.transform, string.Empty,
            Mathf.RoundToInt(diameter * 0.80f), new Color(0.16f, 0.20f, 0.24f),
            TextAlignmentOptions.Center, Vector2.zero,
            new Vector2(diameter, diameter), FontStyles.Bold);
        _label.margin = Vector4.zero;
        _label.enableAutoSizing = false;
    }

    internal void SetCapacity(int capacity)
    {
        if (_capacity == capacity) return;
        _capacity = capacity;
        _label.SetText("{0}", capacity);
    }
}
