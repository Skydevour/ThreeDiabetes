using System;
using System.Collections;
using UnityEngine;

public sealed class YarnMatchPresentation : MonoBehaviour
{
    private YarnMatchAudio _audio;
    private YarnMatchUiReferences _ui;
    private YarnMatchUiBuilder _uiBuilder;
    private YarnMatchUiRenderer _renderer;
    private YarnMatchOverlayController _overlays;
    private YarnMatchEffectPool _effects;
    private YarnMatchAnimationController _animations;
    private bool _initialized;
    private GameObject _loadingOverlay;

    public void Initialize(
        Action<YarnMatchSpoolToken> onSpoolSelected,
        Action onRestart,
        Action onNextLevel,
        Action onHint,
        Action onRefresh,
        Action onOpenLevelSelect,
        Action<int> onLevelSelected,
        Action onUnlockAllLevels,
        Action onStartSpecialChallenge,
        Action onBackToMainMenu)
    {
        if (_initialized)
        {
            return;
        }

        _audio = GetComponent<YarnMatchAudio>();
        if (_audio == null)
        {
            _audio = gameObject.AddComponent<YarnMatchAudio>();
        }

        _uiBuilder = new YarnMatchUiBuilder(_audio);
        _ui = _uiBuilder.Build(
            onSpoolSelected,
            onRestart,
            onNextLevel,
            onHint,
            onRefresh,
            onOpenLevelSelect,
            onLevelSelected,
            onUnlockAllLevels,
            onStartSpecialChallenge,
            () => _overlays.ShowFailurePreview(),
            onBackToMainMenu);
        YarnMatchMusic music = GetComponent<YarnMatchMusic>();
        if (music == null) music = gameObject.AddComponent<YarnMatchMusic>();
        YarnMatchMusicControls.Build(_ui.MainMenuOverlay.transform, music);
        _renderer = new YarnMatchUiRenderer(_ui, _audio, onSpoolSelected);
        _effects = new YarnMatchEffectPool(_ui.EffectsRoot);
        _effects.Build();
        _animations = new YarnMatchAnimationController(_renderer, _effects, _audio);
        _overlays = new YarnMatchOverlayController(this, _ui, _audio);
        var loading = YarnMatchUiPrimitives.CreateScreenBackground("Round Loading", _ui.CanvasRect, Color.white, true);
        _loadingOverlay = loading.gameObject;
        YarnMatchUiPrimitives.CreateText("Loading", loading.transform, "正在整理毛线…", 24,
            new Color(0.22f, 0.25f, 0.30f), TMPro.TextAlignmentOptions.Center,
            Vector2.zero, new Vector2(400f, 60f), TMPro.FontStyles.Normal);
        _loadingOverlay.SetActive(false);
        _initialized = true;
    }

    public IEnumerator ResetGame(YarnMatchBoardModel board, YarnMatchPoolModel pool, YarnMatchRackModel rack)
    {
        _animations?.Reset();
        _effects.Clear();
        _overlays.ResetForGame();
        yield return _renderer.ResetGame(board, pool, rack);
        _overlays.SetStatus("选择底部露出的同色线团，线圈会逐根收紧");
    }

    public void ShowMainMenu()
    {
        SetLoading(false);
        _overlays.ShowMainMenu();
    }

    public void SetLoading(bool visible) => _loadingOverlay.SetActive(visible);

    public void ShowLevelSelect(int highestUnlockedLevel, int focusLevel, bool dailyUnlockActive)
    {
        _overlays.ShowLevelSelect(highestUnlockedLevel, focusLevel, dailyUnlockActive);
    }

    public void SetLevel(int level)
    {
        _renderer.SetLevel(level);
    }

    public void CancelAnimations()
    {
        _animations?.Reset();
        _effects?.Clear();
    }

    public void RenderPool(YarnMatchPoolModel pool, YarnMatchGameState state)
    {
        _renderer.RenderPool(pool, state);
    }

    public void RenderPoolCell(YarnMatchPoolModel pool, YarnMatchPoolCell cell, YarnMatchGameState state)
    {
        _renderer.RenderPoolCell(pool, cell, state);
    }

    public void ShowRackEntry(YarnMatchRackEntry entry)
    {
        _renderer.ShowRackEntry(entry);
    }
    public void RenderRack(YarnMatchRackModel rack)
    {
        _renderer.RenderRack(rack);
    }

    public void UpdateHeader(YarnMatchBoardModel board, YarnMatchRackModel rack)
    {
        _renderer.UpdateHeader(board, rack);
    }

    public void SetInteraction(YarnMatchPoolModel pool, YarnMatchGameState state, bool refreshAvailable)
    {
        _renderer.SetInteraction(pool, state, refreshAvailable);
    }

    public void SetSpoolSelection(YarnMatchPoolModel pool, YarnMatchGameState state, bool enabled)
    {
        _renderer.SetSpoolSelection(pool, state, enabled);
    }
    public void HideSpoolImmediately(YarnMatchSpoolToken token)
    {
        _renderer.HideSpoolImmediately(token);
    }

    public void SetStatus(string message)
    {
        _overlays.SetStatus(message);
    }

    public void ShowToast(string message, float duration)
    {
        _overlays.ShowToast(message, duration);
    }

    public void ShowResult(bool won, string title, string detail, bool canAdvance)
    {
        _overlays.ShowResult(won, title, detail, canAdvance);
    }

    public IEnumerator PlaySpoolToRack(YarnMatchSpoolToken token, YarnMatchRackEntry entry)
    {
        return _animations.PlaySpoolToRack(token, entry);
    }

    public IEnumerator PlayCellIntoRack(YarnMatchBoardCell cell, YarnMatchRackEntry entry)
    {
        return _animations.PlayCellIntoRack(cell, entry);
    }

    public IEnumerator PlayColumnDrop(int column)
    {
        return _animations.PlayColumnDrop(column);
    }

    public IEnumerator PlayRackPulse(YarnMatchRackEntry entry)
    {
        return _animations.PlayRackPulse(entry);
    }

    public IEnumerator PlayPoolEmergence(YarnMatchSpoolToken token)
    {
        return _animations.PlayPoolEmergence(token);
    }

    public IEnumerator PulseSpool(YarnMatchSpoolToken token)
    {
        return _animations.PulseSpool(token);
    }
    public IEnumerator PulseBoardCell(YarnMatchBoardCell cell)
    {
        return _animations.PulseBoardCell(cell);
    }

    private void OnDestroy()
    {
        _overlays?.Stop();
    }
}
