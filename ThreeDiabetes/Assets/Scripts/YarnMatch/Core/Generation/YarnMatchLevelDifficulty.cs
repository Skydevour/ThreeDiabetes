using System;

internal readonly struct YarnMatchLevelDifficulty
{
    internal readonly int Level;
    internal readonly int Detail;
    internal readonly int Colors;
    private readonly double _targetCells;
    private readonly double _targetPressure;

    internal YarnMatchLevelDifficulty(int level)
    {
        Level = Math.Max(1, level);
        Detail = Math.Min(50, 12 + Math.Min(38, Level - 1));
        Colors = YarnMatchLevelCatalog.GetColorCount(Level);
        _targetCells = Level < 10 ? 90 + (Level - 1) * 20 : Detail * Detail * 0.68;
        _targetPressure = 0.1 + Math.Min(1d, (Level - 1) / 38d) * 0.45;
    }

    internal bool TryGetDimensions(int templateIndex, out int width, out int height)
    {
        var template = YarnMatchTemplateSampler.Templates[templateIndex];
        width = Detail;
        height = 0;
        int budget = 96 * YarnMatchRackModel.CellsPerSpool - Colors * (YarnMatchRackModel.CellsPerSpool - 1);
        // Find the finest legal width first. Never reduce an already selected image below its authored minimum.
        for (; width >= template.minSize; width--)
        {
            YarnMatchTemplateSampler.GetDimensions(templateIndex, width, out _, out height);
            if (Level >= 10 || width * height <= budget) return true;
        }
        return false;
    }

    internal double Score(YarnMatchPatternTemplate template, int width)
    {
        var profile = template.profiles[width - template.minSize];
        double sizeDifference = Math.Abs(profile.cells - _targetCells) / _targetCells;
        int actualColors = Math.Min(Colors, profile.colors);
        double pressure = profile.pressure[Colors - 4] / 1000d;
        return sizeDifference * 3d + (Colors - actualColors) * 0.65
            + Math.Abs(pressure - _targetPressure) * 1.6;
    }
}
