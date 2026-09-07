using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

internal sealed class YarnMatchUiRenderer
{
    private readonly YarnMatchUiReferences _ui;
    private readonly YarnMatchAudio _audio;
    private readonly Action<YarnMatchSpoolToken> _onSpoolSelected;
    private readonly Dictionary<YarnMatchBoardCell, YarnMatchBoardCellView> _boardViews = new Dictionary<YarnMatchBoardCell, YarnMatchBoardCellView>();
    private readonly Dictionary<YarnMatchPoolCell, YarnMatchPoolCellView> _poolCellViews = new Dictionary<YarnMatchPoolCell, YarnMatchPoolCellView>();
    private readonly Dictionary<YarnMatchSpoolToken, YarnMatchPoolTokenView> _poolTokenViews = new Dictionary<YarnMatchSpoolToken, YarnMatchPoolTokenView>();
    private readonly Dictionary<YarnMatchTunnel, YarnMatchTunnelView> _tunnelViews = new Dictionary<YarnMatchTunnel, YarnMatchTunnelView>();
    private readonly Dictionary<YarnMatchChain, YarnMatchChainView> _chainViews = new Dictionary<YarnMatchChain, YarnMatchChainView>();
    private readonly Dictionary<YarnMatchRackEntry, YarnMatchRackEntryView> _rackViews = new Dictionary<YarnMatchRackEntry, YarnMatchRackEntryView>();
    private readonly HashSet<YarnMatchRackEntry> _visibleRackEntries = new HashSet<YarnMatchRackEntry>();
    private readonly List<GameObject> _rackSlots = new List<GameObject>();
    private readonly List<Image> _rackSlotImages = new List<Image>();
    private readonly List<TMP_Text> _rackSlotLabels = new List<TMP_Text>();
    private int _poolColumns = YarnMatchPoolModel.DefaultColumns;
    private int _poolRows = YarnMatchPoolModel.DefaultRows;
    private int _boardColumns = YarnMatchUiTheme.BoardColumns;
    private int _boardRows = YarnMatchUiTheme.BoardRows;
    private float _boardCellSize = YarnMatchUiTheme.BoardCellSize;
    private bool _isLargeBoard;
    private bool _fitBoardToViewport;
    private YarnMatchPoolModel _currentPool;
    private YarnMatchGameState _currentState = YarnMatchGameState.Playing;
    private bool _selectionEnabled = true;
    private YarnMatchBoardCell _previewCell;
    private YarnMatchColor _previewColor;
    private bool _hasPreviewColor;

    internal YarnMatchUiRenderer(YarnMatchUiReferences ui, YarnMatchAudio audio, Action<YarnMatchSpoolToken> onSpoolSelected)
    {
        _ui = ui;
        _audio = audio;
        _onSpoolSelected = onSpoolSelected;
    }

    internal void ResetGame(YarnMatchBoardModel board, YarnMatchPoolModel pool, YarnMatchRackModel rack)
    {
        _poolColumns = pool.Columns;
        _poolRows = pool.Rows;
        _currentPool = pool;
        _currentState = YarnMatchGameState.Playing;
        _selectionEnabled = true;
        _previewCell = null;
        _hasPreviewColor = false;
        ConfigureBoardLayout(board);
        YarnMatchUiPrimitives.ClearChildren(_ui.BoardCellsRoot);
        YarnMatchUiPrimitives.ClearChildren(_ui.RackRoot);
        YarnMatchUiPrimitives.ClearChildren(_ui.PoolRoot);
        _boardViews.Clear();
        _rackViews.Clear();
        _visibleRackEntries.Clear();
        _poolCellViews.Clear();
        _poolTokenViews.Clear();
        _tunnelViews.Clear();
        _chainViews.Clear();
        _rackSlots.Clear();
        _rackSlotImages.Clear();
        _rackSlotLabels.Clear();

        SetBoardGridVisibility(board);
        BuildBoardViews(board);
        BuildRackSlots(rack);
        BuildPoolSlots(pool);
        RenderBoard(board, true);
        RenderPool(pool, YarnMatchGameState.Playing);
        RenderRack(rack);
        UpdateHeader(board, rack);
    }

