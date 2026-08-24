using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchUiReferences
{
    internal Canvas Canvas;
    internal RectTransform CanvasRect;
    internal RectTransform BoardArea;
    internal RectTransform RackArea;
    internal RectTransform PoolArea;
    internal Transform BoardGridRoot;
    internal Transform BoardCellsRoot;
    internal Transform RackRoot;
    internal Transform PoolRoot;
    internal Transform PoolSlotsRoot;
    internal Transform PoolButtonsRoot;
    internal Transform EffectsRoot;
    internal Image ProgressFill;
    internal TMP_Text RemainingLabel;
    internal TMP_Text LevelLabel;
    internal TMP_Text RackLabel;
    internal TMP_Text StatusLabel;
    internal TMP_Text ToastLabel;
    internal Button RestartButton;
    internal Button HintButton;
    internal Button RefreshButton;
    internal Button BackHomeButton;
    internal GameObject ResultOverlay;
    internal RectTransform ResultPanelRect;
    internal CanvasGroup ResultCanvasGroup;
    internal TMP_Text ResultTitle;
    internal TMP_Text ResultDetail;
    internal TMP_Text ResultNextLabel;
    internal Button ResultNextButton;
    internal Button ResultReplayButton;
    internal Button ResultHomeButton;
    internal GameObject MainMenuOverlay;
    internal GameObject LevelSelectOverlay;
    internal TMP_Text LevelSelectTitle;
    internal TMP_Text LevelSelectSubtitle;
    internal Button UnlockAllLevelsButton;
    internal TMP_Text UnlockAllLevelsLabel;
    internal Button SpecialChallengeButton;
    internal readonly List<Button> LevelButtons = new List<Button>();
    internal readonly List<TMP_Text> LevelButtonLabels = new List<TMP_Text>();
    internal readonly List<TMP_Text> LevelButtonDetails = new List<TMP_Text>();
}
