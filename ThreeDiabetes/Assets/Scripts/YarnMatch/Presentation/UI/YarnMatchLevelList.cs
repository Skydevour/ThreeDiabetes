using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchLevelList
{
    private const int Columns = 4;
    private const float ColumnStep = 166f;
    private const float RowHeight = 180f;
    private const int VisibleRows = 7;
    private readonly ScrollRect _scroll;
    private readonly RectTransform _content;
    private readonly YarnMatchThumbnailQueue _previews;
    private readonly List<Item> _items = new List<Item>();
    private int _highest;
    private int _lastLevel;
    private int _firstRow = -1;

    internal YarnMatchLevelList(Transform parent, Action<int> onSelected, YarnMatchAudio audio)
    {
        Image viewport = YarnMatchUiPrimitives.CreateImage("Level List", parent,
            YarnMatchVisualFactory.GetSolidSprite(), Color.white,
            new Vector2(680f, 940f), new Vector2(0f, 20f), false);
        viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        _previews = viewport.gameObject.AddComponent<YarnMatchThumbnailQueue>();
        _scroll = viewport.gameObject.AddComponent<ScrollRect>();
        _scroll.viewport = viewport.rectTransform;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 42f;
        _content = YarnMatchUiPrimitives.CreateChild("Content", viewport.transform).GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = Vector2.one;
        _content.pivot = new Vector2(0.5f, 1f);
        _content.sizeDelta = Vector2.zero;
        _scroll.content = _content;
        for (int i = 0; i < VisibleRows * Columns; i++)
        {
            Button button = YarnMatchUiPrimitives.CreateButton("Level Item", _content, string.Empty,
                Vector2.zero, new Vector2(156f, 168f), Color.white,
                new Color(0.93f, 0.95f, 0.97f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            Image preview = YarnMatchUiPrimitives.CreateImage("Pattern Preview", button.transform,
                null, Color.white, new Vector2(144f, 144f), new Vector2(0f, 6f), false);
            TMP_Text state = YarnMatchUiPrimitives.CreateText("State", button.transform, "", 13,
                new Color(0.25f, 0.29f, 0.34f), TextAlignmentOptions.Center,
                new Vector2(0f, -75f), new Vector2(140f, 18f), FontStyles.Normal);
            var item = new Item { Button = button, Rect = rect,
                Preview = preview.gameObject.AddComponent<YarnMatchLevelThumbnail>(), State = state };
            button.onClick.AddListener(() =>
            {
                if (!item.Preview.IsReady) return;
                audio?.PlayClick();
                onSelected(item.Level);
            });
            _items.Add(item);
        }
        _scroll.onValueChanged.AddListener(_ => Refresh());
    }

    internal void Show(int highest)
    {
        _highest = Math.Max(1, highest);
        _lastLevel = YarnMatchLevelCatalog.GetChapterEndLevel(_highest);
        int rowCount = (_lastLevel + Columns - 1) / Columns;
        _content.sizeDelta = new Vector2(0f, rowCount * RowHeight);
        _scroll.StopMovement();
        float offset = Math.Min(((_highest - 1) / Columns) * RowHeight,
            Math.Max(0f, _content.sizeDelta.y - _scroll.viewport.rect.height));
        _content.anchoredPosition = new Vector2(0f, offset);
        _firstRow = -1;
        Refresh();
    }

    private void Refresh()
    {
        int rowCount = (_lastLevel + Columns - 1) / Columns;
        int first = Mathf.Clamp(Mathf.FloorToInt(_content.anchoredPosition.y / RowHeight), 0,
            Math.Max(0, rowCount - VisibleRows));
        if (first == _firstRow) return;
        _firstRow = first;
        for (int offset = 0; offset < VisibleRows; offset++)
        for (int column = 0; column < Columns; column++)
        {
            int row = first + offset;
            // Rotate reusable rows: scrolling one row regenerates only four thumbnails.
            Item item = _items[(row % VisibleRows) * Columns + column];
            int level = row * Columns + column + 1;
            item.Level = level;
            item.Button.gameObject.SetActive(level <= _lastLevel);
            if (level > _lastLevel)
            {
                item.Preview.Clear();
                continue;
            }
            bool unlocked = level <= _highest;
            item.Button.interactable = unlocked;
            item.Rect.anchoredPosition = new Vector2(
                (column - (Columns - 1) * 0.5f) * ColumnStep, -(row + 0.5f) * RowHeight);
            item.State.text = unlocked ? string.Empty : "未解锁";
            item.Preview.Show(YarnMatchLevelCatalog.PrepareRound(level));
            _previews.Enqueue(item.Preview);
        }
    }

    private sealed class Item
    {
        internal int Level;
        internal Button Button;
        internal RectTransform Rect;
        internal TMP_Text State;
        internal YarnMatchLevelThumbnail Preview;
    }
}
