using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class YarnMatchGame : MonoBehaviour
{
    private enum GameState
    {
        Playing,
        Resolving,
        Won,
        Lost
    }

    private enum YarnColor
    {
        Coral,
        Orange,
        Butter,
        Mint,
        Cyan,
        Blue,
        Violet,
        Pink
    }

    private sealed class YarnCell
    {
        public YarnColor Color;
        public int Column;
        public int Row;
        public bool Active = true;
        public RectTransform Rect;
        public Image Image;
        public CanvasGroup Group;
    }

    private sealed class SpoolToken
    {
        public YarnColor Color;
        public bool Used;
        public Button Button;
        public Image Image;
    }

    private sealed class RackEntry
    {
        public YarnColor Color;
        public int Slot;
        public int Progress;
        public RectTransform SpoolRect;
        public Image SpoolImage;
        public Image FillImage;
        public Text CountLabel;
    }

    private sealed class TrailVisual
    {
        public readonly List<RectTransform> Segments = new List<RectTransform>();
    }

    private static readonly Color[] Palette =
    {
        new Color(0.95f, 0.28f, 0.36f),
        new Color(0.98f, 0.50f, 0.18f),
        new Color(1.00f, 0.78f, 0.22f),
        new Color(0.20f, 0.78f, 0.47f),
        new Color(0.08f, 0.71f, 0.86f),
        new Color(0.18f, 0.43f, 0.92f),
        new Color(0.54f, 0.28f, 0.86f),
        new Color(0.92f, 0.30f, 0.71f)
    };

    private static readonly string[] ColorNames =
    {
        "CORAL", "ORANGE", "BUTTER", "MINT", "CYAN", "BLUE", "VIOLET", "PINK"
    };

    private readonly List<List<YarnCell>> _columns = new List<List<YarnCell>>();
    private readonly List<YarnCell> _allCells = new List<YarnCell>();
    private readonly List<SpoolToken> _spoolTokens = new List<SpoolToken>();
    private readonly List<RackEntry> _rackEntries = new List<RackEntry>();
    private readonly List<GameObject> _rackSlots = new List<GameObject>();
    private readonly List<Image> _rackSlotImages = new List<Image>();
    private readonly List<Text> _rackSlotLabels = new List<Text>();
    private readonly List<Button> _spoolButtons = new List<Button>();
    private readonly List<Image> _spoolImages = new List<Image>();
    private readonly Queue<Image> _flyingImagePool = new Queue<Image>();
    private readonly Queue<Image> _trailImagePool = new Queue<Image>();

    private Canvas _canvas;
    private RectTransform _canvasRect;
    private RectTransform _boardPanelRect;
    private RectTransform _rackPanelRect;
    private RectTransform _poolPanelRect;
    private Transform _boardGridRoot;
    private Transform _boardCellsRoot;
    private Transform _rackRoot;
    private Transform _poolRoot;
    private Transform _effectsRoot;
    private Image _progressFill;
    private Text _remainingLabel;
    private Text _rackLabel;
    private Text _statusLabel;
    private Text _toastLabel;
    private Button _restartButton;
    private Button _hintButton;
    private GameObject _resultOverlay;
    private Text _resultTitle;
    private Text _resultDetail;
    private GameState _state;
    private Coroutine _toastRoutine;
    private int _totalCells;
    private int _collectedCells;
    private int _unlockedRackSlots = 7;
    private bool _uiReady;

    private readonly int[] _columnHeights = { 6, 5, 5, 6, 5, 5, 6, 5, 5 };
    private const int BoardColumns = 9;
    private const int BoardRows = 8;
    private const int RackCapacity = 8;
    private const int InitialRackSlots = 7;
    private const int CellsPerSpool = 3;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        CreateEventSystemIfNeeded();
        BuildStaticUi();
        StartNewGame();
    }

    private void OnDestroy()
    {
        if (_toastRoutine != null)
        {
            StopCoroutine(_toastRoutine);
        }
    }

    private void BuildStaticUi()
    {
        if (_uiReady)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Yarn Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        scaler.referencePixelsPerUnit = 100f;

        _canvasRect = canvasObject.GetComponent<RectTransform>();
        CreateImage("Background", canvasObject.transform, YarnVisualFactory.GetSolidSprite(), new Color(0.12f, 0.07f, 0.18f), new Vector2(1080f, 1920f), Vector2.zero, false);

        Image topGlow = CreateImage("Top Glow", canvasObject.transform, YarnVisualFactory.GetSolidSprite(), new Color(0.31f, 0.16f, 0.42f, 0.55f), new Vector2(1080f, 720f), new Vector2(0f, 570f), false);
        topGlow.raycastTarget = false;
        Image lowerGlow = CreateImage("Lower Glow", canvasObject.transform, YarnVisualFactory.GetSolidSprite(), new Color(0.37f, 0.17f, 0.48f, 0.64f), new Vector2(1080f, 760f), new Vector2(0f, -560f), false);
        lowerGlow.raycastTarget = false;

        BuildHeader(canvasObject.transform);

        Image boardPanel = CreateImage("Board Panel", canvasObject.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.18f, 0.11f, 0.27f, 0.96f), new Vector2(980f, 850f), new Vector2(0f, 270f), true);
        _boardPanelRect = boardPanel.rectTransform;
        _boardGridRoot = CreateChild("Board Grid", _boardPanelRect).transform;
        _boardCellsRoot = CreateChild("Board Cells", _boardPanelRect).transform;
        CreateBoardGridBackplates();
        CreateText("Board Caption", _boardPanelRect, "EXPOSED YARN CELLS", 22, new Color(0.84f, 0.72f, 0.93f), TextAnchor.MiddleCenter, new Vector2(0f, 392f), new Vector2(400f, 34f), FontStyle.Bold);

        Image rackPanel = CreateImage("Rack Panel", canvasObject.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.31f, 0.17f, 0.40f, 0.98f), new Vector2(980f, 205f), new Vector2(0f, -290f), true);
        _rackPanelRect = rackPanel.rectTransform;
        _rackLabel = CreateText("Rack Caption", _rackPanelRect, "RACK  /  MAX 8", 20, new Color(0.95f, 0.84f, 0.98f), TextAnchor.MiddleCenter, new Vector2(0f, 78f), new Vector2(520f, 30f), FontStyle.Bold);
        _rackRoot = CreateChild("Rack Slots", _rackPanelRect).transform;

        Image poolPanel = CreateImage("Pool Panel", canvasObject.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.40f, 0.21f, 0.49f, 0.98f), new Vector2(980f, 470f), new Vector2(0f, -665f), true);
        _poolPanelRect = poolPanel.rectTransform;
        CreateText("Pool Caption", _poolPanelRect, "SELECT A SPOOL  /  COLLECT 3 MATCHING STITCHES", 20, new Color(0.99f, 0.91f, 0.98f), TextAnchor.MiddleCenter, new Vector2(0f, 194f), new Vector2(760f, 30f), FontStyle.Bold);
        _poolRoot = CreateChild("Spool Pool", _poolPanelRect).transform;

        BuildFooter(canvasObject.transform);

        _effectsRoot = CreateChild("Effects", canvasObject.transform).transform;
        BuildEffectPool();
        BuildResultOverlay(canvasObject.transform);

        _uiReady = true;
    }

    private void BuildHeader(Transform parent)
    {
        Text title = CreateText("Title", parent, "LINECRAFT", 48, new Color(1f, 0.88f, 0.98f), TextAnchor.MiddleLeft, new Vector2(-455f, 850f), new Vector2(510f, 70f), FontStyle.Bold);
        AddTextOutline(title, new Color(0.14f, 0.07f, 0.20f, 0.85f), 3f);
        CreateText("Subtitle", parent, "A SMALL YARN PUZZLE", 18, new Color(0.74f, 0.60f, 0.82f), TextAnchor.MiddleLeft, new Vector2(-450f, 805f), new Vector2(420f, 32f), FontStyle.Normal);
        CreateText("Level", parent, "LEVEL 01", 25, new Color(1f, 0.77f, 0.38f), TextAnchor.MiddleRight, new Vector2(450f, 850f), new Vector2(260f, 48f), FontStyle.Bold);
        _remainingLabel = CreateText("Remaining", parent, "YARN REMAINING  48", 21, new Color(0.88f, 0.80f, 0.93f), TextAnchor.MiddleRight, new Vector2(450f, 810f), new Vector2(360f, 32f), FontStyle.Normal);

        Image progressBack = CreateImage("Progress Back", parent, YarnVisualFactory.GetPanelSprite(), new Color(0.09f, 0.05f, 0.14f, 0.85f), new Vector2(420f, 12f), new Vector2(235f, 760f), true);
        Image progress = CreateImage("Progress Fill", progressBack.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.94f, 0.39f, 0.70f), new Vector2(420f, 12f), Vector2.zero, true);
        progress.type = Image.Type.Filled;
        progress.fillMethod = Image.FillMethod.Horizontal;
        progress.fillOrigin = 0;
        progress.fillClockwise = true;
        progress.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        progress.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        progress.rectTransform.pivot = new Vector2(0f, 0.5f);
        _progressFill = progress;

        _statusLabel = CreateText("Status", parent, "CHOOSE A COLOR THAT CAN REACH THE FRONT", 17, new Color(0.72f, 0.61f, 0.79f), TextAnchor.MiddleCenter, new Vector2(0f, 748f), new Vector2(780f, 28f), FontStyle.Normal);
    }

    private void BuildFooter(Transform parent)
    {
        _restartButton = CreateButton("Restart", parent, "RESTART", new Vector2(-265f, -900f), new Vector2(250f, 70f), new Color(0.93f, 0.74f, 0.98f), new Color(0.33f, 0.16f, 0.43f));
        _restartButton.onClick.AddListener(RequestRestart);
        _hintButton = CreateButton("Hint", parent, "HINT", new Vector2(0f, -900f), new Vector2(250f, 70f), new Color(1f, 0.78f, 0.30f), new Color(0.38f, 0.19f, 0.14f));
        _hintButton.onClick.AddListener(ShowHint);
        CreateText("Footer Note", parent, "MATCH THE FRONTMOST STITCHES", 15, new Color(0.62f, 0.50f, 0.70f), TextAnchor.MiddleCenter, new Vector2(265f, -900f), new Vector2(300f, 45f), FontStyle.Normal);
        _toastLabel = CreateText("Toast", parent, string.Empty, 22, new Color(1f, 0.92f, 0.99f), TextAnchor.MiddleCenter, new Vector2(0f, -20f), new Vector2(540f, 56f), FontStyle.Bold);
        _toastLabel.gameObject.SetActive(false);
    }

    private void BuildResultOverlay(Transform parent)
    {
        _resultOverlay = CreateChild("Result Overlay", parent);
        Image dim = CreateImage("Dim", _resultOverlay.transform, YarnVisualFactory.GetSolidSprite(), new Color(0.06f, 0.03f, 0.09f, 0.74f), new Vector2(1080f, 1920f), Vector2.zero, false);
        dim.raycastTarget = true;
        Image panel = CreateImage("Result Panel", _resultOverlay.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.26f, 0.13f, 0.34f, 1f), new Vector2(650f, 380f), new Vector2(0f, 100f), true);
        _resultTitle = CreateText("Result Title", panel.transform, "VICTORY", 54, new Color(1f, 0.82f, 0.34f), TextAnchor.MiddleCenter, new Vector2(0f, 90f), new Vector2(560f, 80f), FontStyle.Bold);
        _resultDetail = CreateText("Result Detail", panel.transform, "THE YARN IS CLEAR", 22, new Color(0.94f, 0.86f, 0.97f), TextAnchor.MiddleCenter, new Vector2(0f, 25f), new Vector2(560f, 45f), FontStyle.Normal);
        Button playAgain = CreateButton("Play Again", panel.transform, "PLAY AGAIN", new Vector2(0f, -85f), new Vector2(280f, 66f), new Color(1f, 0.78f, 0.30f), new Color(0.31f, 0.14f, 0.38f));
        playAgain.onClick.AddListener(RequestRestart);
        _resultOverlay.SetActive(false);
    }

    private void BuildEffectPool()
    {
        for (int i = 0; i < 4; i++)
        {
            _flyingImagePool.Enqueue(CreatePooledImage("Pooled Yarn", _effectsRoot));
        }

        for (int i = 0; i < 12; i++)
        {
            _trailImagePool.Enqueue(CreatePooledImage("Pooled Thread", _effectsRoot));
        }
    }

    private void CreateBoardGridBackplates()
    {
        float cellSize = 86f;
        for (int row = 0; row < BoardRows; row++)
        {
            for (int column = 0; column < BoardColumns; column++)
            {
                Vector2 position = BoardPosition(column, row);
                Image plate = CreateImage("Board Slot", _boardGridRoot, YarnVisualFactory.GetPanelSprite(), new Color(0.09f, 0.06f, 0.15f, 0.52f), new Vector2(cellSize + 5f, cellSize + 5f), position, true);
                plate.raycastTarget = false;
            }
        }
    }

    private void StartNewGame()
    {
        _state = GameState.Playing;
        _collectedCells = 0;
        _unlockedRackSlots = InitialRackSlots;
        _rackEntries.Clear();
        _columns.Clear();
        _allCells.Clear();
        _spoolTokens.Clear();
        _spoolButtons.Clear();
        _spoolImages.Clear();
        _rackSlots.Clear();
        _rackSlotImages.Clear();
        _rackSlotLabels.Clear();
        ClearChildren(_boardCellsRoot);
        ClearChildren(_rackRoot);
        ClearChildren(_poolRoot);
        ClearEffects();
        _resultOverlay.SetActive(false);

        BuildBoardData();
        BuildRack();
        BuildSpoolPool();
        UpdateBoardVisuals();
        UpdateHeader();
        UpdateControls();
        ShowToast("FIND A COLOR ON THE FRONT", 1.5f);
    }

    private void BuildBoardData()
    {
        List<YarnColor> shuffledColors = new List<YarnColor>();
        for (int color = 0; color < Palette.Length; color++)
        {
            for (int count = 0; count < 6; count++)
            {
                shuffledColors.Add((YarnColor)color);
            }
        }
        Shuffle(shuffledColors, 217);

        int cursor = 0;
        for (int column = 0; column < _columnHeights.Length; column++)
        {
            List<YarnCell> stack = new List<YarnCell>();
            for (int row = 0; row < _columnHeights[column]; row++)
            {
                YarnCell cell = new YarnCell
                {
                    Color = shuffledColors[cursor++],
                    Column = column,
                    Row = row
                };
                cell.Image = CreateImage("Yarn Cell", _boardCellsRoot, YarnVisualFactory.GetCellSprite(Palette[(int)cell.Color]), Color.white, new Vector2(86f, 86f), BoardPosition(column, row), false);
                cell.Rect = cell.Image.rectTransform;
                cell.Group = cell.Image.gameObject.AddComponent<CanvasGroup>();
                cell.Image.raycastTarget = false;
                stack.Add(cell);
                _allCells.Add(cell);
            }
            _columns.Add(stack);
        }

        _totalCells = cursor;
    }

    private void BuildRack()
    {
        float slotSize = 100f;
        float step = 112f;
        float start = -step * (RackCapacity - 1) * 0.5f;
        for (int slot = 0; slot < RackCapacity; slot++)
        {
            GameObject slotObject = CreateChild("Rack Slot " + slot, _rackRoot);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, 112f);
            slotRect.anchoredPosition = new Vector2(start + slot * step, -5f);
            Image slotImage = slotObject.AddComponent<Image>();
            slotImage.sprite = YarnVisualFactory.GetPanelSprite();
            slotImage.type = Image.Type.Sliced;
            slotImage.color = slot < _unlockedRackSlots ? new Color(0.97f, 0.93f, 0.99f) : new Color(0.20f, 0.13f, 0.25f);
            slotImage.raycastTarget = false;
            _rackSlots.Add(slotObject);
            _rackSlotImages.Add(slotImage);

            Text slotLabel = CreateText("Slot Label", slotObject.transform, slot < _unlockedRackSlots ? string.Empty : "LOCK", 14, new Color(0.77f, 0.68f, 0.82f), TextAnchor.MiddleCenter, new Vector2(0f, -12f), new Vector2(96f, 28f), FontStyle.Bold);
            _rackSlotLabels.Add(slotLabel);
        }
    }

    private void BuildSpoolPool()
    {
        List<YarnColor> tokenColors = new List<YarnColor>();
        for (int color = 0; color < Palette.Length; color++)
        {
            tokenColors.Add((YarnColor)color);
            tokenColors.Add((YarnColor)color);
        }
        Shuffle(tokenColors, 901);

        float cellSize = 86f;
        float step = 96f;
        float start = -step * (BoardColumns - 1) * 0.5f;
        for (int index = 0; index < BoardColumns * 2; index++)
        {
            int row = index / BoardColumns;
            int column = index % BoardColumns;
            float y = row == 0 ? 66f : -46f;
            Image slot = CreateImage("Pool Slot", _poolRoot, YarnVisualFactory.GetPanelSprite(), new Color(0.96f, 0.91f, 0.99f, 0.92f), new Vector2(cellSize, cellSize), new Vector2(start + column * step, y), true);
            slot.raycastTarget = false;

            if (index >= tokenColors.Count)
            {
                continue;
            }

            SpoolToken token = new SpoolToken { Color = tokenColors[index] };
            int capturedIndex = index;
            Button button = CreateButton("Spool " + capturedIndex, _poolRoot, string.Empty, new Vector2(start + column * step, y), new Vector2(82f, 82f), Color.white, new Color(0.15f, 0.08f, 0.20f));
            button.onClick.AddListener(() => SelectSpool(capturedIndex));
            Image spoolImage = CreateImage("Spool Visual", button.transform, YarnVisualFactory.GetSpoolSprite(Palette[(int)token.Color]), Color.white, new Vector2(70f, 74f), new Vector2(0f, 2f), false);
            spoolImage.raycastTarget = false;
            token.Button = button;
            token.Image = spoolImage;
            _spoolTokens.Add(token);
            _spoolButtons.Add(button);
            _spoolImages.Add(spoolImage);
        }
    }

    private void SelectSpool(int tokenIndex)
    {
        if (_state != GameState.Playing || tokenIndex < 0 || tokenIndex >= _spoolTokens.Count)
        {
            return;
        }

        SpoolToken token = _spoolTokens[tokenIndex];
        if (token.Used)
        {
            return;
        }

        StartCoroutine(ResolveSpool(tokenIndex, token));
    }

    private IEnumerator ResolveSpool(int tokenIndex, SpoolToken token)
    {
        _state = GameState.Resolving;
        UpdateControls();
        token.Used = true;
        token.Button.interactable = false;
        token.Image.color = new Color(1f, 1f, 1f, 0.24f);

        RackEntry entry = FindRackEntry(token.Color);
        if (entry == null)
        {
            entry = CreateRackEntry(token.Color);
        }

        if (entry == null)
        {
            yield return ShowResult(false, "RACK FULL", "A new color has nowhere to go.");
            yield break;
        }

        List<YarnCell> collected = TakeExposedCells(token.Color, CellsPerSpool);
        UpdateBoardVisuals();

        if (collected.Count == 0)
        {
            entry.Progress = Mathf.Min(2, entry.Progress);
            UpdateRackEntry(entry);
            ShowToast("NO FRONT MATCH  /  RACK +1", 1.25f);
            yield return new WaitForSecondsRealtime(0.22f);
        }

        for (int index = 0; index < collected.Count; index++)
        {
            YarnCell cell = collected[index];
            _collectedCells++;
            if (entry == null)
            {
                entry = CreateRackEntry(token.Color);
                if (entry == null)
                {
                    yield return ShowResult(false, "RACK FULL", "The yarn cannot be stored.");
                    yield break;
                }
            }

            entry.Progress++;
            UpdateRackEntry(entry);
            yield return AnimateCellToRack(cell, entry);

            if (entry.Progress >= CellsPerSpool)
            {
                ShowToast("THREAD REWOUND  /  3 MATCHED", 0.75f);
                yield return PulseRackSlot(entry.Slot);
                RemoveRackEntry(entry);
                entry = null;
            }
        }

        UpdateBoardVisuals();
        UpdateHeader();
        yield return new WaitForSecondsRealtime(0.10f);

        if (_collectedCells >= _totalCells)
        {
            yield return ShowResult(true, "YARN COMPLETE", "Every stitch found its way home.");
            yield break;
        }

        TryUnlockFinalSlot();
        if (IsDeadlocked())
        {
            yield return ShowResult(false, "NO MOVES LEFT", "The rack is full and the front is silent.");
            yield break;
        }

        _state = GameState.Playing;
        UpdateControls();
    }

    private List<YarnCell> TakeExposedCells(YarnColor color, int amount)
    {
        List<YarnCell> result = new List<YarnCell>(amount);
        while (result.Count < amount)
        {
            YarnCell match = null;
            for (int column = 0; column < _columns.Count; column++)
            {
                List<YarnCell> stack = _columns[column];
                if (stack.Count == 0)
                {
                    continue;
                }

                YarnCell exposed = stack[stack.Count - 1];
                if (exposed.Color == color)
                {
                    match = exposed;
                    stack.RemoveAt(stack.Count - 1);
                    break;
                }
            }

            if (match == null)
            {
                break;
            }

            match.Active = false;
            result.Add(match);
        }

        return result;
    }

    private RackEntry FindRackEntry(YarnColor color)
    {
        for (int index = 0; index < _rackEntries.Count; index++)
        {
            if (_rackEntries[index].Color == color)
            {
                return _rackEntries[index];
            }
        }
        return null;
    }

    private RackEntry CreateRackEntry(YarnColor color)
    {
        for (int slot = 0; slot < _unlockedRackSlots; slot++)
        {
            if (FindRackEntryBySlot(slot) != null)
            {
                continue;
            }

            GameObject slotObject = _rackSlots[slot];
            RectTransform spoolRect = CreateImage("Rack Spool", slotObject.transform, YarnVisualFactory.GetSpoolSprite(Palette[(int)color]), Color.white, new Vector2(70f, 74f), new Vector2(0f, 19f), false).rectTransform;
            Image fillBack = CreateImage("Rack Fill Back", slotObject.transform, YarnVisualFactory.GetPanelSprite(), new Color(0.25f, 0.15f, 0.30f, 0.50f), new Vector2(74f, 8f), new Vector2(0f, -39f), true);
            Image fill = CreateImage("Rack Fill", fillBack.transform, YarnVisualFactory.GetPanelSprite(), Palette[(int)color], new Vector2(74f, 8f), Vector2.zero, true);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillClockwise = true;
            fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            Text count = CreateText("Rack Count", slotObject.transform, "0/3", 14, new Color(0.30f, 0.19f, 0.34f), TextAnchor.MiddleCenter, new Vector2(0f, -54f), new Vector2(74f, 22f), FontStyle.Bold);
            RackEntry entry = new RackEntry
            {
                Color = color,
                Slot = slot,
                Progress = 0,
                SpoolRect = spoolRect,
                SpoolImage = spoolRect.GetComponent<Image>(),
                FillImage = fill,
                CountLabel = count
            };
            _rackEntries.Add(entry);
            UpdateRackEntry(entry);
            return entry;
        }

        return null;
    }

    private RackEntry FindRackEntryBySlot(int slot)
    {
        for (int index = 0; index < _rackEntries.Count; index++)
        {
            if (_rackEntries[index].Slot == slot)
            {
                return _rackEntries[index];
            }
        }
        return null;
    }

    private void UpdateRackEntry(RackEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.FillImage.fillAmount = entry.Progress / (float)CellsPerSpool;
        entry.CountLabel.text = entry.Progress + "/3";
        entry.SpoolImage.color = entry.Progress == 0 ? Color.white : Color.Lerp(Color.white, Palette[(int)entry.Color], 0.12f);
    }

    private void RemoveRackEntry(RackEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        _rackEntries.Remove(entry);
        Destroy(entry.SpoolRect.gameObject);
        Destroy(entry.FillImage.transform.parent.gameObject);
        Destroy(entry.CountLabel.gameObject);
    }

    private void TryUnlockFinalSlot()
    {
        if (_unlockedRackSlots >= RackCapacity || _totalCells == 0)
        {
            return;
        }

        if (_collectedCells >= Mathf.CeilToInt(_totalCells * 0.5f))
        {
            _unlockedRackSlots = RackCapacity;
            _rackSlotImages[RackCapacity - 1].color = new Color(0.97f, 0.93f, 0.99f);
            _rackSlotLabels[RackCapacity - 1].text = string.Empty;
            _rackLabel.text = "RACK  /  MAX 8  /  FINAL SLOT OPEN";
            ShowToast("FINAL RACK SLOT UNLOCKED", 1.5f);
        }
    }

    private bool IsDeadlocked()
    {
        if (_collectedCells >= _totalCells)
        {
            return false;
        }

        bool hasFreeSlot = _rackEntries.Count < _unlockedRackSlots;
        for (int tokenIndex = 0; tokenIndex < _spoolTokens.Count; tokenIndex++)
        {
            SpoolToken token = _spoolTokens[tokenIndex];
            if (token.Used)
            {
                continue;
            }

            if (ExposedCount(token.Color) <= 0)
            {
                continue;
            }

            if (FindRackEntry(token.Color) != null || hasFreeSlot)
            {
                return false;
            }
        }

        return _rackEntries.Count >= _unlockedRackSlots;
    }

    private int ExposedCount(YarnColor color)
    {
        int count = 0;
        for (int column = 0; column < _columns.Count; column++)
        {
            List<YarnCell> stack = _columns[column];
            if (stack.Count > 0 && stack[stack.Count - 1].Color == color)
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateBoardVisuals()
    {
        for (int index = 0; index < _allCells.Count; index++)
        {
            YarnCell cell = _allCells[index];
            if (!cell.Active)
            {
                cell.Group.alpha = 0f;
                continue;
            }

            bool exposed = IsExposed(cell);
            cell.Group.alpha = exposed ? 1f : 0.58f;
            cell.Image.color = exposed ? Color.white : new Color(0.72f, 0.72f, 0.80f, 1f);
        }
    }

    private bool IsExposed(YarnCell cell)
    {
        List<YarnCell> stack = _columns[cell.Column];
        return stack.Count > 0 && stack[stack.Count - 1] == cell;
    }

    private void UpdateHeader()
    {
        int remaining = _totalCells - _collectedCells;
        _remainingLabel.text = "YARN REMAINING  " + remaining.ToString("00");
        _progressFill.fillAmount = _totalCells == 0 ? 0f : _collectedCells / (float)_totalCells;
        if (_unlockedRackSlots < RackCapacity)
        {
            _rackLabel.text = "RACK  /  7 OPEN  /  FINAL SLOT AT 50%";
        }
    }

    private void UpdateControls()
    {
        bool canInteract = _state == GameState.Playing;
        _restartButton.interactable = _state != GameState.Resolving;
        _hintButton.interactable = canInteract;
        for (int index = 0; index < _spoolButtons.Count; index++)
        {
            if (!_spoolTokens[index].Used)
            {
                _spoolButtons[index].interactable = canInteract;
            }
        }
    }

    private void ShowHint()
    {
        if (_state != GameState.Playing)
        {
            return;
        }

        int bestIndex = -1;
        int bestScore = -1;
        for (int index = 0; index < _spoolTokens.Count; index++)
        {
            SpoolToken token = _spoolTokens[index];
            if (token.Used)
            {
                continue;
            }

            int score = ExposedCount(token.Color);
            if (FindRackEntry(token.Color) != null)
            {
                score += 5;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        if (bestIndex < 0)
        {
            ShowToast("NO UNUSED SPOOL", 1.1f);
            return;
        }

        StartCoroutine(PulseButton(_spoolButtons[bestIndex]));
        ShowToast("TRY " + ColorNames[(int)_spoolTokens[bestIndex].Color], 1.25f);
    }

    private IEnumerator PulseButton(Button button)
    {
        RectTransform rect = button.transform as RectTransform;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;
        while (elapsed < 1.2f && button != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float wave = 1f + Mathf.Sin(elapsed * 18f) * 0.10f;
            rect.localScale = baseScale * wave;
            yield return null;
        }

        if (rect != null)
        {
            rect.localScale = baseScale;
        }
    }

    private IEnumerator PulseRackSlot(int slot)
    {
        if (slot < 0 || slot >= _rackSlots.Count)
        {
            yield break;
        }

        RectTransform rect = _rackSlots[slot].transform as RectTransform;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;
        while (elapsed < 0.24f)
        {
            elapsed += Time.unscaledDeltaTime;
            float wave = 1f + Mathf.Sin((elapsed / 0.24f) * Mathf.PI) * 0.10f;
            rect.localScale = baseScale * wave;
            yield return null;
        }
        rect.localScale = baseScale;
    }

    private IEnumerator AnimateCellToRack(YarnCell cell, RackEntry entry)
    {
        if (cell == null || entry == null)
        {
            yield break;
        }

        Vector2 start = WorldToCanvas(cell.Rect.position);
        Vector2 target = WorldToCanvas(entry.SpoolRect.position);
        Image flying = GetFlyingImage();
        flying.sprite = YarnVisualFactory.GetCellSprite(Palette[(int)cell.Color]);
        flying.color = Color.white;
        flying.rectTransform.sizeDelta = new Vector2(66f, 66f);
        flying.rectTransform.anchoredPosition = start;
        flying.rectTransform.localScale = Vector3.one;

        TrailVisual trail = GetTrailVisual(Palette[(int)cell.Color]);
        float elapsed = 0f;
        const float duration = 0.32f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector2 control = Vector2.Lerp(start, target, 0.5f) + new Vector2(0f, 72f + Mathf.Sin(cell.Column * 1.7f) * 20f);
            Vector2 position = QuadraticBezier(start, control, target, EaseInOut(progress));
            flying.rectTransform.anchoredPosition = position;
            flying.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(progress * Mathf.PI * 2f) * 12f);
            flying.rectTransform.localScale = Vector3.one * (1f - progress * 0.34f);
            UpdateTrail(trail, start, target, progress);
            yield return null;
        }

        ReleaseFlyingImage(flying);
        ReleaseTrailVisual(trail);
    }

    private TrailVisual GetTrailVisual(Color color)
    {
        TrailVisual trail = new TrailVisual();
        for (int index = 0; index < 8; index++)
        {
            Image segment = GetTrailImage();
            segment.color = new Color(color.r, color.g, color.b, 0.88f);
            segment.rectTransform.sizeDelta = new Vector2(10f, 4f);
            trail.Segments.Add(segment.rectTransform);
        }
        return trail;
    }

    private void UpdateTrail(TrailVisual trail, Vector2 start, Vector2 target, float progress)
    {
        Vector2 control = Vector2.Lerp(start, target, 0.5f) + new Vector2(0f, 60f);
        for (int index = 0; index < trail.Segments.Count; index++)
        {
            float from = index / (float)trail.Segments.Count;
            float to = (index + 1) / (float)trail.Segments.Count;
            float visible = Mathf.Clamp01((progress - from) / Mathf.Max(0.01f, to - from));
            RectTransform segment = trail.Segments[index];
            segment.gameObject.SetActive(visible > 0.01f);
            if (visible <= 0.01f)
            {
                continue;
            }

            Vector2 a = QuadraticBezier(start, control, target, from);
            Vector2 b = QuadraticBezier(start, control, target, Mathf.Lerp(from, to, visible));
            Vector2 delta = b - a;
            segment.anchoredPosition = Vector2.Lerp(a, b, 0.5f);
            segment.sizeDelta = new Vector2(Mathf.Max(5f, delta.magnitude), 4f);
            segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    private IEnumerator ShowResult(bool won, string title, string detail)
    {
        _state = won ? GameState.Won : GameState.Lost;
        _resultTitle.text = title;
        _resultTitle.color = won ? new Color(1f, 0.82f, 0.34f) : new Color(0.98f, 0.45f, 0.55f);
        _resultDetail.text = detail;
        _resultOverlay.SetActive(true);
        UpdateControls();
        yield return null;
    }

    private void RequestRestart()
    {
        if (_state == GameState.Resolving)
        {
            return;
        }

        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        _state = GameState.Resolving;
        UpdateControls();
        yield return null;
        StartNewGame();
    }

    private void ShowToast(string message, float duration)
    {
        if (_toastRoutine != null)
        {
            StopCoroutine(_toastRoutine);
        }

        _toastRoutine = StartCoroutine(ToastRoutine(message, duration));
    }

    private IEnumerator ToastRoutine(string message, float duration)
    {
        _toastLabel.text = message;
        _toastLabel.gameObject.SetActive(true);
        Color baseColor = _toastLabel.color;
        _toastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        float elapsed = 0f;
        while (elapsed < 0.16f)
        {
            elapsed += Time.unscaledDeltaTime;
            _toastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(elapsed / 0.16f));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(duration);
        elapsed = 0f;
        while (elapsed < 0.20f)
        {
            elapsed += Time.unscaledDeltaTime;
            _toastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - Mathf.Clamp01(elapsed / 0.20f));
            yield return null;
        }
        _toastLabel.gameObject.SetActive(false);
    }

    private Vector2 BoardPosition(int column, int row)
    {
        float step = 92f;
        return new Vector2((column - (BoardColumns - 1) * 0.5f) * step, -20f + (row - (BoardRows - 1) * 0.5f) * step);
    }

    private Vector2 WorldToCanvas(Vector3 worldPosition)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * a + 2f * inverse * t * b + t * t * c;
    }

    private static float EaseInOut(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private Image GetFlyingImage()
    {
        Image image = _flyingImagePool.Count > 0 ? _flyingImagePool.Dequeue() : CreatePooledImage("Pooled Yarn", _effectsRoot);
        image.gameObject.SetActive(true);
        return image;
    }

    private Image GetTrailImage()
    {
        Image image = _trailImagePool.Count > 0 ? _trailImagePool.Dequeue() : CreatePooledImage("Pooled Thread", _effectsRoot);
        image.gameObject.SetActive(true);
        return image;
    }

    private void ReleaseFlyingImage(Image image)
    {
        image.gameObject.SetActive(false);
        _flyingImagePool.Enqueue(image);
    }

    private void ReleaseTrailVisual(TrailVisual trail)
    {
        for (int index = 0; index < trail.Segments.Count; index++)
        {
            Image image = trail.Segments[index].GetComponent<Image>();
            image.gameObject.SetActive(false);
            _trailImagePool.Enqueue(image);
        }
    }

    private Image CreatePooledImage(string name, Transform parent)
    {
        Image image = CreateImage(name, parent, YarnVisualFactory.GetSolidSprite(), Color.white, new Vector2(20f, 20f), Vector2.zero, false);
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private void ClearEffects()
    {
        for (int index = 0; index < _effectsRoot.childCount; index++)
        {
            _effectsRoot.GetChild(index).gameObject.SetActive(false);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Destroy(parent.GetChild(index).gameObject);
        }
    }

    private static void Shuffle<T>(IList<T> list, int seed)
    {
        System.Random random = new System.Random(seed);
        for (int index = list.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            T value = list[index];
            list[index] = list[other];
            list[other] = value;
        }
    }

    private void CreateEventSystemIfNeeded()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private static GameObject CreateChild(string name, Transform parent)
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

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 position, bool sliced)
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

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color textColor, Color backgroundColor)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = YarnVisualFactory.GetPanelSprite();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.48f, 0.48f, 0.52f, 0.65f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            Text text = CreateText("Button Label", buttonObject.transform, label, 21, textColor, TextAnchor.MiddleCenter, Vector2.zero, size - new Vector2(18f, 10f), FontStyle.Bold);
            text.raycastTarget = false;
        }

        return button;
    }

    private static Text CreateText(string name, Transform parent, string content, int size, Color color, TextAnchor anchor, Vector2 position, Vector2 dimensions, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void AddTextOutline(Text text, Color color, float distance)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private static class YarnVisualFactory
    {
        private static readonly Dictionary<int, Sprite> CellSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> SpoolSprites = new Dictionary<int, Sprite>();
        private static Sprite _solidSprite;
        private static Sprite _panelSprite;

        public static Sprite GetSolidSprite()
        {
            if (_solidSprite == null)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                texture.filterMode = FilterMode.Bilinear;
                _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            }
            return _solidSprite;
        }

        public static Sprite GetPanelSprite()
        {
            if (_panelSprite == null)
            {
                Texture2D texture = CreateRoundedTexture(64, 64, 14);
                _panelSprite = Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18f, 18f, 18f, 18f));
            }
            return _panelSprite;
        }

        public static Sprite GetCellSprite(Color color)
        {
            int key = ColorUtility.ToHtmlStringRGB(color).GetHashCode();
            if (!CellSprites.TryGetValue(key, out Sprite sprite))
            {
                Texture2D texture = CreateYarnTexture(color);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                CellSprites.Add(key, sprite);
            }
            return sprite;
        }

        public static Sprite GetSpoolSprite(Color color)
        {
            int key = ColorUtility.ToHtmlStringRGB(color).GetHashCode();
            if (!SpoolSprites.TryGetValue(key, out Sprite sprite))
            {
                Texture2D texture = CreateSpoolTexture(color);
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                SpoolSprites.Add(key, sprite);
            }
            return sprite;
        }

        private static Texture2D CreateRoundedTexture(int width, int height, int radius)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - halfWidth) - (halfWidth - radius);
                    float dy = Mathf.Abs(y + 0.5f - halfHeight) - (halfHeight - radius);
                    float distance = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - radius;
                    pixels[y * width + x] = distance < 0f ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Texture2D CreateYarnTexture(Color baseColor)
        {
            const int size = 72;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - 10f);
                    float dy = Mathf.Abs(y + 0.5f - half) - (half - 10f);
                    float distance = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) + Mathf.Min(Mathf.Max(dx, dy), 0f) - 10f;
                    if (distance > 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float edge = Mathf.Clamp01(-distance / 5f);
                    Color color = Color.Lerp(baseColor * 0.68f, baseColor, edge);
                    float thread = Mathf.Sin((x * 0.45f) + (y * 0.72f));
                    if (thread > 0.68f)
                    {
                        color = Color.Lerp(color, Color.white, 0.11f);
                    }
                    if (Mathf.Abs(Mathf.Sin(y * 0.43f)) > 0.93f)
                    {
                        color *= 0.80f;
                    }
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private static Texture2D CreateSpoolTexture(Color baseColor)
        {
            const int width = 82;
            const int height = 92;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float bodyWidth = y > 18f && y < 75f ? 25f : 0f;
                    bool body = bodyWidth > 0f && Mathf.Abs(point.x - center.x) <= bodyWidth;
                    float topEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 18f) * (point.y - 18f)) / (10f * 10f);
                    float bottomEllipse = ((point.x - center.x) * (point.x - center.x)) / (28f * 28f) + ((point.y - 75f) * (point.y - 75f)) / (10f * 10f);
                    if (!body && topEllipse > 1f && bottomEllipse > 1f)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    Color color = baseColor;
                    if (body && Mathf.Abs(Mathf.Sin(y * 0.55f)) > 0.72f)
                    {
                        color *= 0.76f;
                    }
                    if (topEllipse <= 1f)
                    {
                        color = Color.Lerp(baseColor, Color.white, 0.30f);
                        float hole = ((point.x - center.x) * (point.x - center.x)) / (10f * 10f) + ((point.y - 18f) * (point.y - 18f)) / (4f * 4f);
                        if (hole < 1f)
                        {
                            color = new Color(0.12f, 0.08f, 0.16f, 1f);
                        }
                    }
                    if (bottomEllipse <= 1f)
                    {
                        color = Color.Lerp(baseColor * 0.76f, baseColor, 0.45f);
                    }
                    pixels[y * width + x] = new Color(color.r, color.g, color.b, 1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
