using System.Threading.Tasks;

internal sealed class YarnMatchRoundData
{
    internal readonly YarnMatchBoardModel Board;
    internal readonly YarnMatchPoolModel Pool;

    internal YarnMatchRoundData(YarnMatchLevelConfig config, YarnMatchPatternLayout pattern)
    {
        Board = new YarnMatchBoardModel();
        Board.Build(pattern, !config.UsesReferencePattern && config.BoardRows <= config.BoardColumns * 1.25f);
        Pool = new YarnMatchPoolModel();
        Pool.Build(config, Board.ColorCounts);
        if (!config.UsesReferencePattern && config.Number >= 10)
            YarnMatchOpeningPlanner.Arrange(Board, Pool, config.Seed);
    }

    private YarnMatchRoundData(YarnMatchLevelSnapshot snapshot)
    {
        Board = new YarnMatchBoardModel();
        Board.Build(snapshot.CreatePattern(), snapshot.FitToViewport);
        Pool = new YarnMatchPoolModel();
        Pool.Restore(snapshot.Pool);
    }

    internal static Task<YarnMatchRoundData> PrepareAsync(YarnMatchLevelSnapshot snapshot)
        => Task.Run(() => new YarnMatchRoundData(snapshot));
}