    internal void RenderBoard(YarnMatchBoardModel board, bool snapPositions)
    {
        for (int index = 0; index < board.AllCells.Count; index++)
        {
            YarnMatchBoardCell cell = board.AllCells[index];
            YarnMatchBoardCellView view = _boardViews[cell];
            if (!cell.Active)
            {
                view.Group.alpha = 0f;
                view.Group.blocksRaycasts = false;
                continue;
            }

            view.Group.alpha = 1f;
            view.Group.blocksRaycasts = true;
            view.Image.color = YarnMatchUiTheme.Palette[(int)cell.Color];
            if (snapPositions)
            {
                for (int strandIndex = 0; strandIndex < view.Strands.Count; strandIndex++)
                {
                    view.Strands[strandIndex].Group.alpha = 0f;
                    view.Strands[strandIndex].Rect.gameObject.SetActive(false);
                }
                view.Rect.localScale = Vector3.one;
                view.Rect.anchoredPosition = BoardPosition(cell.Column, cell.Row);
            }
        }

        if (_previewCell != null && !_previewCell.Active)
        {
            ClearColorPreview();
        }
    }

    internal void RenderPool(YarnMatchPoolModel pool, YarnMatchGameState state)
    {
        _currentPool = pool;
        _currentState = state;
        _poolColumns = pool.Columns;
        _poolRows = pool.Rows;
        _poolTokenViews.Clear();
        for (int index = 0; index < pool.Cells.Count; index++)
        {
            UpdatePoolCellView(pool.Cells[index], state);
        }

        for (int index = 0; index < pool.Tunnels.Count; index++)
        {
            UpdateTunnelView(pool.Tunnels[index]);
        }
        for (int index = 0; index < pool.Chains.Count; index++)
        {
            UpdateChainView(pool.Chains[index]);
        }

        ApplyColorPreview();
    }

    internal void RenderPoolCell(YarnMatchPoolModel pool, YarnMatchPoolCell cell, YarnMatchGameState state)
    {
        if (pool == null || cell == null || !_poolCellViews.ContainsKey(cell))
        {
            return;
        }

        _currentPool = pool;
        _currentState = state;
        _poolColumns = pool.Columns;
        _poolRows = pool.Rows;
        UpdatePoolCellView(cell, state);
        if (cell.Tunnel != null)
        {
            UpdateTunnelView(cell.Tunnel);
        }
        if (cell.SourceTunnel != null && cell.SourceTunnel != cell.Tunnel)
        {
            UpdateTunnelView(cell.SourceTunnel);
        }
        if (cell.ChainId >= 0)
        {
            for (int index = 0; index < pool.Chains.Count; index++)
            {
                if (pool.Chains[index].Id == cell.ChainId)
                {
                    UpdateChainView(pool.Chains[index]);
                    break;
                }
            }
        }
        ApplyColorPreview();
    }

    private void UpdatePoolCellView(YarnMatchPoolCell cell, YarnMatchGameState state)
    {
        if (!_poolCellViews.TryGetValue(cell, out YarnMatchPoolCellView cellView))
        {
            return;
        }

        UpdatePoolSlotBackground(cell, cellView);
        UpdateFreezeView(cell, cellView);
        RemovePoolTokenViewsForButton(cellView.Button, cell.Token);

        YarnMatchSpoolToken token = cell.Token;
        bool visible = token != null && !token.Used;
        cellView.Button.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        cellView.SpoolImage.sprite = YarnMatchVisualFactory.GetSpoolSprite(YarnMatchUiTheme.Palette[(int)token.Color]);
        cellView.SpoolImage.color = cell.Unlocked
            ? Color.white
            : new Color(0.64f, 0.68f, 0.78f, 0.80f);
        float baseAlpha = cell.Unlocked ? 1f : 0.80f;
        cellView.BaseAlpha = baseAlpha;
        cellView.Group.alpha = baseAlpha;
        cellView.Outline.enabled = false;
        cellView.Button.interactable = state == YarnMatchGameState.Playing && _selectionEnabled && _currentPool.IsSelectable(token);
        _poolTokenViews[token] = new YarnMatchPoolTokenView
        {
            Button = cellView.Button,
            Image = cellView.SpoolImage,
            Group = cellView.Group,
            Outline = cellView.Outline,
            BaseAlpha = baseAlpha
        };
    }

    private static void UpdatePoolSlotBackground(YarnMatchPoolCell cell, YarnMatchPoolCellView cellView)
    {
        bool lockedToken = cell.Token != null && !cell.Unlocked;
        cellView.ShadowImage.enabled = lockedToken;
        cellView.SlotImage.color = lockedToken
            ? new Color(0.52f, 0.45f, 0.64f, 0.90f)
            : new Color(0.96f, 0.98f, 1f);
    }

