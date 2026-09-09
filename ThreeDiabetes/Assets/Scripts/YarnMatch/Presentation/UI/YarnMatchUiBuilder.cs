using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchUiBuilder
{
    private readonly YarnMatchAudio _audio;

    internal YarnMatchUiBuilder(YarnMatchAudio audio)
    {
        _audio = audio;
    }

    internal YarnMatchUiReferences Build(
        Action<YarnMatchSpoolToken> onSpoolSelected,
        Action onRestart,
        Action onNextLevel,
        Action onHint,
        Action onRefresh,
        Action onOpenLevelSelect,
        Action<int> onLevelSelected,
        Action onUnlockAllLevels,
        Action onStartSpecialChallenge,
        Action onPreviewFailure,
        Action onBackToMainMenu)
    {
        YarnMatchUiPrimitives.CreateEventSystemIfNeeded();
        YarnMatchUiReferences ui = new YarnMatchUiReferences();
        GameObject canvasObject = new GameObject("Yarn Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(750f, 1334f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        ui.Canvas = canvas;
        ui.CanvasRect = canvasObject.GetComponent<RectTransform>();

        YarnMatchUiPrimitives.CreateScreenBackground("Background", canvasObject.transform, new Color(0.985f, 0.99f, 1f), false);
        Image topBand = YarnMatchUiPrimitives.CreateImage("Top Band", canvasObject.transform, YarnMatchVisualFactory.GetSolidSprite(), new Color(0.91f, 0.95f, 1f, 0.82f), new Vector2(0f, 420f), new Vector2(0f, 415f), false);
        topBand.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        topBand.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        Image poolBand = YarnMatchUiPrimitives.CreateImage("Pool Band", canvasObject.transform, YarnMatchVisualFactory.GetSolidSprite(), new Color(0.94f, 0.97f, 1f, 0.96f), new Vector2(0f, 430f), new Vector2(0f, -390f), false);
        poolBand.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        poolBand.rectTransform.anchorMax = new Vector2(1f, 0.5f);

        BuildHeader(ui, canvasObject.transform, onBackToMainMenu);
        BuildBoardArea(ui, canvasObject.transform);
        BuildRackArea(ui, canvasObject.transform);
        BuildPoolArea(ui, canvasObject.transform);
        BuildFooter(ui, canvasObject.transform, onRestart, onHint, onRefresh);
        BuildResultOverlay(ui, canvasObject.transform, onRestart, onNextLevel, onPreviewFailure, onBackToMainMenu);
        BuildMainMenu(ui, canvasObject.transform, onOpenLevelSelect);
        BuildLevelSelect(ui, canvasObject.transform, onLevelSelected, onUnlockAllLevels, onStartSpecialChallenge, onBackToMainMenu);
        ui.EffectsRoot = YarnMatchUiPrimitives.CreateChild("Effects", canvasObject.transform).transform;
        return ui;
    }

    private static void BuildHeader(YarnMatchUiReferences ui, Transform parent, Action onBackToMainMenu)
    {
        ui.BackHomeButton = YarnMatchUiPrimitives.CreateButton("Back Home", parent, "\u8FD4\u56DE", new Vector2(-312f, 628f), new Vector2(76f, 42f), new Color(0.25f, 0.34f, 0.50f), new Color(0.90f, 0.94f, 1f));
        ui.BackHomeButton.onClick.AddListener(() => onBackToMainMenu?.Invoke());
        TMP_Text title = YarnMatchUiPrimitives.CreateText("Title", parent, "\u7EBF\u56E2\u6D88\u6D88\u4E50", 34, new Color(0.15f, 0.19f, 0.34f), TextAlignmentOptions.MidlineLeft, new Vector2(-130f, 628f), new Vector2(250f, 52f), FontStyles.Bold);
        YarnMatchUiPrimitives.AddTextOutline(title, new Color(1f, 1f, 1f, 0.92f), 2f);
        YarnMatchUiPrimitives.CreateText("Subtitle", parent, "\u4E00\u683C\u4E00\u683C\uFF0C\u628A\u6BDB\u7EBF\u6536\u56DE\u5BB6", 14, new Color(0.35f, 0.41f, 0.57f), TextAlignmentOptions.MidlineLeft, new Vector2(-130f, 596f), new Vector2(250f, 26f), FontStyles.Normal);
        ui.LevelLabel = YarnMatchUiPrimitives.CreateText("Level", parent, "\u7B2C 1 \u5173", 17, new Color(0.13f, 0.54f, 0.88f), TextAlignmentOptions.MidlineRight, new Vector2(245f, 628f), new Vector2(170f, 34f), FontStyles.Bold);
        ui.RemainingLabel = YarnMatchUiPrimitives.CreateText("Remaining", parent, "\u5269\u4F59\u7EBF\u56E2 72", 14, new Color(0.35f, 0.41f, 0.57f), TextAlignmentOptions.MidlineRight, new Vector2(245f, 596f), new Vector2(200f, 26f), FontStyles.Normal);
        Image progressBack = YarnMatchUiPrimitives.CreateImage("Progress Back", parent, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.82f, 0.87f, 0.95f, 1f), new Vector2(500f, 10f), new Vector2(0f, 566f), true);
        Image progress = YarnMatchUiPrimitives.CreateImage("Progress Fill", progressBack.transform, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.11f, 0.65f, 0.94f), new Vector2(500f, 10f), Vector2.zero, true);
        progress.type = Image.Type.Filled;
        progress.fillMethod = Image.FillMethod.Horizontal;
        progress.fillOrigin = 0;
        progress.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        progress.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        progress.rectTransform.pivot = new Vector2(0f, 0.5f);
        ui.ProgressFill = progress;
        ui.StatusLabel = YarnMatchUiPrimitives.CreateText("Status", parent, "\u9009\u62E9\u5E95\u90E8\u9732\u51FA\u7684\u540C\u8272\u7EBF\u56E2\uFF0C\u7EBF\u5708\u4F1A\u9010\u6839\u6536\u7D27", 13, new Color(0.38f, 0.44f, 0.58f), TextAlignmentOptions.Center, new Vector2(0f, 535f), new Vector2(680f, 26f), FontStyles.Normal);
    }
    private static void BuildBoardArea(YarnMatchUiReferences ui, Transform parent)
    {
        GameObject boardAreaObject = YarnMatchUiPrimitives.CreateChild("Board Area", parent);
        ui.BoardArea = boardAreaObject.GetComponent<RectTransform>();
        ui.BoardArea.sizeDelta = new Vector2(700f, 500f);
        ui.BoardArea.anchoredPosition = new Vector2(0f, 235f);
        ui.BoardArea.gameObject.AddComponent<RectMask2D>();
        ui.BoardGridRoot = YarnMatchUiPrimitives.CreateChild("Board Grid", ui.BoardArea).transform;
        ui.BoardCellsRoot = YarnMatchUiPrimitives.CreateChild("Board Cells", ui.BoardArea).transform;
    }

    private static void BuildRackArea(YarnMatchUiReferences ui, Transform parent)
    {
        GameObject rackAreaObject = YarnMatchUiPrimitives.CreateChild("Rack Area", parent);
        ui.RackArea = rackAreaObject.GetComponent<RectTransform>();
        ui.RackArea.sizeDelta = new Vector2(700f, 130f);
        ui.RackArea.anchoredPosition = new Vector2(0f, -92f);
        ui.RackLabel = YarnMatchUiPrimitives.CreateText("Rack Caption", ui.RackArea, "收线台  ·  7 格开放  ·  收集一半后解锁最后一格", 15, new Color(0.22f, 0.25f, 0.38f), TextAlignmentOptions.Center, new Vector2(0f, 48f), new Vector2(680f, 28f), FontStyles.Bold);
        ui.RackRoot = YarnMatchUiPrimitives.CreateChild("Rack Slots", ui.RackArea).transform;
    }

    private static void BuildPoolArea(YarnMatchUiReferences ui, Transform parent)
    {
        GameObject poolAreaObject = YarnMatchUiPrimitives.CreateChild("Pool Area", parent);
        ui.PoolArea = poolAreaObject.GetComponent<RectTransform>();
        ui.PoolArea.sizeDelta = new Vector2(700f, 430f);
        ui.PoolArea.anchoredPosition = new Vector2(0f, -365f);
        YarnMatchUiPrimitives.CreateText("Pool Caption", ui.PoolArea, "选择滚筒  ·  先选第一排，再向四周解锁", 15, new Color(0.22f, 0.25f, 0.38f), TextAlignmentOptions.Center, new Vector2(0f, 178f), new Vector2(680f, 28f), FontStyles.Bold);
        ui.PoolRoot = YarnMatchUiPrimitives.CreateChild("Spool Pool", ui.PoolArea).transform;
    }

    private void BuildFooter(YarnMatchUiReferences ui, Transform parent, Action onRestart, Action onHint, Action onRefresh)
    {
        ui.RestartButton = YarnMatchUiPrimitives.CreateButton("Restart", parent, "重新开始", new Vector2(-252f, -620f), new Vector2(136f, 54f), new Color(0.20f, 0.27f, 0.44f), new Color(0.86f, 0.91f, 0.99f));
        ui.RestartButton.onClick.AddListener(() => { _audio?.PlayClick(); onRestart?.Invoke(); });
        ui.HintButton = YarnMatchUiPrimitives.CreateButton("Hint", parent, "提示", new Vector2(-92f, -620f), new Vector2(120f, 54f), new Color(0.48f, 0.28f, 0.02f), new Color(1f, 0.86f, 0.34f));
        ui.HintButton.onClick.AddListener(() => { _audio?.PlayClick(); onHint?.Invoke(); });
        ui.RefreshButton = YarnMatchUiPrimitives.CreateButton("Refresh", parent, "刷新道具", new Vector2(62f, -620f), new Vector2(120f, 54f), new Color(0.08f, 0.31f, 0.52f), new Color(0.48f, 0.87f, 1f));
        ui.RefreshButton.onClick.AddListener(() => { _audio?.PlayClick(); onRefresh?.Invoke(); });
        YarnMatchUiPrimitives.CreateText("Footer Note", parent, "同色滚筒最多收集 3 格", 12, new Color(0.42f, 0.48f, 0.62f), TextAlignmentOptions.MidlineLeft, new Vector2(238f, -620f), new Vector2(190f, 38f), FontStyles.Normal);
        ui.ToastLabel = YarnMatchUiPrimitives.CreateText("Toast", parent, string.Empty, 14, new Color(0.15f, 0.19f, 0.34f), TextAlignmentOptions.Center, new Vector2(0f, 87f), new Vector2(650f, 38f), FontStyles.Bold);
        ui.ToastLabel.gameObject.SetActive(false);
    }

    private void BuildResultOverlay(
        YarnMatchUiReferences ui,
        Transform parent,
        Action onRestart,
        Action onNextLevel,
        Action onPreviewFailure,
        Action onBackToMainMenu)
    {
        ui.ResultOverlay = YarnMatchUiPrimitives.CreateScreenLayer("Result Overlay", parent);
        ui.ResultCanvasGroup = ui.ResultOverlay.AddComponent<CanvasGroup>();
        YarnMatchUiPrimitives.CreateScreenBackground("Dim", ui.ResultOverlay.transform, new Color(0.18f, 0.22f, 0.32f, 0.38f), true);
        Image panel = YarnMatchUiPrimitives.CreateImage("Result Panel", ui.ResultOverlay.transform, YarnMatchVisualFactory.GetPanelSprite(), Color.white, new Vector2(610f, 350f), new Vector2(0f, 10f), true);
        ui.ResultPanelRect = panel.rectTransform;
        ui.ResultTitle = YarnMatchUiPrimitives.CreateText("Result Title", panel.transform, "完成啦！", 40, new Color(0.08f, 0.60f, 0.32f), TextAlignmentOptions.Center, new Vector2(0f, 86f), new Vector2(540f, 62f), FontStyles.Bold);
        ui.ResultDetail = YarnMatchUiPrimitives.CreateText("Result Detail", panel.transform, "每一根毛线都回到收线台了", 17, new Color(0.31f, 0.36f, 0.49f), TextAlignmentOptions.Center, new Vector2(0f, 36f), new Vector2(540f, 42f), FontStyles.Normal);
        ui.ResultNextButton = YarnMatchUiPrimitives.CreateButton("Next Level", panel.transform, "下一关", new Vector2(0f, -58f), new Vector2(270f, 60f), Color.white, new Color(0.10f, 0.62f, 0.88f));
        ui.ResultNextLabel = ui.ResultNextButton.GetComponentInChildren<TMP_Text>();
        ui.ResultNextButton.onClick.AddListener(() => { _audio?.PlayClick(); onNextLevel?.Invoke(); });
        ui.ResultReplayButton = YarnMatchUiPrimitives.CreateButton("Replay Level", panel.transform, "重玩本关", new Vector2(115f, -58f), new Vector2(210f, 60f), Color.white, new Color(0.91f, 0.34f, 0.42f));
        ui.ResultReplayButton.onClick.AddListener(() => { _audio?.PlayClick(); onRestart?.Invoke(); });
        ui.ResultPreviewButton = YarnMatchUiPrimitives.CreateButton("Preview Failure", panel.transform, "查看残局", new Vector2(-115f, -58f), new Vector2(210f, 60f), new Color(0.12f, 0.34f, 0.52f), new Color(0.72f, 0.91f, 1f));
        ui.ResultPreviewButton.onClick.AddListener(() => { _audio?.PlayClick(); onPreviewFailure?.Invoke(); });
        ui.ResultHomeButton = YarnMatchUiPrimitives.CreateButton("Back To Home", panel.transform, "返回首页", new Vector2(0f, -124f), new Vector2(270f, 52f), new Color(0.20f, 0.32f, 0.52f), new Color(0.90f, 0.94f, 1f));
        ui.ResultHomeButton.onClick.AddListener(() => { _audio?.PlayClick(); onBackToMainMenu?.Invoke(); });
        ui.ResultOverlay.SetActive(false);
    }

    private void BuildMainMenu(YarnMatchUiReferences ui, Transform parent, Action onOpenLevelSelect)
    {
        ui.MainMenuOverlay = YarnMatchUiPrimitives.CreateScreenLayer("Main Menu", parent);
        YarnMatchUiPrimitives.CreateScreenBackground("Menu Background", ui.MainMenuOverlay.transform, new Color(0.985f, 0.99f, 1f, 1f), true);
        YarnMatchUiPrimitives.CreateImage("Menu Yarn Blue", ui.MainMenuOverlay.transform, YarnMatchVisualFactory.GetCellSprite(YarnMatchUiTheme.Palette[(int)YarnMatchColor.Cyan]), Color.white, new Vector2(144f, 144f), new Vector2(-150f, 210f), false);
        YarnMatchUiPrimitives.CreateImage("Menu Yarn Pink", ui.MainMenuOverlay.transform, YarnMatchVisualFactory.GetCellSprite(YarnMatchUiTheme.Palette[(int)YarnMatchColor.Pink]), Color.white, new Vector2(118f, 118f), new Vector2(148f, 160f), false);
        TMP_Text title = YarnMatchUiPrimitives.CreateText("Menu Title", ui.MainMenuOverlay.transform, "线团消消乐", 46, new Color(0.13f, 0.19f, 0.36f), TextAlignmentOptions.Center, new Vector2(0f, 360f), new Vector2(650f, 70f), FontStyles.Bold);
        YarnMatchUiPrimitives.AddTextOutline(title, new Color(0.84f, 0.93f, 1f, 1f), 3f);
        YarnMatchUiPrimitives.CreateText("Menu Subtitle", ui.MainMenuOverlay.transform, "一格一格，把毛线收回家", 18, new Color(0.30f, 0.42f, 0.60f), TextAlignmentOptions.Center, new Vector2(0f, 302f), new Vector2(640f, 36f), FontStyles.Normal);
        YarnMatchUiPrimitives.CreateText("Menu Rule", ui.MainMenuOverlay.transform, "底部优先  ·  四方向解锁  ·  同色收满三格", 14, new Color(0.47f, 0.53f, 0.66f), TextAlignmentOptions.Center, new Vector2(0f, -252f), new Vector2(640f, 30f), FontStyles.Normal);
        Button startButton = YarnMatchUiPrimitives.CreateButton("Start Game", ui.MainMenuOverlay.transform, "开始游戏", new Vector2(0f, -158f), new Vector2(310f, 70f), Color.white, new Color(0.10f, 0.62f, 0.88f));
        startButton.onClick.AddListener(() => { _audio?.PlayStart(); onOpenLevelSelect?.Invoke(); });
        ui.MainMenuOverlay.SetActive(true);
    }
    private void BuildLevelSelect(YarnMatchUiReferences ui, Transform parent,
        Action<int> onLevelSelected, Action onUnlockAllLevels,
        Action onStartSpecialChallenge, Action onBackToMainMenu)
    {
        ui.LevelSelectOverlay = YarnMatchUiPrimitives.CreateScreenLayer("Level Select", parent);
        Transform root = ui.LevelSelectOverlay.transform;
        YarnMatchUiPrimitives.CreateScreenBackground("Level Background", root, Color.white, true);
        ui.LevelSelectTitle = YarnMatchUiPrimitives.CreateText("Level Title", root,
            "选择关卡", 34, new Color(0.12f, 0.20f, 0.30f), TextAlignmentOptions.Center,
            new Vector2(0f, 599f), new Vector2(650f, 54f), FontStyles.Bold);
        ui.LevelSelectSubtitle = YarnMatchUiPrimitives.CreateText("Chapter Range", root,
            "", 18, new Color(0.36f, 0.44f, 0.49f), TextAlignmentOptions.Center,
            new Vector2(0f, 548f), new Vector2(650f, 32f), FontStyles.Normal);
        ui.LevelList = new YarnMatchLevelList(root, onLevelSelected, _audio);
        ui.SpecialChallengeButton = YarnMatchUiPrimitives.CreateButton("Special Challenge", root,
            "特殊挑战", new Vector2(-170f, -523f), new Vector2(310f, 58f),
            new Color(0.58f, 0.20f, 0.38f), new Color(1f, 0.87f, 0.92f));
        ui.SpecialChallengeButton.onClick.AddListener(() => { _audio?.PlayClick(); onStartSpecialChallenge?.Invoke(); });
        ui.UnlockAllLevelsButton = YarnMatchUiPrimitives.CreateButton("Unlock Today", root,
            "今日全部解锁", new Vector2(170f, -523f), new Vector2(310f, 58f),
            new Color(0.15f, 0.38f, 0.45f), new Color(0.79f, 0.95f, 0.92f));
        ui.UnlockAllLevelsLabel = ui.UnlockAllLevelsButton.GetComponentInChildren<TMP_Text>();
        ui.UnlockAllLevelsButton.onClick.AddListener(() => { _audio?.PlayClick(); onUnlockAllLevels?.Invoke(); });
        Button back = YarnMatchUiPrimitives.CreateButton("Back To Menu", root,
            "返回首页", new Vector2(0f, -610f), new Vector2(310f, 56f),
            new Color(0.25f, 0.34f, 0.40f), new Color(0.90f, 0.94f, 0.96f));
        back.onClick.AddListener(() => { _audio?.PlayClick(); onBackToMainMenu?.Invoke(); });
        ui.LevelSelectOverlay.SetActive(false);
    }

    private static void CreateBoardGridBackplates(Transform parent)
    {
        float cellSize = YarnMatchUiTheme.BoardCellSize;
        for (int row = 0; row < YarnMatchUiTheme.BoardRows; row++)
        {
            for (int column = 0; column < YarnMatchUiTheme.BoardColumns; column++)
            {
                Vector2 position = YarnMatchUiTheme.BoardPosition(column, row);
                YarnMatchUiPrimitives.CreateImage("Board Tile Shadow", parent, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.28f, 0.36f, 0.52f, 0.15f), new Vector2(cellSize + 6f, cellSize + 6f), position + new Vector2(2f, -3f), true);
                YarnMatchUiPrimitives.CreateImage("Board Tile", parent, YarnMatchVisualFactory.GetPanelSprite(), new Color(1f, 1f, 1f, 0.92f), new Vector2(cellSize, cellSize), position, true);
            }
        }
    }
}
