using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchLevelList
{
    private const float RowHeight = 116f;
    private const int VisibleRows = 10;
    private readonly ScrollRect _scroll;
    private readonly RectTransform _content;
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
        for (int i = 0; i < VisibleRows; i++)
        {
            Button button = YarnMatchUiPrimitives.CreateButton("Level Item", _content, "关卡",
                Vector2.zero, new Vector2(652f, 104f), new Color(0.12f, 0.27f, 0.40f),
                new Color(0.91f, 0.96f, 0.98f));
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            TMP_Text title = button.GetComponentInChildren<TMP_Text>();
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.rectTransform.sizeDelta = new Vector2(422f, 36f);
            title.rectTransform.anchoredPosition = new Vector2(36f, 19f);
            TMP_Text detail = YarnMatchUiPrimitives.CreateText("Detail", button.transform, "", 17,
                new Color(0.35f, 0.42f, 0.48f), TextAlignmentOptions.MidlineLeft,
                new Vector2(36f, -19f), new Vector2(422f, 30f), FontStyles.Normal);
            Image preview = YarnMatchUiPrimitives.CreateImage("Pattern Preview", button.transform,
                null, Color.white, new Vector2(80f, 80f), new Vector2(-267f, 0f), false);
            TMP_Text state = YarnMatchUiPrimitives.CreateText("State", button.transform, "", 18,
                new Color(0.10f, 0.54f, 0.39f), TextAlignmentOptions.Center,
                new Vector2(275f, 0f), new Vector2(72f, 34f), FontStyles.Bold);
            var item = new Item { Button = button, Rect = rect, Title = title,
                Detail = detail, Preview = preview, State = state };
            button.onClick.AddListener(() => { audio?.PlayClick(); onSelected(item.Level); });
            _items.Add(item);
        }
        _scroll.onValueChanged.AddListener(_ => Refresh());
    }

    internal void Show(int highest)
    {
        _highest = Math.Max(1, highest);
        _lastLevel = YarnMatchLevelCatalog.GetChapterEndLevel(_highest);
        _content.sizeDelta = new Vector2(0f, _lastLevel * RowHeight);
        _scroll.StopMovement();
        float offset = Math.Min((_highest - 1) * RowHeight,
            Math.Max(0f, _content.sizeDelta.y - _scroll.viewport.rect.height));
        _content.anchoredPosition = new Vector2(0f, offset);
        _firstRow = -1;
        Refresh();
    }

    private void Refresh()
    {
        int first = Mathf.Clamp(Mathf.FloorToInt(_content.anchoredPosition.y / RowHeight), 0,
            Math.Max(0, _lastLevel - _items.Count));
        if (first == _firstRow) return;
        _firstRow = first;
        for (int i = 0; i < _items.Count; i++)
        {
            Item item = _items[i];
            int level = first + i + 1;
            item.Level = level;
            item.Button.gameObject.SetActive(level <= _lastLevel);
            if (level > _lastLevel) continue;
            bool unlocked = level <= _highest;
            item.Button.interactable = unlocked;
            item.Rect.anchoredPosition = new Vector2(0f, -(level - 0.5f) * RowHeight);
            item.Title.text = "第 " + level + " 关";
            item.Detail.text = "第 " + (YarnMatchLevelCatalog.GetChapterIndex(level) + 1)
                + " 章 · " + YarnMatchLevelCatalog.GetColorCount(level)
                + " 色" + (level >= 10 ? " · 机关挑战" : "");
            item.State.text = unlocked ? (level < _highest ? "开放" : "开始") : "锁定";
            var templates = YarnMatchTemplateSampler.Templates;
            string asset = templates[(level - 1) % templates.Count].id;
            item.Preview.sprite = Resources.Load<Sprite>("YarnMatch/Patterns/" + asset);
        }
    }

    private sealed class Item
    {
        internal int Level;
        internal Button Button;
        internal RectTransform Rect;
        internal TMP_Text Title;
        internal TMP_Text Detail;
        internal TMP_Text State;
        internal Image Preview;
    }
}
