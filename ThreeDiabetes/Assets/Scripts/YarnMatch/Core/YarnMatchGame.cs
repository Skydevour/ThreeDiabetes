using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class YarnMatchGame : MonoBehaviour
{
    private const string HighestUnlockedLevelKey = "YarnMatch.HighestUnlockedLevel";
    private const string DailyUnlockDateKey = "YarnMatch.DailyUnlockDate";
    private const string DailyUnlockEnabledKey = "YarnMatch.DailyUnlockEnabled";
    private readonly List<YarnMatchCollectionJob> _collectionJobs = new List<YarnMatchCollectionJob>();
    private readonly HashSet<YarnMatchBoardCell> _claimedBoardCells = new HashSet<YarnMatchBoardCell>();
    private readonly HashSet<YarnMatchPoolCell> _pendingReplenishments = new HashSet<YarnMatchPoolCell>();
    private bool _boardSettleInProgress;
    private bool _gameStarted;
    private bool _inputLocked;
    private int _roundId;
    private int _selectedLevel = 1;
    private int _highestUnlockedLevel = 1;
    private YarnMatchLevelConfig _levelConfig;
    private YarnMatchBoardModel _board;
    private YarnMatchPoolModel _pool;
    private YarnMatchRackModel _rack;
    private YarnMatchPresentation _presentation;
    private YarnMatchGameState _state;
    private bool _dailyUnlockActive;
    private bool _specialChallenge;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        _highestUnlockedLevel = Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1));
        RefreshDailyUnlockState();
        _presentation = GetComponent<YarnMatchPresentation>();
        if (_presentation == null)
        {
            _presentation = gameObject.AddComponent<YarnMatchPresentation>();
        }
        _presentation.Initialize(SelectSpool, RequestRestart, AdvanceToNextLevel, ShowHint, RequestRefresh, OpenLevelSelect, StartSelectedLevel, UnlockAllLevelsForToday, StartSpecialChallenge, ReturnToMainMenu);
        _presentation.ShowMainMenu();
    }

    private void OpenLevelSelect()
    {
        RefreshDailyUnlockState();
        _presentation.ShowLevelSelect(GetAvailableHighestLevel(), _dailyUnlockActive);
    }

    private void StartSelectedLevel(int level)
    {
        RefreshDailyUnlockState();
        if (level < 1 || level > GetAvailableHighestLevel())
        {
            return;
        }

        _selectedLevel = level;
        _specialChallenge = false;
        _gameStarted = true;
        StartNewGame();
    }

    private void StartSpecialChallenge()
    {
        _specialChallenge = true;
        _selectedLevel = YarnMatchLevelCatalog.ReferencePatternLevel;
        _gameStarted = true;
        StartNewGame();
    }

    private void AdvanceToNextLevel()
    {
        if (_specialChallenge)
        {
            StartSpecialChallenge();
            return;
        }
        int nextLevel = _selectedLevel + 1;
        _highestUnlockedLevel = Mathf.Max(_highestUnlockedLevel, nextLevel);
        SaveProgress();
        StartSelectedLevel(nextLevel);
    }

    private void ReturnToMainMenu()
    {
        _gameStarted = false;
        _inputLocked = true;
        _state = YarnMatchGameState.Resolving;
        _roundId++;
        _presentation.CancelAnimations();
        _presentation.ShowMainMenu();
    }

    private void StartNewGame()
    {
        _levelConfig = _specialChallenge
            ? YarnMatchLevelCatalog.CreateSpecialRound()
            : YarnMatchLevelCatalog.CreateRound(_selectedLevel);
        _roundId++;
        _state = YarnMatchGameState.Playing;
        _inputLocked = false;
        _collectionJobs.Clear();
        _claimedBoardCells.Clear();
        _pendingReplenishments.Clear();
        _boardSettleInProgress = false;
        _board = new YarnMatchBoardModel();
        _board.Build(_levelConfig);
        _pool = new YarnMatchPoolModel();
        _pool.Build(_levelConfig, _board.ColorCounts);
        _rack = new YarnMatchRackModel();
        _presentation.SetLevel(_levelConfig.Number);
        _presentation.ResetGame(_board, _pool, _rack);
        _presentation.SetInteraction(_pool, _state, true);
    }

    private void SelectSpool(YarnMatchSpoolToken token)
    {
        if (!_gameStarted
            || _inputLocked
            || _state != YarnMatchGameState.Playing
            || _board == null
            || _rack == null
            || _rack.IsFull
            || _board.CollectedCells >= _board.TotalCells
            || !_pool.IsSelectable(token))
        {
            return;
        }

        int selectionCount = _pool.GetSelectionCount(token);
        if (!_rack.CanCreate(selectionCount))
        {
            _presentation.ShowToast(selectionCount > 1 ? "锁链需要两个空位" : "收线台已满", 1.15f);
            return;
        }

        StartCoroutine(ResolveSpool(token, _roundId));
    }

    private IEnumerator ResolveSpool(YarnMatchSpoolToken token, int roundId)
    {
        if (!IsCurrentRound(roundId))
        {
            yield break;
        }

        YarnMatchPoolSelection preview = _pool.PreviewSelection(token);
        if (preview == null || !_rack.CanCreate(preview.Tokens.Count))
        {
            yield break;
        }

        List<YarnMatchRackEntry> entries = new List<YarnMatchRackEntry>(preview.Tokens.Count);
        for (int index = 0; index < preview.Tokens.Count; index++)
        {
            YarnMatchSpoolToken selectedToken = preview.Tokens[index];
            YarnMatchRackEntry entry = _rack.TryCreate(selectedToken.Color, selectedToken.Capacity);
            if (entry == null)
            {
                for (int createdIndex = 0; createdIndex < entries.Count; createdIndex++)
                {
                    _rack.Remove(entries[createdIndex]);
                }
                yield break;
            }
            entries.Add(entry);
        }

        YarnMatchPoolSelection selection = _pool.Consume(preview);
        if (selection == null)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                _rack.Remove(entries[index]);
            }
            yield break;
        }

        List<YarnMatchCollectionJob> jobs = new List<YarnMatchCollectionJob>(selection.Tokens.Count);
        for (int index = 0; index < selection.Tokens.Count; index++)
        {
            YarnMatchCollectionJob job = new YarnMatchCollectionJob
            {
                Token = selection.Tokens[index],
                SourceCell = selection.SourceCells[index],
                Entry = entries[index]
            };
            _collectionJobs.Add(job);
            jobs.Add(job);
            _presentation.ShowRackEntry(entries[index]);
            _presentation.HideSpoolImmediately(selection.Tokens[index]);
        }

        _presentation.RenderRack(_rack);
        _presentation.SetStatus(selection.Tokens.Count > 1
            ? "锁链滚筒同时飞向收线台"
            : YarnMatchUiTheme.GetColorName(token.Color) + "滚筒正在飞向收线台");
        for (int index = 0; index < jobs.Count; index++)
        {
            StartCoroutine(CompleteSpoolArrival(jobs[index], roundId));
        }

        _presentation.RenderPool(_pool, _state);
        _presentation.UpdateHeader(_board, _rack);
        _presentation.SetInteraction(_pool, _state, true);
        if (_rack.IsFull)
        {
            _presentation.SetSpoolSelection(_pool, _state, false);
        }

        for (int index = 0; index < selection.SourceCells.Count; index++)
        {
            YarnMatchPoolCell sourceCell = selection.SourceCells[index];
            if (sourceCell.SourceTunnel != null && _pendingReplenishments.Add(sourceCell))
            {
                StartCoroutine(ReplenishAfterDelay(sourceCell, roundId));
            }
        }
        yield break;
    }

    private IEnumerator CompleteSpoolArrival(YarnMatchCollectionJob job, int roundId)
    {
        yield return _presentation.PlaySpoolToRack(job.Token, job.Entry);
        if (!IsCurrentRound(roundId))
        {
            yield break;
        }

        _presentation.RenderPoolCell(_pool, job.SourceCell, _state);
        _presentation.SetInteraction(_pool, _state, true);
        job.Ready = true;
        _presentation.RenderRack(_rack);
        _presentation.UpdateHeader(_board, _rack);
        ScheduleCollections(roundId);
    }
    private void ScheduleCollections(int roundId)
    {
        if (!IsCurrentRound(roundId) || _state != YarnMatchGameState.Playing)
        {
            return;
        }

        if (_boardSettleInProgress)
        {
            return;
        }

        for (int jobIndex = 0; jobIndex < _collectionJobs.Count; jobIndex++)
        {
            YarnMatchCollectionJob job = _collectionJobs[jobIndex];
            if (!job.Ready || job.Completing)
            {
                continue;
            }

            int available = job.Entry.Capacity - job.Entry.Progress - job.PendingCells;
            while (available > 0)
            {
                YarnMatchBoardCell cell = FindAvailableExposedCell(job.Entry.Color);
                if (cell == null)
                {
                    break;
                }

                _claimedBoardCells.Add(cell);
                job.PendingCells++;
                available--;
                StartCoroutine(CollectCellForJob(job, cell, roundId));
            }
        }

        TryEvaluateTerminal(roundId);
    }

    private IEnumerator CollectCellForJob(YarnMatchCollectionJob job, YarnMatchBoardCell cell, int roundId)
    {
        yield return _presentation.PlayCellIntoRack(cell, job.Entry);
        if (!IsCurrentRound(roundId))
        {
            _claimedBoardCells.Remove(cell);
            job.PendingCells = Mathf.Max(0, job.PendingCells - 1);
            yield break;
        }

        bool removed = false;
        _claimedBoardCells.Remove(cell);
        if (_board.Remove(cell))
        {
            removed = true;
            _rack.AddProgress(job.Entry);
            _presentation.RenderBoard(_board, false);
            _presentation.RenderRack(_rack);
            _presentation.UpdateHeader(_board, _rack);
            StartCoroutine(_presentation.PlayRackImpact(job.Entry));
            TryUnlockFinalRackSlot();
        }

        job.PendingCells = Mathf.Max(0, job.PendingCells - 1);

        if (removed)
        {
            yield return WaitForBoardSettle(roundId);
        }

        ScheduleCollections(roundId);
        TryCompleteJob(job, roundId);
    }

    private bool HasPendingCollectionCells()
    {
        for (int index = 0; index < _collectionJobs.Count; index++)
        {
            if (_collectionJobs[index].PendingCells > 0)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitForBoardSettle(int roundId)
    {
        while (IsCurrentRound(roundId) && HasPendingCollectionCells())
        {
            yield return null;
        }

        if (!IsCurrentRound(roundId))
        {
            yield break;
        }

        if (_boardSettleInProgress)
        {
            while (IsCurrentRound(roundId) && _boardSettleInProgress)
            {
                yield return null;
            }

            yield break;
        }

        _boardSettleInProgress = true;
        yield return _presentation.PlayBoardDrop(_board);
        _boardSettleInProgress = false;
    }

    private YarnMatchBoardCell FindAvailableExposedCell(YarnMatchColor color)
    {
        for (int column = 0; column < _board.Columns.Count; column++)
        {
            List<YarnMatchBoardCell> stack = _board.Columns[column];
            if (stack.Count > 0 && stack[0].Color == color && !_claimedBoardCells.Contains(stack[0]))
            {
                return stack[0];
            }
        }

        return null;
    }

    private void TryCompleteJob(YarnMatchCollectionJob job, int roundId)
    {
        if (!IsCurrentRound(roundId) || job.Completing || job.PendingCells > 0 || job.Entry.Progress < job.Entry.Capacity)
        {
            return;
        }

        job.Completing = true;
        StartCoroutine(CompleteJob(job, roundId));
    }

    private IEnumerator CompleteJob(YarnMatchCollectionJob job, int roundId)
    {
        yield return _presentation.PlayRackPulse(job.Entry);
        if (!IsCurrentRound(roundId))
        {
            yield break;
        }

        _rack.Remove(job.Entry);
        _collectionJobs.Remove(job);
        _presentation.ShowToast("同色线团收满，滚筒离开收线台", 0.8f);
        _presentation.RenderRack(_rack);
        _presentation.UpdateHeader(_board, _rack);
        _presentation.SetSpoolSelection(_pool, _state, true);
        ScheduleCollections(roundId);
        TryEvaluateTerminal(roundId);
    }

    private IEnumerator ReplenishAfterDelay(YarnMatchPoolCell cell, int roundId)
    {
        yield return new WaitForSecondsRealtime(0.24f);
        if (!IsCurrentRound(roundId) || !_pendingReplenishments.Contains(cell))
        {
            yield break;
        }

        YarnMatchSpoolToken token = _pool.Replenish(cell);
        _pendingReplenishments.Remove(cell);
        if (token == null)
        {
            ScheduleCollections(roundId);
            yield break;
        }

        _presentation.RenderPoolCell(_pool, cell, _state);
        yield return _presentation.PlayPoolEmergence(token);
        ScheduleCollections(roundId);
    }

    private void TryEvaluateTerminal(int roundId)
    {
        if (!IsCurrentRound(roundId) || _state != YarnMatchGameState.Playing)
        {
            return;
        }

        if (HasResolutionInProgress())
        {
            return;
        }

        if (_board.CollectedCells >= _board.TotalCells
            && _pool.RemainingTokenCount == 0
            && _rack.Entries.Count == 0)
        {
            _inputLocked = true;
            StartCoroutine(FinishGame(true, "全部收好啦！", "每一格毛线都回到了收线台。"));
            return;
        }

        if (IsDeadlocked())
        {
            _inputLocked = true;
            StartCoroutine(FinishGame(false, "挑战失败", "收线台已满，当前没有可以继续收取的线团。"));
        }
    }

    private bool HasResolutionInProgress()
    {
        if (_boardSettleInProgress
            || _claimedBoardCells.Count > 0
            || _pendingReplenishments.Count > 0)
        {
            return true;
        }

        for (int index = 0; index < _collectionJobs.Count; index++)
        {
            YarnMatchCollectionJob job = _collectionJobs[index];
            if (!job.Ready
                || job.PendingCells > 0
                || job.Completing
                || job.Entry.Progress >= job.Entry.Capacity)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowHint()
    {
        if (_state != YarnMatchGameState.Playing || _inputLocked)
        {
            return;
        }

        if (_rack.IsFull)
        {
            _presentation.ShowToast("当前无可消除", 1.1f);
            return;
        }

        YarnMatchSpoolToken best = null;
        YarnMatchBoardCell bestCell = null;
        int bestScore = -1;
        int bestExposedCount = 0;
        List<YarnMatchSpoolToken> selectable = _pool.GetSelectableTokens();
        for (int index = 0; index < selectable.Count; index++)
        {
            YarnMatchSpoolToken token = selectable[index];
            if (!_rack.CanCreate(_pool.GetSelectionCount(token))) continue;
            int exposedCount = CountAvailableExposed(token.Color);
            if (exposedCount <= 0)
            {
                continue;
            }

            int score = exposedCount;
            if (_rack.Find(token.Color) != null)
            {
                score += 5;
            }

            if (score > bestScore)
            {
                best = token;
                bestCell = FindAvailableExposedCell(token.Color);
                bestScore = score;
                bestExposedCount = exposedCount;
            }
        }

        if (best == null || bestCell == null)
        {
            _presentation.ShowToast("当前无可消除", 1.1f);
            return;
        }

        StartCoroutine(_presentation.PulseSpool(best));
        StartCoroutine(_presentation.PulseBoardCell(bestCell));
        _presentation.ShowToast(
            "点击" + YarnMatchUiTheme.GetColorName(best.Color) + "滚筒，可收取 "
            + bestExposedCount + " 个线团",
            1.25f);
    }

    private void RequestRefresh()
    {
        if (_state != YarnMatchGameState.Playing || _inputLocked)
        {
            return;
        }

        if (HasResolutionInProgress())
        {
            _presentation.ShowToast("正在收线，请稍候再刷新", 1.1f);
            return;
        }

        if (_rack.IsFull)
        {
            _presentation.ShowToast("收线台已满，无法放入新的滚筒", 1.2f);
            return;
        }

        YarnMatchSpoolToken readyToken = FindCompletableSelectableSpool();
        if (readyToken != null)
        {
            StartCoroutine(_presentation.PulseSpool(readyToken));
            _presentation.ShowToast(
                YarnMatchUiTheme.GetColorName(readyToken.Color) + "滚筒已经可以直接收满",
                1.25f);
            return;
        }

        if (!TryFindRefreshTarget(out YarnMatchColor targetColor, out int exposedCount))
        {
            _presentation.ShowToast("当前没有可整理的滚筒", 1.1f);
            return;
        }

        YarnMatchSpoolToken arrangedToken = _pool.MakeColorSelectable(targetColor);
        if (arrangedToken == null)
        {
            _presentation.ShowToast("当前没有可整理的滚筒", 1.1f);
            return;
        }

        _presentation.RenderPool(_pool, _state);
        _presentation.SetInteraction(_pool, _state, true);
        StartCoroutine(_presentation.PulseSpool(arrangedToken));
        YarnMatchBoardCell exposedCell = FindAvailableExposedCell(targetColor);
        if (exposedCell != null)
        {
            StartCoroutine(_presentation.PulseBoardCell(exposedCell));
        }

        string result = exposedCount >= YarnMatchRackModel.CellsPerSpool
            ? "已整理出可以直接收满的" + YarnMatchUiTheme.GetColorName(targetColor) + "滚筒"
            : "当前露出不足三格，已整理出推进最多的" + YarnMatchUiTheme.GetColorName(targetColor) + "滚筒";
        _presentation.ShowToast(result, 1.5f);
    }

    private YarnMatchSpoolToken FindCompletableSelectableSpool()
    {
        List<YarnMatchSpoolToken> selectable = _pool.GetSelectableTokens();
        for (int index = 0; index < selectable.Count; index++)
        {
            if (_rack.CanCreate(_pool.GetSelectionCount(selectable[index]))
                && CountAvailableExposed(selectable[index].Color) >= selectable[index].Capacity)
            {
                return selectable[index];
            }
        }

        return null;
    }

    private bool TryFindRefreshTarget(out YarnMatchColor targetColor, out int exposedCount)
    {
        targetColor = default(YarnMatchColor);
        exposedCount = 0;
        int colorCount = Enum.GetValues(typeof(YarnMatchColor)).Length;
        for (int color = 0; color < colorCount; color++)
        {
            YarnMatchColor candidate = (YarnMatchColor)color;
            int candidateCount = CountAvailableExposed(candidate);
            if (candidateCount <= 0 || !_pool.HasRemainingToken(candidate))
            {
                continue;
            }

            bool candidateCompletes = candidateCount >= YarnMatchRackModel.CellsPerSpool;
            bool currentCompletes = exposedCount >= YarnMatchRackModel.CellsPerSpool;
            if ((candidateCompletes && !currentCompletes)
                || candidateCompletes == currentCompletes && candidateCount > exposedCount)
            {
                targetColor = candidate;
                exposedCount = candidateCount;
            }
        }

        return exposedCount > 0;
    }

    private void RequestRestart()
    {
        if (!_gameStarted || _state == YarnMatchGameState.Resolving)
        {
            return;
        }
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        _inputLocked = true;
        _state = YarnMatchGameState.Resolving;
        _presentation.SetInteraction(_pool, _state, false);
        yield return null;
        StartNewGame();
    }

    private IEnumerator FinishGame(bool won, string title, string detail)
    {
        if (_state != YarnMatchGameState.Playing)
        {
            yield break;
        }

        _state = won ? YarnMatchGameState.Won : YarnMatchGameState.Lost;
        if (won && !_specialChallenge && _selectedLevel >= _highestUnlockedLevel)
        {
            _highestUnlockedLevel = _selectedLevel + 1;
            SaveProgress();
        }
        _presentation.ShowResult(won, title, detail, !_specialChallenge);
        _presentation.SetInteraction(_pool, _state, false);
        yield return null;
    }

    private void TryUnlockFinalRackSlot()
    {
        if (_rack.TryUnlockFinalSlot(_board.CollectedCells, _board.TotalCells))
        {
            _presentation.RenderRack(_rack);
            _presentation.UpdateHeader(_board, _rack);
            _presentation.ShowToast("最后一格收线台已解锁", 1.5f);
        }
    }

    private bool IsDeadlocked()
    {
        if (_board.CollectedCells >= _board.TotalCells)
        {
            return false;
        }

        return _rack.IsFull;
    }

    private void RefreshDailyUnlockState()
    {
        string today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string savedDate = PlayerPrefs.GetString(DailyUnlockDateKey, string.Empty);
        if (savedDate != today)
        {
            _dailyUnlockActive = false;
            PlayerPrefs.SetString(DailyUnlockDateKey, today);
            PlayerPrefs.SetInt(DailyUnlockEnabledKey, 0);
            PlayerPrefs.Save();
            return;
        }

        _dailyUnlockActive = PlayerPrefs.GetInt(DailyUnlockEnabledKey, 0) == 1;
    }

    private int GetAvailableHighestLevel()
    {
        if (!_dailyUnlockActive)
        {
            return _highestUnlockedLevel;
        }

        int chapterStart = YarnMatchLevelCatalog.GetChapterStartLevel(_highestUnlockedLevel);
        return chapterStart + YarnMatchLevelCatalog.LevelsPerChapter - 1;
    }

    private void UnlockAllLevelsForToday()
    {
        RefreshDailyUnlockState();
        if (_dailyUnlockActive)
        {
            _presentation.ShowToast("今天已经解锁过全部关卡", 1.2f);
            return;
        }

        _dailyUnlockActive = true;
        string today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        PlayerPrefs.SetString(DailyUnlockDateKey, today);
        PlayerPrefs.SetInt(DailyUnlockEnabledKey, 1);
        PlayerPrefs.Save();
        _presentation.ShowLevelSelect(GetAvailableHighestLevel(), true);
        _presentation.ShowToast("今天的关卡已经全部解锁", 1.5f);
    }

    private int CountAvailableExposed(YarnMatchColor color)
    {
        int count = 0;
        for (int column = 0; column < _board.Columns.Count; column++)
        {
            List<YarnMatchBoardCell> stack = _board.Columns[column];
            if (stack.Count > 0
                && stack[0].Color == color
                && !_claimedBoardCells.Contains(stack[0]))
            {
                count++;
            }
        }

        return count;
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, _highestUnlockedLevel);
        PlayerPrefs.Save();
    }

    private bool IsCurrentRound(int roundId)
    {
        return _gameStarted && roundId == _roundId && _board != null && _pool != null && _rack != null;
    }
}