    private void RemovePoolTokenViewsForButton(Button button, YarnMatchSpoolToken keep)
    {
        List<YarnMatchSpoolToken> staleTokens = new List<YarnMatchSpoolToken>();
        foreach (KeyValuePair<YarnMatchSpoolToken, YarnMatchPoolTokenView> pair in _poolTokenViews)
        {
            if (pair.Key != keep && pair.Value != null && pair.Value.Button == button)
            {
                staleTokens.Add(pair.Key);
            }
        }

        for (int index = 0; index < staleTokens.Count; index++)
        {
            _poolTokenViews.Remove(staleTokens[index]);
        }
    }
    internal void RenderRack(YarnMatchRackModel rack)
    {
        UpdateRackSlotViews(rack);
        HashSet<YarnMatchRackEntry> activeEntries = new HashSet<YarnMatchRackEntry>();
        for (int index = 0; index < rack.Entries.Count; index++)
        {
            YarnMatchRackEntry entry = rack.Entries[index];
            if (!_visibleRackEntries.Contains(entry))
            {
                continue;
            }
            activeEntries.Add(entry);
            if (!_rackViews.ContainsKey(entry))
            {
                _rackViews.Add(entry, CreateRackEntryView(entry));
            }
            UpdateRackEntryView(entry);
        }

        List<YarnMatchRackEntry> stale = new List<YarnMatchRackEntry>();
        foreach (KeyValuePair<YarnMatchRackEntry, YarnMatchRackEntryView> pair in _rackViews)
        {
            if (!activeEntries.Contains(pair.Key))
            {
                DestroyRackEntryView(pair.Value);
                stale.Add(pair.Key);
            }
        }
        for (int index = 0; index < stale.Count; index++)
        {
            _rackViews.Remove(stale[index]);
            _visibleRackEntries.Remove(stale[index]);
        }
    }

    internal void UpdateHeader(YarnMatchBoardModel board, YarnMatchRackModel rack)
    {
        int remaining = board.TotalCells - board.CollectedCells;
        _ui.RemainingLabel.text = "剩余线团 " + remaining.ToString("00");
        _ui.ProgressFill.fillAmount = board.TotalCells == 0 ? 0f : board.CollectedCells / (float)board.TotalCells;
        _ui.RackLabel.text = rack.UnlockedSlots < YarnMatchUiTheme.RackCapacity
            ? "收线台  /  已开放 7 格  /  收集一半后解锁最后一格"
            : "收线台  /  8 格全部开放";
    }

    internal void SetLevel(int level)
    {
        _ui.LevelLabel.text = "第 " + level + " 关";
    }

    internal void SetInteraction(YarnMatchPoolModel pool, YarnMatchGameState state, bool refreshAvailable)
    {
        _currentPool = pool;
        _currentState = state;
        _ui.RestartButton.interactable = state != YarnMatchGameState.Resolving;
        _ui.HintButton.interactable = state == YarnMatchGameState.Playing;
        _ui.RefreshButton.interactable = state == YarnMatchGameState.Playing && refreshAvailable;
        for (int index = 0; index < pool.Cells.Count; index++)
        {
            YarnMatchPoolCell cell = pool.Cells[index];
            if (_poolCellViews.TryGetValue(cell, out YarnMatchPoolCellView cellView))
            {
                UpdatePoolCellView(cell, state);
            }
            if (cell.Token != null && _poolTokenViews.TryGetValue(cell.Token, out YarnMatchPoolTokenView view))
            {
                view.Button.interactable = state == YarnMatchGameState.Playing && pool.IsSelectable(cell.Token);
            }
        }

        for (int index = 0; index < pool.Chains.Count; index++)
        {
            UpdateChainView(pool.Chains[index]);
        }

        ApplyColorPreview();
    }

    internal void SetSpoolSelection(YarnMatchPoolModel pool, YarnMatchGameState state, bool enabled)
    {
        _currentPool = pool;
        _currentState = state;
        _selectionEnabled = enabled;
        for (int index = 0; index < pool.Cells.Count; index++)
        {
            YarnMatchPoolCell cell = pool.Cells[index];
            if (_poolCellViews.TryGetValue(cell, out YarnMatchPoolCellView cellView))
            {
                UpdatePoolCellView(cell, state);
            }
            if (cell.Token != null && _poolTokenViews.TryGetValue(cell.Token, out YarnMatchPoolTokenView view))
            {
                view.Button.interactable = enabled
                    && state == YarnMatchGameState.Playing
                    && pool.IsSelectable(cell.Token);
            }
        }

        ApplyColorPreview();
    }

