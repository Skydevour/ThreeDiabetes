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
    private YarnMatchCollectionScheduler _scheduler;
    private readonly HashSet<YarnMatchPoolCell> _pendingReplenishments = new HashSet<YarnMatchPoolCell>();
    private bool _gameStarted;
    private bool _inputLocked;
    private int _roundId;
    private int _selectedLevel = 1;
    private int _highestUnlockedLevel = 1;
    private YarnMatchBoardModel _board;
    private YarnMatchPoolModel _pool;
    private YarnMatchRackModel _rack;
    private YarnMatchPresentation _presentation;
    private YarnMatchGameState _state;
    private bool _dailyUnlockActive;
    private bool _specialChallenge;
    private Coroutine _roundBuildRoutine;
    private int _activeReplenishmentAnimations;
    private Coroutine _levelSelectRoutine;
    private readonly System.Random _refreshSeeds = new System.Random();

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
        YarnMatchLevelStore.Initialize(Application.persistentDataPath);
        StartCoroutine(PrepareSavedLevels());
    }

    private IEnumerator PrepareSavedLevels()
    {
        var preparation = YarnMatchLevelStore.EnsureLevelsAsync(_highestUnlockedLevel);
        while (!preparation.IsCompleted) yield return null;
        if (preparation.IsFaulted) Debug.LogException(preparation.Exception.GetBaseException());
    }

    private void OpenLevelSelect()
    {
        if (_levelSelectRoutine != null) return;
        _levelSelectRoutine = StartCoroutine(OpenLevelSelectRoutine());
    }

    private IEnumerator OpenLevelSelectRoutine()
    {
        _presentation.SetLoading(true);
        yield return null;
        var loading = YarnMatchLevelStore.EnsureLevelsAsync(_highestUnlockedLevel);
        while (!loading.IsCompleted) yield return null;
        _levelSelectRoutine = null;
        _presentation.SetLoading(false);
        if (loading.IsFaulted)
        {
            Debug.LogException(loading.Exception.GetBaseException());
            _presentation.ShowToast("关卡存档读取或保存失败，请重新打开", 2f);
            yield break;
        }
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
        if (_levelSelectRoutine != null)
        {
            StopCoroutine(_levelSelectRoutine);
            _levelSelectRoutine = null;
        }
        _gameStarted = false;
        _inputLocked = true;
        _state = YarnMatchGameState.Resolving;
        _roundId++;
        if (_roundBuildRoutine != null)
        {
            StopCoroutine(_roundBuildRoutine);
            _roundBuildRoutine = null;
        }
        _presentation.CancelAnimations();
        _presentation.ShowMainMenu();
    }

    private void StartNewGame()
    {
        _roundId++;
        _state = YarnMatchGameState.Resolving;
        _inputLocked = true;
        if (_roundBuildRoutine != null) StopCoroutine(_roundBuildRoutine);
        _presentation.CancelAnimations();
        _presentation.SetLoading(true);
        _roundBuildRoutine = StartCoroutine(BuildRound(_roundId));
    }

    private IEnumerator BuildRound(int roundId)
    {
        yield return null;
        var loading = YarnMatchLevelStore.GetAsync(_selectedLevel, _specialChallenge);
        while (!loading.IsCompleted) yield return null;
        if (roundId != _roundId) yield break;
        if (loading.IsFaulted)
        {
            Debug.LogException(loading.Exception.GetBaseException());
            _roundBuildRoutine = null;
            ReturnToMainMenu();
            _presentation.ShowToast("关卡存档读取或保存失败，请重新进入", 2f);
            yield break;
        }
        YarnMatchLevelSnapshot snapshot = loading.Result;
        var preparation = YarnMatchRoundData.PrepareAsync(snapshot);
        while (!preparation.IsCompleted) yield return null;
        if (roundId != _roundId) yield break;
        if (preparation.IsFaulted)
        {
            Debug.LogException(preparation.Exception.GetBaseException());
            _roundBuildRoutine = null;
            ReturnToMainMenu();
            _presentation.ShowToast("关卡准备失败，请重新进入", 2f);
            yield break;
        }
        YarnMatchRoundData data = preparation.Result;
        _collectionJobs.Clear();
        _pendingReplenishments.Clear();
        _activeReplenishmentAnimations = 0;
        _board = data.Board;
        _scheduler = new YarnMatchCollectionScheduler(_board, snapshot.Seed);
        _pool = data.Pool;
        _rack = new YarnMatchRackModel();
        _presentation.SetLevel(snapshot.Number);
        yield return _presentation.ResetGame(_board, _pool, _rack);
        if (roundId != _roundId) yield break;
        _state = YarnMatchGameState.Playing;
        _inputLocked = false;
        _presentation.SetInteraction(_pool, _state, true);
        _presentation.SetLoading(false);
        _roundBuildRoutine = null;
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

        while (_scheduler.TryReserve(_collectionJobs, out YarnMatchCollectionJob job, out YarnMatchBoardCell cell))
        {
            job.PendingCells++;
            StartCoroutine(CollectCellForJob(job, cell, roundId));
        }

        TryEvaluateTerminal(roundId);
    }

    private IEnumerator CollectCellForJob(YarnMatchCollectionJob job, YarnMatchBoardCell cell, int roundId)
    {
        yield return _presentation.PlayCellIntoRack(cell, job.Entry);
        if (!IsCurrentRound(roundId)) yield break;

        bool removed = _board.Remove(cell);
        if (removed)
        {
            _rack.AddProgress(job.Entry);
            _presentation.RenderRack(_rack);
            _presentation.UpdateHeader(_board, _rack);
            TryUnlockFinalRackSlot();
        }
        job.PendingCells--;
        TryCompleteJob(job, roundId);
        if (removed) yield return _presentation.PlayColumnDrop(cell.Column);
        if (!IsCurrentRound(roundId)) yield break;
        _scheduler.ReleaseColumn(cell.Column);
        ScheduleCollections(roundId);
    }

    private YarnMatchBoardCell FindAvailableExposedCell(YarnMatchColor color) => _scheduler.FindAvailable(color);

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

        _activeReplenishmentAnimations++;
        _presentation.RenderPoolCell(_pool, cell, _state);
        yield return _presentation.PlayPoolEmergence(token);
        if (!IsCurrentRound(roundId)) yield break;
        _activeReplenishmentAnimations--;
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
        if (_scheduler.IsBusy
            || _activeReplenishmentAnimations > 0
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

        if (!_pool.Refresh(_refreshSeeds.Next()))
        {
            _presentation.ShowToast("当前没有可整理的滚筒", 1.1f);
            return;
        }

        _presentation.RenderPool(_pool, _state);
        _presentation.SetInteraction(_pool, _state, true);
        _presentation.ShowToast("线轴已重新排列", 1.1f);
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
            if (YarnMatchLevelCatalog.GetChapterIndex(_highestUnlockedLevel)
                != YarnMatchLevelCatalog.GetChapterIndex(_selectedLevel))
                StartCoroutine(PrepareSavedLevels());
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

    private int CountAvailableExposed(YarnMatchColor color) => _scheduler.CountAvailable(color);

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
