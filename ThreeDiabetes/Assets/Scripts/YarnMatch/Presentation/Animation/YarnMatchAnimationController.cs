using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchAnimationController
{
    private readonly YarnMatchUiRenderer _renderer;
    private readonly YarnMatchEffectPool _effects;
    private readonly YarnMatchAudio _audio;
    private int _generation;
    private readonly YarnMatchBoardAnimationController _boardAnimations;

    internal YarnMatchAnimationController(YarnMatchUiRenderer renderer, YarnMatchEffectPool effects, YarnMatchAudio audio)
    {
        _renderer = renderer;
        _effects = effects;
        _audio = audio;
        _boardAnimations = new YarnMatchBoardAnimationController(renderer, effects, audio);
    }

    internal void Reset()
    {
        _generation++;
        _boardAnimations.Reset();
    }

    internal IEnumerator PlaySpoolToRack(YarnMatchSpoolToken token, YarnMatchRackEntry entry)
    {
        int generation = _generation;
        if (!_renderer.TryGetPoolTokenView(token, out YarnMatchPoolTokenView tokenView)
            || entry == null
            || !_renderer.TryGetRackSlotCanvasPosition(entry.Slot, out Vector2 target))
        {
            yield break;
        }

        if (!_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView rackView))
        {
            yield break;
        }

        rackView.SpoolGroup.alpha = 0f;
        _renderer.SetRackProgressVisible(entry, false);

        Vector2 start = _renderer.WorldToCanvas(tokenView.Button.transform.position);
        Image flying = _effects.GetFlyingImage(token.Capacity);
        flying.sprite = YarnMatchVisualFactory.GetSpoolSprite(YarnMatchUiTheme.Palette[(int)token.Color]);
        flying.rectTransform.sizeDelta = new Vector2(62f, 70f);
        flying.rectTransform.anchoredPosition = start;
        float elapsed = 0f;
        const float duration = 0.30f;
        while (elapsed < duration && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / duration));
            flying.rectTransform.anchoredPosition = Vector2.Lerp(start, target, progress);
            flying.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.78f, progress);
            yield return null;
        }

        if (generation == _generation && flying != null)
        {
            flying.rectTransform.anchoredPosition = target;
            _effects.ReleaseFlyingImage(flying);
            rackView.SpoolGroup.alpha = 1f;
            _renderer.SetRackProgressVisible(entry, true);
        }
    }
    internal IEnumerator PlayCellIntoRack(YarnMatchBoardCell cell, YarnMatchRackEntry entry)
        => _boardAnimations.Collect(cell, entry);

    internal IEnumerator PlayColumnDrop(int column) => _boardAnimations.Drop(column);


    internal IEnumerator PlayRackPulse(YarnMatchRackEntry entry)
    {
        int generation = _generation;
        if (!_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView view))
        {
            yield break;
        }

        _renderer.SetRackProgressVisible(entry, false);
        RectTransform rect = view.SpoolRect;
        Vector3 baseScale = rect.localScale;
        Vector2 basePosition = rect.anchoredPosition;
        float elapsed = 0f;
        const float duration = 0.48f;
        while (elapsed < duration && rect != null && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = YarnMatchUiTheme.EaseInOut(progress);
            float bounce = Mathf.Sin(progress * Mathf.PI * 7f) * 4f * (1f - progress);
            rect.localScale = baseScale * Mathf.Lerp(1f, 1.34f, eased);
            rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Sin(progress * Mathf.PI) * 24f + bounce);
            view.SpoolGroup.alpha = 1f;
            yield return null;
        }
        if (generation == _generation && rect != null)
        {
            rect.localScale = baseScale;
            rect.anchoredPosition = basePosition;
            view.SpoolGroup.alpha = 0f;
        }
    }
    internal IEnumerator PlayPoolEmergence(YarnMatchSpoolToken token)
    {
        int generation = _generation;
        if (!_renderer.TryGetPoolTokenView(token, out YarnMatchPoolTokenView view) || token.Cell == null)
        {
            yield break;
        }

        RectTransform rect = view.Button.transform as RectTransform;
        YarnMatchButtonFeedback feedback = view.Button.GetComponent<YarnMatchButtonFeedback>();
        feedback?.SetAnimationDriven(true);
        Vector2 target = _renderer.PoolSlotPosition(token.Cell.Column, token.Cell.Row);
        YarnMatchTunnelDirection direction = token.Cell.SourceTunnel == null ? YarnMatchTunnelDirection.Up : token.Cell.SourceTunnel.Direction;
        const float emergenceDistance = 56f;
        const float minimumScale = 0.88f;
        const float maximumScale = 1.04f;
        Vector2 start = target - YarnMatchUiTheme.DirectionVector(direction) * emergenceDistance;
        rect.anchoredPosition = start;
        rect.localScale = Vector3.one * minimumScale;
        float elapsed = 0f;
        const float duration = 0.34f;
        while (elapsed < duration && rect != null && generation == _generation && !token.Used)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(start, target, progress);
            rect.localScale = Vector3.one * Mathf.Lerp(minimumScale, maximumScale, progress);
            yield return null;
        }

        if (generation == _generation) feedback?.SetAnimationDriven(false);
        if (generation == _generation && rect != null && !token.Used)
        {
            rect.anchoredPosition = target;
            rect.localScale = Vector3.one;
            _audio?.PlayTunnel();
        }
    }
    internal IEnumerator PulseBoardCell(YarnMatchBoardCell cell) => _boardAnimations.Pulse(cell);

    internal IEnumerator PulseSpool(YarnMatchSpoolToken token)
    {
        int generation = _generation;
        if (!_renderer.TryGetPoolTokenView(token, out YarnMatchPoolTokenView view))
        {
            yield break;
        }

        RectTransform rect = view.Button.transform as RectTransform;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;
        while (elapsed < 1f && view.Button != null && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = baseScale * (1f + Mathf.Sin(elapsed * 18f) * 0.10f);
            yield return null;
        }
        if (generation == _generation && rect != null)
        {
            rect.localScale = baseScale;
        }
    }

}