    private void SetBoardCellPreview(YarnMatchBoardCell cell)
    {
        if (cell == null || !cell.Active || _currentPool == null || _currentState != YarnMatchGameState.Playing || !_selectionEnabled)
        {
            return;
        }

        bool colorChanged = !_hasPreviewColor || _previewColor != cell.Color;
        _previewCell = cell;
        _previewColor = cell.Color;
        _hasPreviewColor = true;
        if (colorChanged)
        {
            ApplyColorPreview();
        }
    }

    private void ClearBoardCellPreview(YarnMatchBoardCell cell, PointerEventData eventData)
    {
        if (_previewCell != cell)
        {
            return;
        }

        // PointerExit fires before the next cell's PointerEnter. Keep the preview
        // alive while the cursor is still inside the board to avoid a blank frame.
        if (IsPointerInsideBoardArea(eventData))
        {
            return;
        }

        ClearColorPreview();
    }

    private bool IsPointerInsideBoardArea(PointerEventData eventData)
    {
        return eventData != null
            && _ui.BoardArea != null
            && RectTransformUtility.RectangleContainsScreenPoint(
                _ui.BoardArea,
                eventData.position,
                eventData.enterEventCamera);
    }

    private void ClearColorPreview()
    {
        _previewCell = null;
        _hasPreviewColor = false;
        ApplyColorPreview();
    }

    private void ApplyColorPreview()
    {
        bool previewActive = _hasPreviewColor
            && _previewCell != null
            && _previewCell.Active
            && _currentPool != null
            && _currentState == YarnMatchGameState.Playing
            && _selectionEnabled;

        foreach (KeyValuePair<YarnMatchSpoolToken, YarnMatchPoolTokenView> pair in _poolTokenViews)
        {
            YarnMatchSpoolToken token = pair.Key;
            YarnMatchPoolTokenView view = pair.Value;
            if (view == null || view.Group == null || view.Outline == null)
            {
                continue;
            }

            bool selectable = previewActive
                && token != null
                && !token.Used
                && _currentPool.IsSelectable(token);
            bool matches = selectable && token.Color == _previewColor;
            // Keep the pool stable while hovering the board; only the matching outline changes.
            view.Group.alpha = view.BaseAlpha;
            view.Outline.enabled = matches;
        }
    }
    internal void HideSpoolImmediately(YarnMatchSpoolToken token)
    {
        if (token == null || !_poolTokenViews.TryGetValue(token, out YarnMatchPoolTokenView view) || view.Button == null)
        {
            return;
        }

        view.Button.gameObject.SetActive(false);
    }

    internal bool TryGetBoardCellView(YarnMatchBoardCell cell, out YarnMatchBoardCellView view)
    {
        return _boardViews.TryGetValue(cell, out view);
    }

    internal bool TryGetPoolTokenView(YarnMatchSpoolToken token, out YarnMatchPoolTokenView view)
    {
        return _poolTokenViews.TryGetValue(token, out view);
    }

    internal void ShowRackEntry(YarnMatchRackEntry entry)
    {
        if (entry != null)
        {
            _visibleRackEntries.Add(entry);
        }
    }

    internal void HideRackProgress(YarnMatchRackEntry entry)
    {
        if (entry == null || !_rackViews.TryGetValue(entry, out YarnMatchRackEntryView view))
        {
            return;
        }

        view.FillImage.transform.parent.gameObject.SetActive(false);
        view.CountLabel.gameObject.SetActive(false);
    }

    internal bool TryGetRackSlotCanvasPosition(int slot, out Vector2 position)
    {
        if (slot < 0 || slot >= _rackSlots.Count)
        {
            position = Vector2.zero;
            return false;
        }

        position = WorldToCanvas(_rackSlots[slot].transform.TransformPoint(new Vector3(0f, 11f, 0f)));
        return true;
    }
    internal bool TryGetRackEntryView(YarnMatchRackEntry entry, out YarnMatchRackEntryView view)
    {
        return _rackViews.TryGetValue(entry, out view);
    }

    internal Vector2 BoardPosition(int column, int row)
    {
        return YarnMatchUiTheme.BoardPosition(column, row, _boardColumns, _boardRows, _fitBoardToViewport);
    }

    internal Vector2 PoolSlotPosition(int column, int row)
    {
        return YarnMatchUiTheme.PoolSlotPosition(column, row, _poolColumns, _poolRows);
    }

