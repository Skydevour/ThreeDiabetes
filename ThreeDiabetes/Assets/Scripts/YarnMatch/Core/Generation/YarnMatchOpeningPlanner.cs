using System;

internal static class YarnMatchOpeningPlanner
{
    internal static void Arrange(YarnMatchBoardModel board, YarnMatchPoolModel pool, int seed)
    {
        YarnMatchPoolCell first = null, second = null;
        foreach (var cell in pool.Cells)
        {
            if (!IsPlain(cell) || !cell.Unlocked) continue;
            foreach (var neighbor in pool.Cells)
            {
                if (neighbor == cell || !IsPlain(neighbor)) continue;
                if (!neighbor.Unlocked && Math.Abs(cell.Column - neighbor.Column)
                    + Math.Abs(cell.Row - neighbor.Row) != 1) continue;
                first = cell;
                second = neighbor;
                break;
            }
            if (first != null) break;
        }
        if (first == null) return;

        int count = board.ColorCounts.Count;
        int start = new Random(seed).Next(count);
        for (int offset = 0; offset < count; offset++)
        {
            var held = (YarnMatchColor)((start + offset) % count);
            int available = CountOpeningRun(board, held);
            if (available < 1 || available >= 3) continue;
            var heldToken = FindFullToken(pool, held);
            if (heldToken == null) continue;
            for (int other = 0; other < count; other++)
            {
                var support = (YarnMatchColor)other;
                if (support == held || CountOpeningRun(board, support) == 0) continue;
                var supportToken = FindFullToken(pool, support);
                if (supportToken == null || !CanReleaseHeld(board, held, support)) continue;
                pool.PlaceToken(heldToken, first);
                pool.PlaceToken(supportToken, second);
                return;
            }
        }
    }

    private static bool CanReleaseHeld(YarnMatchBoardModel board, YarnMatchColor held, YarnMatchColor support)
    {
        int heldCount = 0, supportCount = 0;
        foreach (var column in board.Columns)
        foreach (var cell in column)
        {
            if (cell.Color == held) heldCount++;
            else if (cell.Color == support) supportCount++;
            else break;
        }
        // Every accessible support cell fits one spool, regardless of column scheduling order.
        return heldCount >= 3 && supportCount > 0 && supportCount <= 3;
    }

    private static int CountOpeningRun(YarnMatchBoardModel board, YarnMatchColor color)
    {
        int count = 0;
        foreach (var column in board.Columns)
        foreach (var cell in column)
        {
            if (cell.Color != color) break;
            count++;
        }
        return count;
    }

    private static bool IsPlain(YarnMatchPoolCell cell) => cell.Token != null
        && cell.Tunnel == null && cell.ChainId < 0 && cell.FreezeHitsRemaining == 0;

    private static YarnMatchSpoolToken FindFullToken(YarnMatchPoolModel pool, YarnMatchColor color)
    {
        foreach (var token in pool.Tokens)
            if (!token.Used && token.Color == color && token.Capacity == 3) return token;
        return null;
    }
}
