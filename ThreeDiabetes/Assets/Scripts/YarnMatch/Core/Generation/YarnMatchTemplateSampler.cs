using System;
using System.Collections.Generic;

internal static class YarnMatchTemplateSampler
{
    internal static IReadOnlyList<YarnMatchPatternTemplate> Templates => YarnMatchPatternResources.Templates;

    internal static void GetDimensions(int templateIndex, int detail, out int width, out int height)
    {
        YarnMatchPatternTemplate template = Templates[templateIndex];
        width = detail;
        height = Math.Max(1, Math.Min(100, (int)Math.Round(detail * template.Height / (double)template.Width,
            MidpointRounding.AwayFromZero)));
    }

    internal static YarnMatchPatternLayout Generate(YarnMatchLevelConfig config)
    {
        YarnMatchPatternTemplate template = Templates[config.TemplateIndex];
        int width = config.BoardColumns, height = config.BoardRows;
        var symbols = new List<int>(width * height);
        int[] sampled = new int[width * height];
        double[] weights = new double[template.palette.Length];
        double scale = Math.Min(width / (double)template.Width, height / (double)template.Height);
        double offsetX = (width - template.Width * scale) * 0.5;
        double offsetY = (height - template.Height * scale) * 0.5;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int symbol = Sample(template,
                template.Left + (x - offsetX) / scale,
                template.Top + (height - y - 1d - offsetY) / scale,
                1d / scale, weights);
            sampled[y * width + x] = symbol;
            if (symbol >= 0) symbols.Add(symbol);
        }

        int[] map = YarnMatchTemplatePalette.Build(template.Rgb, symbols, config.ColorCount, out _);
        for (int i = 0; i < sampled.Length; i++)
            if (sampled[i] >= 0) sampled[i] = map[sampled[i]];
        return new YarnMatchPatternLayout(width, height, sampled);
    }

    private static int Sample(YarnMatchPatternTemplate template, double left, double top,
        double span, double[] weights)
    {
        Array.Clear(weights, 0, weights.Length);
        double covered = 0;
        for (int y = Math.Max(0, (int)Math.Floor(top)); y < Math.Min(template.rows.Length, Math.Ceiling(top + span)); y++)
        for (int x = Math.Max(0, (int)Math.Floor(left)); x < Math.Min(template.rows[0].Length, Math.Ceiling(left + span)); x++)
        {
            if (template.mask[y][x] != '1') continue;
            double area = (Math.Min(x + 1d, left + span) - Math.Max(x, left))
                * (Math.Min(y + 1d, top + span) - Math.Max(y, top));
            char symbol = template.rows[y][x];
            weights[symbol <= '9' ? symbol - '0' : symbol - 'A' + 10] += area;
            covered += area;
        }
        // Retain narrow details without filling empty silhouette corners.
        if (covered < span * span * 0.25) return -1;
        int best = 0;
        for (int i = 1; i < weights.Length; i++) if (weights[i] > weights[best]) best = i;
        return best;
    }
}
