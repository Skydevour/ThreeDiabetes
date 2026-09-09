using System.Collections.Generic;
using UnityEngine;

internal sealed class YarnMatchThumbnailQueue : MonoBehaviour
{
    private readonly List<YarnMatchLevelThumbnail> _pending = new List<YarnMatchLevelThumbnail>();

    internal void Enqueue(YarnMatchLevelThumbnail thumbnail)
    {
        if (!_pending.Contains(thumbnail)) _pending.Add(thumbnail);
    }

    private void Update()
    {
        float started = Time.realtimeSinceStartup;
        while (_pending.Count > 0)
        {
            var thumbnail = _pending[0];
            if (thumbnail != null && !thumbnail.TryPublish()) break;
            _pending.RemoveAt(0);
            if (Time.realtimeSinceStartup - started >= 0.003f) break;
        }
    }
}
