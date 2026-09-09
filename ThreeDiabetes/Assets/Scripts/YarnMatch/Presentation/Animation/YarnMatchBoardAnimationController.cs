using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal sealed class YarnMatchBoardAnimationController
{
    private readonly YarnMatchUiRenderer _renderer;
    private readonly YarnMatchEffectPool _effects;
    private readonly YarnMatchAudio _audio;
    private int _generation;

    internal YarnMatchBoardAnimationController(YarnMatchUiRenderer renderer, YarnMatchEffectPool effects, YarnMatchAudio audio)
    {
        _renderer = renderer;
        _effects = effects;
        _audio = audio;
    }

    internal void Reset() => _generation++;

    internal IEnumerator Collect(YarnMatchBoardCell cell, YarnMatchRackEntry entry)
    {
        int generation = _generation;
        if (!_renderer.TryGetRackEntryView(entry, out YarnMatchRackEntryView rack)) yield break;
        YarnMatchBoardSurface board = _renderer.BoardSurface;
        Image tile = board.AcquireTile(cell);
        RectTransform rect = tile.rectTransform;
        Vector2 origin = rect.anchoredPosition;
        float size = board.CellSize;
        Color tint = YarnMatchUiTheme.Palette[(int)cell.Color];
        YarnMatchTrailVisual trail = _effects.GetTrailVisual(tint);
        rack.ActiveCollections++;
        _audio?.PlayCollect();
        float elapsed = 0f, previous = 0f;
        const float duration = 1.2f;
        const int rows = 10;
        while (elapsed < duration && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // The top edge and width stay fixed; ten consecutive rows unwind continuously.
            float remaining = 1f - progress;
            rect.sizeDelta = new Vector2(size, size * remaining);
            rect.anchoredPosition = origin + Vector2.up * (size * progress * 0.5f);
            Vector3 sourceWorld = rect.TransformPoint(new Vector3(
                Mathf.Lerp(-size * 0.5f, size * 0.5f, Mathf.Repeat(progress * rows, 1f)),
                -size * remaining * 0.5f));
            UpdateThread(trail, _renderer.WorldToCanvas(sourceWorld),
                _renderer.WorldToCanvas(rack.SpoolRect.position), progress, cell.Column * 0.73f, size);
            rack.InFlightProgress += progress - previous;
            previous = progress;
            UpdateSpool(rack, (entry.Progress + rack.InFlightProgress) / entry.Capacity);
            yield return null;
        }
        if (generation != _generation) yield break;
        rack.InFlightProgress = Mathf.Max(0f, rack.InFlightProgress - previous);
        rack.ActiveCollections--;
        // Core commits the completed cell immediately when this iterator returns.
        UpdateSpool(rack, (entry.Progress + 1f + rack.InFlightProgress) / entry.Capacity);
        if (rack.ActiveCollections == 0) rack.SpoolRect.localRotation = Quaternion.identity;
        board.ReleaseTile(cell);
        _effects.ReleaseTrailVisual(trail);
        _audio?.PlayRackHit();
    }

    private static void UpdateSpool(YarnMatchRackEntryView rack, float progress)
    {
        progress = Mathf.Clamp01(progress);
        rack.FillImage.fillAmount = progress;
        rack.SpoolRect.localScale = new Vector3(
            rack.BaseScale.x * Mathf.Lerp(1f, 1.18f, progress),
            rack.BaseScale.y * Mathf.Lerp(1f, 1.04f, progress), rack.BaseScale.z);
        rack.SpoolRect.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 230f);
    }

    internal IEnumerator Drop(int column)
    {
        int generation = _generation;
        YarnMatchBoardSurface board = _renderer.BoardSurface;
        board.BeginDrop(column);
        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration && generation == _generation)
        {
            elapsed += Time.unscaledDeltaTime;
            board.SetDrop(column, YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        if (generation == _generation) board.SetDrop(column, 1f);
    }

    internal IEnumerator Pulse(YarnMatchBoardCell cell)
    {
        int generation = _generation;
        float elapsed = 0f;
        while (elapsed < 0.75f && generation == _generation && cell.Active)
        {
            elapsed += Time.unscaledDeltaTime;
            _renderer.BoardSurface.Highlight(cell, 0.15f + Mathf.Sin(elapsed * 17f) * 0.12f);
            yield return null;
        }
        if (generation == _generation) _renderer.BoardSurface.Highlight(cell, 0f);
    }

    private static void UpdateThread(YarnMatchTrailVisual trail, Vector2 start, Vector2 target,
        float progress, float phase, float cellSize)
    {
        Vector2 delta = target - start;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
        float bend = Mathf.Clamp(delta.magnitude * 0.12f, 28f, 80f);
        Vector2 a = start + delta * 0.20f + normal * bend;
        Vector2 b = start + delta * 0.68f - normal * bend * 0.72f;
        float reach = Mathf.Clamp01(progress * 8f);
        for (int i = 0; i < trail.Segments.Count; i++)
        {
            float from = i / (float)trail.Segments.Count;
            float to = Mathf.Min(reach, (i + 1f) / trail.Segments.Count);
            RectTransform segment = trail.Segments[i];
            segment.gameObject.SetActive(to > from);
            if (to <= from) continue;
            Vector2 p = ThreadPoint(start, a, b, target, normal, from, phase);
            Vector2 q = ThreadPoint(start, a, b, target, normal, to, phase);
            Vector2 line = q - p;
            segment.anchoredPosition = (p + q) * 0.5f;
            segment.sizeDelta = new Vector2(line.magnitude + 0.5f, Mathf.Clamp(cellSize * 0.09f, 1.6f, 4f));
            segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(line.y, line.x) * Mathf.Rad2Deg);
        }
    }

    private static Vector2 ThreadPoint(Vector2 start, Vector2 a, Vector2 b, Vector2 end,
        Vector2 normal, float t, float phase)
    {
        float u = 1f - t;
        Vector2 position = u * u * u * start + 3f * u * u * t * a + 3f * u * t * t * b + t * t * t * end;
        return position + normal * (Mathf.Sin(t * Mathf.PI * 2f + phase + Time.unscaledTime * 3f)
            * 6f * Mathf.Sin(t * Mathf.PI));
    }
}
