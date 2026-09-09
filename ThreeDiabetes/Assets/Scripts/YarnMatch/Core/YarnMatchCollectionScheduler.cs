using System;
using System.Collections.Generic;

internal sealed class YarnMatchCollectionScheduler
{
    private readonly YarnMatchBoardModel _board;
    private readonly HashSet<int> _busyColumns = new HashSet<int>();
    private readonly int[] _nextColumns = new int[Enum.GetValues(typeof(YarnMatchColor)).Length];
    private int _nextJob;
    internal bool IsBusy => _busyColumns.Count > 0;

    internal YarnMatchCollectionScheduler(YarnMatchBoardModel board, int seed)
    {
        _board = board;
        Random random = new Random(seed);
        for (int i = 0; i < _nextColumns.Length; i++) _nextColumns[i] = random.Next(board.Columns.Count);
    }

    internal bool TryReserve(IReadOnlyList<YarnMatchCollectionJob> jobs,
        out YarnMatchCollectionJob job, out YarnMatchBoardCell cell)
    {
        for (int offset = 0; offset < jobs.Count; offset++)
        {
            int index = (_nextJob + offset) % jobs.Count;
            var candidate = jobs[index];
            if (!candidate.Ready || candidate.Completing
                || candidate.Entry.Progress + candidate.PendingCells >= candidate.Entry.Capacity) continue;
            cell = FindAvailable(candidate.Entry.Color);
            if (cell == null) continue;
            // Reserve before starting any coroutine; a falling column remains reserved.
            _busyColumns.Add(cell.Column);
            _nextColumns[(int)cell.Color] = (cell.Column + 1) % _board.Columns.Count;
            _nextJob = (index + 1) % jobs.Count;
            job = candidate;
            return true;
        }
        job = null;
        cell = null;
        return false;
    }

    internal YarnMatchBoardCell FindAvailable(YarnMatchColor color)
    {
        int start = _nextColumns[(int)color];
        for (int offset = 0; offset < _board.Columns.Count; offset++)
        {
            int column = (start + offset) % _board.Columns.Count;
            var stack = _board.Columns[column];
            if (!_busyColumns.Contains(column) && stack.Count > 0 && stack[0].Color == color) return stack[0];
        }
        return null;
    }

    internal int CountAvailable(YarnMatchColor color)
    {
        int count = 0;
        for (int column = 0; column < _board.Columns.Count; column++)
        {
            var stack = _board.Columns[column];
            if (!_busyColumns.Contains(column) && stack.Count > 0 && stack[0].Color == color) count++;
        }
        return count;
    }

    internal void ReleaseColumn(int column) => _busyColumns.Remove(column);
}
