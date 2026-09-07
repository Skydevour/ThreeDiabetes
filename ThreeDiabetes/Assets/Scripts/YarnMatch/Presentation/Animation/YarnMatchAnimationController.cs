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

    internal YarnMatchAnimationController(YarnMatchUiRenderer renderer, YarnMatchEffectPool effects, YarnMatchAudio audio)
    {
        _renderer = renderer;
        _effects = effects;
        _audio = audio;
    }

    internal void Reset()
    {
        _generation++;
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

        Vector2 start = _renderer.WorldToCanvas(tokenView.Button.transform.position);
        Image flying = _effects.GetFlyingImage();
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
        }
    }
    internal IEnumerator PlayCellIntoRack(YarnMatchBoardCell cell, YarnMatchRackEntry entry)
    {
        int generation = _generation;
        if (!_renderer.TryGetBoardCellView(cell, out YarnMatchBoardCellView cellView)
            || !_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView rackView))
        {
            yield break;
        }

        RectTransform cellRect = cellView.Rect;
        RectTransform spoolRect = rackView.SpoolRect;
        Vector2 target = _renderer.WorldToCanvas(rackView.SpoolRect.position);
        Vector2 cellBasePosition = cellRect.anchoredPosition;
        Vector3 cellBaseScale = cellRect.localScale;
        float startProgress = entry.Progress / (float)entry.Capacity;
        float targetProgress = (entry.Progress + 1) / (float)entry.Capacity;
        cellView.Group.alpha = 1f;
        _audio?.PlayCollect();
        int strandsPerCell = cellView.Strands.Count;
        for (int strandIndex = 0; strandIndex < strandsPerCell; strandIndex++)
        {
            if (generation != _generation)
            {
                yield break;
            }

            YarnMatchStrandView strand = cellView.Strands[strandIndex];
            strand.Rect.localScale = Vector3.one;
            float cellSize = cellRect.sizeDelta.y;
            float strandSlot = cellSize / strandsPerCell;
            strand.Rect.anchoredPosition = new Vector2(
                0f,
                -cellSize * 0.5f + strandSlot * 0.5f + strandIndex * strandSlot);
            strand.Group.alpha = 1f;
            strand.Rect.gameObject.SetActive(true);
            float segmentStart = strandIndex / (float)strandsPerCell;
            float segmentEnd = (strandIndex + 1) / (float)strandsPerCell;
            yield return PlayStrandIntoRack(
                strand,
                target,
                cell.Column * 1.71f + cell.Row * 0.83f + strandIndex * 0.37f,
                YarnMatchUiTheme.Palette[(int)cell.Color],
                generation,
                cellRect,
                cellBasePosition,
                cellBaseScale,
                spoolRect,
                rackView,
                startProgress,
                targetProgress,
                segmentStart,
                segmentEnd);
        }

        if (generation == _generation)
        {
            cellRect.localScale = cellBaseScale;
            cellRect.anchoredPosition = cellBasePosition;
            spoolRect.localScale = GetSpoolProgressScale(rackView.BaseScale, targetProgress);
            rackView.FillImage.fillAmount = targetProgress;
            cellView.Group.alpha = 0f;
        }
        yield return null;
    }
    private static Vector3 GetSpoolProgressScale(Vector3 baseScale, float progress)
    {
        float width = Mathf.Lerp(1f, 1.18f, progress);
        float height = Mathf.Lerp(1f, 1.04f, progress);
        return new Vector3(baseScale.x * width, baseScale.y * height, baseScale.z);
    }

    internal IEnumerator PlayBoardDrop(YarnMatchBoardModel board)
    {
        int generation = _generation;
        List<YarnMatchBoardCell> cells = new List<YarnMatchBoardCell>();
        List<Vector2> starts = new List<Vector2>();
        for (int index = 0; index < board.AllCells.Count; index++)
        {
            YarnMatchBoardCell cell = board.AllCells[index];
            if (cell.Active && _renderer.TryGetBoardCellView(cell, out YarnMatchBoardCellView view))
            {
                cells.Add(cell);
                starts.Add(view.Rect.anchoredPosition);
            }
        }

        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / duration));
            for (int index = 0; index < cells.Count; index++)
            {
                if (_renderer.TryGetBoardCellView(cells[index], out YarnMatchBoardCellView view) && view.Rect != null)
                {
                    view.Rect.anchoredPosition = Vector2.Lerp(starts[index], _renderer.BoardPosition(cells[index].Column, cells[index].Row), progress);
                }
            }
            yield return null;
        }

        if (generation == _generation)
        {
            _renderer.RenderBoard(board, true);
        }
    }

    internal IEnumerator PlayRackImpact(YarnMatchRackEntry entry)
    {
        if (entry == null || entry.Progress >= entry.Capacity)
        {
            yield break;
        }

        int generation = _generation;
        if (!_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView view))
        {
            yield break;
        }

        RectTransform spool = view.SpoolRect;
        Vector3 baseScale = spool.localScale;
        Quaternion baseRotation = spool.localRotation;
        Color baseColor = view.SpoolImage.color;
        float targetFill = entry.Progress / (float)entry.Capacity;
        float fromFill = Mathf.Max(0f, targetFill - 1f / entry.Capacity);
        view.FillImage.fillAmount = fromFill;
        _audio?.PlayRackHit();
        float elapsed = 0f;
        const float duration = 0.46f;
        while (elapsed < duration && spool != null && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = YarnMatchUiTheme.EaseInOut(progress);
            spool.localRotation = baseRotation * Quaternion.Euler(0f, 0f, 360f * eased + Mathf.Sin(progress * Mathf.PI * 3f) * 10f);
            float squeeze = Mathf.Sin(progress * Mathf.PI);
            spool.localScale = new Vector3(baseScale.x * (1f + squeeze * 0.10f), baseScale.y * (1f - squeeze * 0.08f), baseScale.z);
            view.FillImage.fillAmount = Mathf.Lerp(fromFill, targetFill, eased);
            view.SpoolImage.color = Color.Lerp(Color.white, YarnMatchUiTheme.Palette[(int)entry.Color], 0.12f + squeeze * 0.20f);
            yield return null;
        }

        if (generation == _generation && spool != null)
        {
            spool.localRotation = baseRotation;
            spool.localScale = baseScale;
            view.FillImage.fillAmount = targetFill;
            view.SpoolImage.color = baseColor;
            view.CountLabel.text = entry.Progress + "/" + entry.Capacity;
        }
    }

    internal IEnumerator PlayRackPulse(YarnMatchRackEntry entry)
    {
        int generation = _generation;
        if (!_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView view))
        {
            yield break;
        }

        _renderer.HideRackProgress(entry);
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
        while (elapsed < duration && rect != null && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / duration));
            rect.anchoredPosition = Vector2.Lerp(start, target, progress);
            rect.localScale = Vector3.one * Mathf.Lerp(minimumScale, maximumScale, progress);
            yield return null;
        }

        feedback?.SetAnimationDriven(false);
        if (generation == _generation && rect != null)
        {
            rect.anchoredPosition = target;
            rect.localScale = Vector3.one;
            _audio?.PlayTunnel();
        }
    }
    internal IEnumerator PulseBoardCell(YarnMatchBoardCell cell)
    {
        int generation = _generation;
        if (!_renderer.TryGetBoardCellView(cell, out YarnMatchBoardCellView view))
        {
            yield break;
        }

        Image image = view.Image;
        Color baseColor = image.color;
        float elapsed = 0f;
        const float duration = 0.75f;
        while (elapsed < duration && image != null && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float highlight = 0.10f + (Mathf.Sin(progress * Mathf.PI * 4f) * 0.5f + 0.5f) * 0.22f;
            image.color = Color.Lerp(baseColor, Color.white, highlight);
            yield return null;
        }

        if (generation == _generation && image != null)
        {
            image.color = baseColor;
        }
    }

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

    private IEnumerator PlayStrandIntoRack(
        YarnMatchStrandView strand,
        Vector2 target,
        float phase,
        Color yarnColor,
        int generation,
        RectTransform cellRect,
        Vector2 cellBasePosition,
        Vector3 cellBaseScale,
        RectTransform spoolRect,
        YarnMatchRackEntryView rackView,
        float startProgress,
        float targetProgress,
        float segmentStart,
        float segmentEnd)
    {
        Vector2 start = _renderer.WorldToCanvas(strand.Rect.position);
        YarnMatchTrailVisual trail = _effects.GetTrailVisual(yarnColor);
        float elapsed = 0f;
        const float duration = 0.12f;
        Vector3 baseScale = strand.Rect.localScale;
        Vector2 basePosition = strand.Rect.anchoredPosition;
        Vector3 startSpoolScale = GetSpoolProgressScale(rackView.BaseScale, startProgress);
        Vector3 targetSpoolScale = GetSpoolProgressScale(rackView.BaseScale, targetProgress);
        while (elapsed < duration && generation == _generation && strand.Rect != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = YarnMatchUiTheme.EaseInOut(progress);
            float overallProgress = Mathf.Lerp(segmentStart, segmentEnd, eased);
            float heightScale = Mathf.Lerp(1f, 0.08f, overallProgress);
            cellRect.localScale = new Vector3(
                cellBaseScale.x,
                cellBaseScale.y * heightScale,
                cellBaseScale.z);
            float cellHeight = cellRect.sizeDelta.y * cellBaseScale.y;
            cellRect.anchoredPosition = cellBasePosition
                + Vector2.up * (cellHeight * (1f - heightScale) * 0.5f);
            strand.Rect.anchoredPosition = Vector2.Lerp(basePosition, Vector2.zero, eased * 0.82f);
            strand.Rect.localScale = new Vector3(
                Mathf.Lerp(baseScale.x, baseScale.x * 0.04f, eased),
                Mathf.Lerp(baseScale.y, baseScale.y * 0.72f, eased),
                baseScale.z);
            strand.Group.alpha = 1f - eased * 0.18f;
            spoolRect.localScale = Vector3.Lerp(startSpoolScale, targetSpoolScale, overallProgress);
            rackView.FillImage.fillAmount = Mathf.Lerp(startProgress, targetProgress, overallProgress);
            UpdateTrail(trail, start, target, eased, phase);
            yield return null;
        }

        if (generation == _generation && strand.Rect != null)
        {
            strand.Rect.anchoredPosition = basePosition;
            strand.Rect.localScale = baseScale;
            strand.Group.alpha = 0f;
            strand.Rect.gameObject.SetActive(false);
        }
        if (generation == _generation)
        {
            _effects.ReleaseTrailVisual(trail);
        }
    }
    private void UpdateTrail(YarnMatchTrailVisual trail, Vector2 start, Vector2 target, float progress, float phase)
    {
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
            Vector2 a = SoftThreadPoint(start, target, from, phase);
            Vector2 b = SoftThreadPoint(start, target, Mathf.Lerp(from, to, visible), phase);
            Vector2 delta = b - a;
            segment.anchoredPosition = Vector2.Lerp(a, b, 0.5f);
            float thickness = Mathf.Lerp(5.5f, 2.8f, from);
            segment.sizeDelta = new Vector2(Mathf.Max(5f, delta.magnitude), thickness);
            segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }

    private static Vector2 SoftThreadPoint(Vector2 start, Vector2 target, float progress, float phase)
    {
        Vector2 delta = target - start;
        Vector2 normal = delta.sqrMagnitude < 0.01f ? Vector2.up : new Vector2(-delta.y, delta.x).normalized;
        float distance = delta.magnitude;
        float bend = Mathf.Clamp(distance * 0.14f, 34f, 92f);
        Vector2 controlOne = start + delta * 0.20f + normal * (bend + Mathf.Sin(phase) * 10f);
        Vector2 controlTwo = start + delta * 0.68f - normal * (bend * 0.72f + Mathf.Cos(phase * 0.7f) * 12f);
        float sway = Mathf.Sin(progress * Mathf.PI * 2.2f + phase) * 12f * Mathf.Sin(progress * Mathf.PI);
        return CubicBezier(start, controlOne, controlTwo, target, progress) + normal * sway;
    }

    private static Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * inverse * a
            + 3f * inverse * inverse * t * b
            + 3f * inverse * t * t * c
            + t * t * t * d;
    }
}