    internal Vector2 WorldToCanvas(Vector3 worldPosition)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_ui.CanvasRect, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }

    private void ConfigureBoardLayout(YarnMatchBoardModel board)
    {
        _boardColumns = Mathf.Max(1, board.Columns.Count);
        _boardRows = 1;
        for (int index = 0; index < board.AllCells.Count; index++)
        {
            _boardRows = Mathf.Max(_boardRows, board.AllCells[index].Row + 1);
        }

        _isLargeBoard = true;
        _fitBoardToViewport = _boardColumns == _boardRows;
        _boardCellSize = YarnMatchUiTheme.BoardCellSizeFor(_boardColumns, _boardRows, _fitBoardToViewport);
    }

    private void BuildBoardViews(YarnMatchBoardModel board)
    {
        for (int index = 0; index < board.AllCells.Count; index++)
        {
            YarnMatchBoardCell cell = board.AllCells[index];
            Image image = YarnMatchUiPrimitives.CreateImage("Yarn Cell", _ui.BoardCellsRoot, YarnMatchVisualFactory.GetSolidSprite(), YarnMatchUiTheme.Palette[(int)cell.Color], Vector2.one * _boardCellSize, BoardPosition(cell.Column, cell.Row), false);
            image.raycastTarget = true;
            YarnMatchBoardCell hoverCell = cell;
            YarnMatchBoardCellHover hover = image.gameObject.AddComponent<YarnMatchBoardCellHover>();
            hover.Configure(
                eventData => SetBoardCellPreview(hoverCell),
                eventData => ClearBoardCellPreview(hoverCell, eventData));
            CanvasGroup group = image.gameObject.AddComponent<CanvasGroup>();
            YarnMatchBoardCellView view = new YarnMatchBoardCellView { Rect = image.rectTransform, Image = image, Group = group };
            int strandCount = YarnMatchUiTheme.BoardStrandCountForSize(_boardCellSize);
            for (int strandIndex = 0; strandIndex < strandCount; strandIndex++)
            {
                float strandSlot = _boardCellSize / strandCount;
                float strandHeight = Mathf.Max(0.8f, strandSlot * 0.72f);
                float y = -_boardCellSize * 0.5f + strandSlot * 0.5f + strandIndex * strandSlot;
                Image strandImage = YarnMatchUiPrimitives.CreateImage("Yarn Strand", image.transform, YarnMatchVisualFactory.GetThreadSprite(YarnMatchUiTheme.Palette[(int)cell.Color]), Color.white, new Vector2(_boardCellSize * 0.75f, strandHeight), new Vector2(0f, y), false);
                strandImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, strandIndex % 2 == 0 ? -8f : 8f);
                CanvasGroup strandGroup = strandImage.gameObject.AddComponent<CanvasGroup>();
                view.Strands.Add(new YarnMatchStrandView { Rect = strandImage.rectTransform, Group = strandGroup });
            }
            _boardViews.Add(cell, view);
        }
    }

    private void SetBoardGridVisibility(YarnMatchBoardModel board)
    {
        if (_isLargeBoard)
        {
            for (int childIndex = 0; childIndex < _ui.BoardGridRoot.childCount; childIndex++)
            {
                _ui.BoardGridRoot.GetChild(childIndex).gameObject.SetActive(false);
            }

            return;
        }

        int gridChildIndex = 0;
        for (int row = 0; row < YarnMatchUiTheme.BoardRows; row++)
        {
            for (int column = 0; column < YarnMatchUiTheme.BoardColumns; column++)
            {
                bool visible = column < board.Columns.Count && row < board.Columns[column].Count;
                if (gridChildIndex + 1 < _ui.BoardGridRoot.childCount)
                {
                    _ui.BoardGridRoot.GetChild(gridChildIndex).gameObject.SetActive(visible);
                    _ui.BoardGridRoot.GetChild(gridChildIndex + 1).gameObject.SetActive(visible);
                }
                gridChildIndex += 2;
            }
        }
    }

    private void BuildRackSlots(YarnMatchRackModel rack)
    {
        const float slotSize = 70f;
        const float step = 78f;
        float start = -step * (YarnMatchUiTheme.RackCapacity - 1) * 0.5f;
        for (int slot = 0; slot < YarnMatchUiTheme.RackCapacity; slot++)
        {
            GameObject slotObject = YarnMatchUiPrimitives.CreateChild("Rack Slot " + slot, _ui.RackRoot);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(slotSize, 86f);
            slotRect.anchoredPosition = new Vector2(start + slot * step, -5f);
            YarnMatchUiPrimitives.CreateImage("Rack Slot Shadow", slotObject.transform, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.28f, 0.36f, 0.52f, 0.16f), new Vector2(slotSize + 6f, 86f), new Vector2(3f, -4f), true);
            Image slotImage = YarnMatchUiPrimitives.CreateImage("Rack Slot", slotObject.transform, YarnMatchVisualFactory.GetPanelSprite(), slot < rack.UnlockedSlots ? new Color(0.96f, 0.98f, 1f) : new Color(0.78f, 0.82f, 0.90f), new Vector2(slotSize, 82f), Vector2.zero, true);
            TMP_Text label = YarnMatchUiPrimitives.CreateText("Slot Label", slotObject.transform, slot < rack.UnlockedSlots ? string.Empty : "锁定", 11, new Color(0.46f, 0.52f, 0.64f), TextAlignmentOptions.Center, new Vector2(0f, -17f), new Vector2(68f, 24f), FontStyles.Bold);
            _rackSlots.Add(slotObject);
            _rackSlotImages.Add(slotImage);
            _rackSlotLabels.Add(label);
        }
    }

    private void BuildPoolSlots(YarnMatchPoolModel pool)
    {
        float cellSize = YarnMatchUiTheme.PoolCellSize(_poolColumns, _poolRows);
        _ui.PoolSlotsRoot = YarnMatchUiPrimitives.CreateChild("Pool Slots", _ui.PoolRoot).transform;
        _ui.PoolButtonsRoot = YarnMatchUiPrimitives.CreateChild("Pool Buttons", _ui.PoolRoot).transform;
        for (int index = 0; index < pool.Cells.Count; index++)
        {
            YarnMatchPoolCell cell = pool.Cells[index];
            Vector2 position = PoolSlotPosition(cell.Column, cell.Row);
            Image shadowImage = YarnMatchUiPrimitives.CreateImage("Pool Tile Shadow", _ui.PoolSlotsRoot, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.28f, 0.36f, 0.52f, 0.16f), new Vector2(cellSize + 4f, cellSize + 4f), position + new Vector2(cellSize * 0.05f, -cellSize * 0.07f), true);
            Image slotImage = YarnMatchUiPrimitives.CreateImage("Pool Tile", _ui.PoolSlotsRoot, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.96f, 0.98f, 1f), new Vector2(cellSize, cellSize), position, true);

            Button button = YarnMatchUiPrimitives.CreateButton("Pool Spool", _ui.PoolButtonsRoot, string.Empty, position, new Vector2(cellSize + 8f, cellSize + 8f), Color.white, new Color(1f, 1f, 1f, 0f));
            YarnMatchPoolCell sourceCell = cell;
            button.onClick.AddListener(() =>
            {
                YarnMatchSpoolToken token = sourceCell.Token;
                if (token == null || token.Used)
                {
                    return;
                }
                _audio?.PlayClick();
                _onSpoolSelected?.Invoke(token);
            });
            Image spoolImage = YarnMatchUiPrimitives.CreateImage("Spool", button.transform, YarnMatchVisualFactory.GetSpoolSprite(YarnMatchUiTheme.Palette[(int)YarnMatchColor.Coral]), Color.white, new Vector2(cellSize * 0.90f, cellSize * 0.98f), new Vector2(0f, cellSize * 0.03f), false);
            spoolImage.raycastTarget = false;
            Image freezeImage = YarnMatchUiPrimitives.CreateImage(
                "Freeze Overlay",
                button.transform,
                YarnMatchVisualFactory.GetFreezeSprite(0),
                Color.white,
                new Vector2(cellSize * 0.98f, cellSize * 0.98f),
                Vector2.zero,
                false);
            freezeImage.preserveAspect = true;
            freezeImage.raycastTarget = false;
            Outline spoolOutline = spoolImage.gameObject.AddComponent<Outline>();
            spoolOutline.effectColor = new Color(0.10f, 0.18f, 0.34f, 0.95f);
            spoolOutline.effectDistance = new Vector2(3f, 3f);
            spoolOutline.useGraphicAlpha = false;
            spoolOutline.enabled = false;
            CanvasGroup spoolGroup = button.gameObject.AddComponent<CanvasGroup>();
            button.gameObject.SetActive(false);
            _poolCellViews.Add(cell, new YarnMatchPoolCellView
            {
                ShadowImage = shadowImage,
                SlotImage = slotImage,
                Button = button,
                SpoolImage = spoolImage,
                FreezeImage = freezeImage,
                Group = spoolGroup,
                Outline = spoolOutline,
                BaseAlpha = 1f
            });
        }

        float badgeSize = Mathf.Clamp(cellSize * 0.48f, 18f, 30f);
        Vector2 badgePosition = new Vector2(cellSize * 0.31f, cellSize * 0.31f);
        int badgeFontSize = Mathf.Clamp(Mathf.RoundToInt(cellSize * 0.34f), 12, 20);
        for (int index = 0; index < pool.Tunnels.Count; index++)
        {
            YarnMatchTunnel tunnel = pool.Tunnels[index];
            GameObject tunnelObject = YarnMatchUiPrimitives.CreateChild("Tunnel Cell", _ui.PoolSlotsRoot);
            RectTransform rect = tunnelObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = PoolSlotPosition(tunnel.Target.Column, tunnel.Target.Row);

            Image tunnelImage = YarnMatchUiPrimitives.CreateImage(
                "Tunnel Pipe",
                tunnelObject.transform,
                YarnMatchVisualFactory.GetTunnelSprite(tunnel.Direction),
                Color.white,
                new Vector2(cellSize, cellSize),
                Vector2.zero,
                false);
            tunnelImage.preserveAspect = true;
            tunnelImage.raycastTarget = false;

            Image countBadge = YarnMatchUiPrimitives.CreateImage(
                "Tunnel Count Badge",
                tunnelObject.transform,
                YarnMatchVisualFactory.GetPanelSprite(),
                new Color(0.25f, 0.46f, 0.73f, 0.96f),
                new Vector2(badgeSize, badgeSize),
                badgePosition,
                true);
            TMP_Text count = YarnMatchUiPrimitives.CreateText(
                "Tunnel Count",
                tunnelObject.transform,
                string.Empty,
                badgeFontSize,
                Color.white,
                TextAlignmentOptions.Center,
                badgePosition,
                new Vector2(badgeSize, badgeSize),
                FontStyles.Bold);
            _tunnelViews.Add(tunnel, new YarnMatchTunnelView { CountLabel = count, CountBadge = countBadge });
            UpdateTunnelView(tunnel);
        }

        for (int index = 0; index < pool.Chains.Count; index++)
        {
            YarnMatchChain chain = pool.Chains[index];
            GameObject chainObject = YarnMatchUiPrimitives.CreateChild("Chain Overlay", _ui.PoolButtonsRoot);
            Image chainImage = YarnMatchUiPrimitives.CreateImage(
                "Chain",
                chainObject.transform,
                YarnMatchVisualFactory.GetChainSprite(true),
                Color.white,
                new Vector2(cellSize * 2f + YarnMatchUiTheme.PoolSlotGap, cellSize),
                Vector2.zero,
                false);
            chainImage.preserveAspect = true;
            chainImage.raycastTarget = false;
            _chainViews.Add(chain, new YarnMatchChainView
            {
                Rect = chainImage.rectTransform,
                Image = chainImage
            });
            UpdateChainView(chain);
        }
    }
    private YarnMatchRackEntryView CreateRackEntryView(YarnMatchRackEntry entry)
    {
        GameObject slotObject = _rackSlots[entry.Slot];
        RectTransform spoolRect = YarnMatchUiPrimitives.CreateImage("Rack Spool", slotObject.transform, YarnMatchVisualFactory.GetSpoolSprite(YarnMatchUiTheme.Palette[(int)entry.Color]), Color.white, new Vector2(54f, 62f), new Vector2(0f, 11f), false).rectTransform;
        CanvasGroup spoolGroup = spoolRect.gameObject.AddComponent<CanvasGroup>();
        Image fillBack = YarnMatchUiPrimitives.CreateImage("Rack Fill Back", slotObject.transform, YarnMatchVisualFactory.GetPanelSprite(), new Color(0.55f, 0.62f, 0.75f, 0.30f), new Vector2(58f, 6f), new Vector2(0f, -30f), true);
        Image fill = YarnMatchUiPrimitives.CreateImage("Rack Fill", fillBack.transform, YarnMatchVisualFactory.GetPanelSprite(), YarnMatchUiTheme.Palette[(int)entry.Color], new Vector2(58f, 6f), Vector2.zero, true);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        TMP_Text count = YarnMatchUiPrimitives.CreateText("Rack Count", slotObject.transform, "0/3", 10, new Color(0.25f, 0.32f, 0.46f), TextAlignmentOptions.Center, new Vector2(0f, -40f), new Vector2(64f, 20f), FontStyles.Bold);
        return new YarnMatchRackEntryView { SpoolRect = spoolRect, BaseScale = spoolRect.localScale, SpoolImage = spoolRect.GetComponent<Image>(), SpoolGroup = spoolGroup, FillImage = fill, CountLabel = count };
    }

    private void UpdateRackSlotViews(YarnMatchRackModel rack)
    {
        for (int slot = 0; slot < _rackSlotImages.Count; slot++)
        {
            bool unlocked = slot < rack.UnlockedSlots;
            _rackSlotImages[slot].color = unlocked ? new Color(0.98f, 0.99f, 1f) : new Color(0.78f, 0.82f, 0.90f);
            _rackSlotLabels[slot].text = unlocked ? string.Empty : "锁定";
            _rackSlotLabels[slot].color = unlocked ? new Color(0.46f, 0.52f, 0.64f) : new Color(0.38f, 0.45f, 0.58f);
        }
    }

    private void UpdateRackEntryView(YarnMatchRackEntry entry)
    {
        YarnMatchRackEntryView view = _rackViews[entry];
        view.FillImage.fillAmount = entry.Progress / (float)entry.Capacity;
        view.CountLabel.text = entry.Progress + "/" + entry.Capacity;
        view.SpoolImage.color = entry.Progress == 0 ? Color.white : Color.Lerp(Color.white, YarnMatchUiTheme.Palette[(int)entry.Color], 0.16f);
    }

    private void DestroyRackEntryView(YarnMatchRackEntryView view)
    {
        UnityEngine.Object.Destroy(view.SpoolRect.gameObject);
        UnityEngine.Object.Destroy(view.FillImage.transform.parent.gameObject);
        UnityEngine.Object.Destroy(view.CountLabel.gameObject);
    }

    private void UpdateTunnelView(YarnMatchTunnel tunnel)
    {
        if (!_tunnelViews.TryGetValue(tunnel, out YarnMatchTunnelView view))
        {
            return;
        }
        view.CountLabel.text = tunnel.Queue.Count.ToString();
        bool hasQueuedSpool = tunnel.Queue.Count > 0;
        view.CountLabel.color = hasQueuedSpool ? Color.white : new Color(1f, 1f, 1f, 0.52f);
        view.CountBadge.color = hasQueuedSpool
            ? new Color(0.25f, 0.46f, 0.73f, 0.96f)
            : new Color(0.36f, 0.48f, 0.63f, 0.70f);
    }

    private static void UpdateFreezeView(YarnMatchPoolCell cell, YarnMatchPoolCellView cellView)
    {
        if (cellView.FreezeImage == null)
        {
            return;
        }

        bool frozen = cell != null
            && cell.Token != null
            && !cell.Token.Used
            && cell.FreezeHitsRemaining > 0;
        cellView.FreezeImage.enabled = frozen;
        if (frozen)
        {
            cellView.FreezeImage.sprite = YarnMatchVisualFactory.GetFreezeSprite(cell.FreezeHitsRemaining);
            cellView.FreezeImage.color = Color.white;
            cellView.FreezeImage.rectTransform.SetAsLastSibling();
        }
    }

    private void UpdateChainView(YarnMatchChain chain)
    {
        if (chain == null || !_chainViews.TryGetValue(chain, out YarnMatchChainView view) || view.Image == null)
        {
            return;
        }

        YarnMatchPoolCell first = chain.First;
        YarnMatchPoolCell second = chain.Second;
        bool visible = first != null
            && second != null
            && first.ChainId == chain.Id
            && second.ChainId == chain.Id
            && (first.Token != null || second.Token != null);
        view.Image.enabled = visible;
        if (!visible)
        {
            return;
        }

        Vector2 firstPosition = PoolSlotPosition(first.Column, first.Row);
        Vector2 secondPosition = PoolSlotPosition(second.Column, second.Row);
        Vector2 delta = secondPosition - firstPosition;
        float cellSize = YarnMatchUiTheme.PoolCellSize(_poolColumns, _poolRows);
        bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
        float width = Mathf.Abs(delta.x) + cellSize;
        float height = Mathf.Abs(delta.y) + cellSize;
        view.Rect.sizeDelta = new Vector2(Mathf.Max(cellSize, width), Mathf.Max(cellSize, height));
        view.Rect.anchoredPosition = (firstPosition + secondPosition) * 0.5f;
        view.Image.sprite = YarnMatchVisualFactory.GetChainSprite(
            horizontal,
            horizontal && (first.FreezeHitsRemaining > 0 || second.FreezeHitsRemaining > 0));
        view.Image.preserveAspect = true;
        view.Image.raycastTarget = false;
        view.Rect.SetAsLastSibling();
    }

}
