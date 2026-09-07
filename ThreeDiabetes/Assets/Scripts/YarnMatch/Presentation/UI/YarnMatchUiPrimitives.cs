using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

internal static class YarnMatchUiPrimitives
{
    private const string FallbackFontResourcePath = "Fonts & Materials/LiberationSans SDF";
    private const string ProjectFontResourcePath = "Fonts/SimHei";

    private static TMP_FontAsset _uiFontAsset;
    private static bool _fontWarningIssued;

    internal static GameObject CreateChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        return child;
    }

    internal static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 position, bool sliced)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    internal static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color textColor, Color backgroundColor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(YarnMatchButtonFeedback));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = YarnMatchVisualFactory.GetPanelSprite();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.48f, 0.48f, 0.52f, 0.65f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText("Button Label", buttonObject.transform, label, 17, textColor, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(18f, 10f), FontStyles.Bold);
            text.raycastTarget = false;
        }
        return button;
    }

    internal static TMP_Text CreateText(string name, Transform parent, string content, int size, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = GetUiFontAsset();
        text.text = content;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(8f, size * 0.68f);
        text.fontSizeMax = size;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.margin = new Vector4(4f, 0f, 4f, 0f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    internal static TMP_FontAsset GetUiFontAsset()
    {
        if (_uiFontAsset != null)
        {
            return _uiFontAsset;
        }

        // Prefer the imported project font. This avoids touching TMP_Settings before
        // the package has created its optional settings asset.
        Font font = Resources.Load<Font>(ProjectFontResourcePath);
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "SimHei", "Microsoft YaHei", "Deng", "Arial" }, 64);
        }

        if (font != null)
        {
            try
            {
                _uiFontAsset = TMP_FontAsset.CreateFontAsset(font, 64, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            }
            catch (Exception exception)
            {
                Debug.LogError("YarnMatch UI font asset creation failed: " + exception.Message);
            }
        }

        if (_uiFontAsset == null)
        {
            _uiFontAsset = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
        }

        if (_uiFontAsset == null && !_fontWarningIssued)
        {
            _fontWarningIssued = true;
            Debug.LogError("YarnMatch UI font is unavailable. Import TMP Essential Resources before starting the game.");
        }

        return _uiFontAsset;
    }

    internal static void AddTextOutline(TMP_Text text, Color color, float distance)
    {
        text.outlineWidth = 0.18f;
        text.outlineColor = color;
    }

    internal static void CreateEventSystemIfNeeded()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(eventSystem);
    }

    internal static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(index).gameObject);
        }
    }
}
