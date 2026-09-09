using System.Threading.Tasks;

internal sealed class YarnMatchRoundData
{
    internal readonly YarnMatchBoardModel Board;
    internal readonly YarnMatchPoolModel Pool;

    private YarnMatchRoundData(YarnMatchLevelConfig config, YarnMatchPatternLayout pattern)
    {
        Board = new YarnMatchBoardModel();
        Board.Build(pattern, !config.UsesReferencePattern && config.BoardRows <= config.BoardColumns * 1.25f);
        Pool = new YarnMatchPoolModel();
        Pool.Build(config, Board.ColorCounts);
        if (!config.UsesReferencePattern && config.Number >= 10)
            YarnMatchOpeningPlanner.Arrange(Board, Pool, config.Seed);
    }

    internal static async Task<YarnMatchRoundData> PrepareAsync(YarnMatchLevelConfig config)
    {
        YarnMatchPatternLayout pattern = await config.PreparePatternAsync();
        return await Task.Run(() => new YarnMatchRoundData(config, pattern));
    }
}
