using System;
using System.Collections.Generic;

internal sealed class YarnMatchTemplateDeck
{
    private readonly Queue<int> _recent = new Queue<int>();
    private readonly HashSet<int> _drawn = new HashSet<int>();
    private string _lastCategory;

    internal int Draw(YarnMatchLevelDifficulty difficulty, Random random, out int width, out int height)
    {
        var templates = YarnMatchTemplateSampler.Templates;
        width = height = 0;
        int chosen = -1;
        for (int cycle = 0; cycle < 2; cycle++)
        {
            double bestScore = double.PositiveInfinity;
            for (int i = 0; i < templates.Count; i++)
            {
                if (_drawn.Contains(i) || !difficulty.TryGetDimensions(i, out int w, out int h)) continue;
                double score = difficulty.Score(templates[i], w) + random.NextDouble() * 0.6;
                if (_recent.Contains(i)) score += 4d;
                if (templates[i].category == _lastCategory) score += 0.25;
                if (score >= bestScore) continue;
                bestScore = score;
                chosen = i;
                width = w;
                height = h;
            }
            if (chosen >= 0) break;
            _drawn.Clear();
        }
        if (chosen < 0) throw new InvalidOperationException("No readable local pattern fits this level.");
        _drawn.Add(chosen);
        _recent.Enqueue(chosen);
        if (_recent.Count > 8) _recent.Dequeue();
        _lastCategory = templates[chosen].category;
        return chosen;
    }
}
