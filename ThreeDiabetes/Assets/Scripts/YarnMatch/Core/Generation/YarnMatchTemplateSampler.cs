using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
internal sealed class YarnMatchPatternTemplate
{
    public string id;
    public string title;
    public int minSize;
    public string[] palette;
    public string[] rows;
}

[Serializable]
internal sealed class YarnMatchPatternPack
{
    public YarnMatchPatternTemplate[] patterns;
}

internal static class YarnMatchTemplateSampler
{
    private static readonly int[] Colors =
        { 0xF5384D, 0xFC7D14, 0xFFBF0D, 0x1AB861, 0x08B3E6, 0x1F5CE0, 0x8038D1, 0xF23394, 0xFFEEAD };
    private static YarnMatchPatternTemplate[] _templates;

    internal static IReadOnlyList<YarnMatchPatternTemplate> Templates
    {
        get
        {
            if (_templates == null)
            {
                TextAsset asset = Resources.Load<TextAsset>("YarnMatch/Patterns/PatternTemplates");
                _templates = JsonUtility.FromJson<YarnMatchPatternPack>(asset.text).patterns;
            }
            return _templates;
        }
    }

    internal static IReadOnlyList<YarnMatchColor> Generate(
        IReadOnlyList<int> heights, int colorCount, int seed)
    {
        System.Random random = new System.Random(seed);
        var candidates = new List<YarnMatchPatternTemplate>();
        foreach (YarnMatchPatternTemplate template in Templates)
            if (template.minSize <= heights.Count) candidates.Add(template);
        YarnMatchPatternTemplate selected = candidates[random.Next(candidates.Count)];
        int[] sourcePalette = new int[selected.palette.Length];
        for (int i = 0; i < sourcePalette.Length; i++)
            sourcePalette[i] = Convert.ToInt32(selected.palette[i], 16);
        int height = 0;
        foreach (int h in heights) height = Math.Max(height, h);
        int sourceWidth = selected.rows[0].Length;
        int sourceHeight = selected.rows.Length;
        float scale = Math.Min(heights.Count / (float)sourceWidth, height / (float)sourceHeight);
        float offsetX = (heights.Count - sourceWidth * scale) * 0.5f;
        float offsetY = (height - sourceHeight * scale) * 0.5f;
        bool mirror = selected.minSize < 32 && random.Next(2) == 0;
        var pixels = new List<int>();
        int[] frequencies = new int[Colors.Length];
        for (int x = 0; x < heights.Count; x++)
        for (int y = 0; y < heights[x]; y++)
        {
            int sx = (int)Math.Floor((x + 0.5f - offsetX) / scale);
            int sy = (int)Math.Floor((height - y - 0.5f - offsetY) / scale);
            int symbol = Decode(selected.rows[0][0]);
            if (sx >= 0 && sx < sourceWidth && sy >= 0 && sy < sourceHeight)
                symbol = Decode(selected.rows[sy][mirror ? sourceWidth - 1 - sx : sx]);
            int color = Nearest(sourcePalette[symbol], null);
            pixels.Add(color);
            frequencies[color]++;
        }

        var order = new List<int>();
        for (int i = 0; i < Colors.Length; i++) order.Add(i);
        order.Sort((a, b) => frequencies[b] != frequencies[a]
            ? frequencies[b].CompareTo(frequencies[a]) : a.CompareTo(b));
        var palette = order.GetRange(0, colorCount);
        var pattern = new List<YarnMatchColor>(pixels.Count);
        int[] used = new int[Colors.Length];
        foreach (int color in pixels)
        {
            int mapped = palette.Contains(color) ? color : Nearest(Colors[color], palette);
            pattern.Add((YarnMatchColor)mapped);
            used[mapped]++;
        }
        // Small border accents supply missing gameplay colors without scattering the subject.
        int cursor = 0;
        foreach (int color in palette)
        {
            if (used[color] > 0) continue;
            while (used[(int)pattern[cursor]] <= 1) cursor++;
            used[(int)pattern[cursor]]--;
            pattern[cursor++] = (YarnMatchColor)color;
            used[color]++;
        }
        return pattern;
    }

    private static int Decode(char value) => value <= '9' ? value - '0' : value - 'A' + 10;

    private static int Nearest(int rgb, List<int> palette)
    {
        int best = 0, distance = int.MaxValue;
        int count = palette == null ? Colors.Length : palette.Count;
        for (int i = 0; i < count; i++)
        {
            int color = palette == null ? i : palette[i];
            int target = Colors[color];
            int r = (rgb >> 16 & 255) - (target >> 16 & 255);
            int g = (rgb >> 8 & 255) - (target >> 8 & 255);
            int b = (rgb & 255) - (target & 255);
            int d = r * r + g * g + b * b;
            if (d < distance) { distance = d; best = color; }
        }
        return best;
    }
}
