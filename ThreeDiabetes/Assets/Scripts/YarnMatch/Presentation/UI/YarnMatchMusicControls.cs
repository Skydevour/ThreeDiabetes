using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class YarnMatchMusicControls
{
    internal static void Build(Transform parent, YarnMatchMusic music)
    {
        GameObject root = YarnMatchUiPrimitives.CreateChild("Music Controls", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(480f, 64f);
        rect.anchoredPosition = new Vector2(0f, -535f);
        Image box = YarnMatchUiPrimitives.CreateImage("Music Toggle", root.transform,
            YarnMatchVisualFactory.GetPanelSprite(), new Color(0.85f, 0.93f, 0.92f),
            new Vector2(34f, 34f), new Vector2(-205f, 0f), true);
        box.raycastTarget = true;
        Image check = YarnMatchUiPrimitives.CreateImage("Checked", box.transform,
            YarnMatchVisualFactory.GetPanelSprite(), new Color(0.10f, 0.60f, 0.40f),
            new Vector2(20f, 20f), Vector2.zero, true);
        Toggle toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = music.MusicEnabled;
        toggle.onValueChanged.AddListener(music.SetEnabled);
        YarnMatchUiPrimitives.CreateText("Music Label", root.transform, "背景音乐", 20,
            new Color(0.22f, 0.31f, 0.36f), TextAlignmentOptions.MidlineLeft,
            new Vector2(-111f, 0f), new Vector2(120f, 36f), FontStyles.Normal);
        Image track = YarnMatchUiPrimitives.CreateImage("Music Volume", root.transform,
            YarnMatchVisualFactory.GetPanelSprite(), new Color(0.80f, 0.86f, 0.88f),
            new Vector2(220f, 10f), new Vector2(118f, 0f), true);
        track.raycastTarget = true;
        Image handle = YarnMatchUiPrimitives.CreateImage("Volume Handle", track.transform,
            YarnMatchVisualFactory.GetPanelSprite(), new Color(0.10f, 0.60f, 0.40f),
            new Vector2(28f, 34f), Vector2.zero, true);
        handle.raycastTarget = true;
        Slider slider = track.gameObject.AddComponent<Slider>();
        slider.targetGraphic = handle;
        slider.handleRect = handle.rectTransform;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = music.Volume;
        slider.onValueChanged.AddListener(music.SetVolume);
    }
}
