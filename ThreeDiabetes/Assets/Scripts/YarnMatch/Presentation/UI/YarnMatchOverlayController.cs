using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class YarnMatchOverlayController
{
    private readonly MonoBehaviour _host;
    private readonly YarnMatchUiReferences _ui;
    private readonly YarnMatchAudio _audio;
    private Coroutine _toastRoutine;
    private Coroutine _resultRoutine;

    internal YarnMatchOverlayController(MonoBehaviour host, YarnMatchUiReferences ui, YarnMatchAudio audio)
    {
        _host = host;
        _ui = ui;
        _audio = audio;
    }

    internal void ResetForGame()
    {
        _ui.ResultOverlay.SetActive(false);
        _ui.MainMenuOverlay.SetActive(false);
        _ui.LevelSelectOverlay.SetActive(false);
    }

    internal void ShowMainMenu()
    {
        _ui.ResultOverlay.SetActive(false);
        _ui.LevelSelectOverlay.SetActive(false);
        _ui.MainMenuOverlay.SetActive(true);
    }

    internal void ShowLevelSelect(int highestUnlockedLevel, int focusLevel, bool dailyUnlockActive)
    {
        _ui.ResultOverlay.SetActive(false);
        _ui.MainMenuOverlay.SetActive(false);
        int safeHighestUnlockedLevel = Mathf.Max(1, highestUnlockedLevel);
        _ui.LevelSelectTitle.text = "选择关卡";
        _ui.LevelSelectSubtitle.text = "图案挑战";
        _ui.LevelList.Show(safeHighestUnlockedLevel, YarnMatchLevelCatalog.RevealedLevelCount, focusLevel);
        _ui.UnlockAllLevelsButton.interactable = !dailyUnlockActive;
        _ui.UnlockAllLevelsLabel.text = dailyUnlockActive ? "今日已全部解锁" : "今日全部解锁";
        _ui.LevelSelectOverlay.SetActive(true);
    }

    internal void SetStatus(string message)
    {
        _ui.StatusLabel.text = message;
    }

    internal void ShowToast(string message, float duration)
    {
        if (_toastRoutine != null)
        {
            _host.StopCoroutine(_toastRoutine);
        }
        _toastRoutine = _host.StartCoroutine(ToastRoutine(message, duration));
    }

    internal void ShowResult(bool won, string title, string detail, bool canAdvance)
    {
        _ui.ResultTitle.text = title;
        _ui.ResultTitle.color = won ? new Color(0.08f, 0.60f, 0.32f) : new Color(0.90f, 0.15f, 0.25f);
        _ui.ResultDetail.text = detail;
        _ui.ResultNextButton.gameObject.SetActive(won && canAdvance);
        _ui.ResultNextLabel.text = "下一关";
        _ui.ResultReplayButton.gameObject.SetActive(!won);
        _ui.ResultPreviewButton.gameObject.SetActive(!won);
        _ui.ResultHomeButton.gameObject.SetActive(true);
        _ui.ResultOverlay.SetActive(true);
        if (_resultRoutine != null)
        {
            _host.StopCoroutine(_resultRoutine);
        }
        _resultRoutine = _host.StartCoroutine(ResultRoutine());
        if (won)
        {
            _audio?.PlayWin();
        }
        else
        {
            _audio?.PlayFail();
        }
    }

    internal void ShowFailurePreview()
    {
        if (_resultRoutine != null)
        {
            _host.StopCoroutine(_resultRoutine);
            _resultRoutine = null;
        }

        _ui.ResultCanvasGroup.alpha = 1f;
        _ui.ResultPanelRect.localScale = Vector3.one;
        _ui.ResultOverlay.SetActive(false);
        SetStatus("残局预览：当前关卡已结束，可检查剩余线团与收线台");
        ShowToast("正在查看失败残局", 1.2f);
    }

    internal void Stop()
    {
        if (_toastRoutine != null)
        {
            _host.StopCoroutine(_toastRoutine);
            _toastRoutine = null;
        }
        if (_resultRoutine != null)
        {
            _host.StopCoroutine(_resultRoutine);
            _resultRoutine = null;
        }
    }

    private System.Collections.IEnumerator ToastRoutine(string message, float duration)
    {
        _ui.ToastLabel.text = message;
        _ui.ToastLabel.gameObject.SetActive(true);
        Color baseColor = _ui.ToastLabel.color;
        _ui.ToastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        float elapsed = 0f;
        while (elapsed < 0.16f)
        {
            elapsed += Time.unscaledDeltaTime;
            _ui.ToastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(elapsed / 0.16f));
            yield return null;
        }
        yield return new WaitForSecondsRealtime(duration);
        elapsed = 0f;
        while (elapsed < 0.20f)
        {
            elapsed += Time.unscaledDeltaTime;
            _ui.ToastLabel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - Mathf.Clamp01(elapsed / 0.20f));
            yield return null;
        }
        _ui.ToastLabel.gameObject.SetActive(false);
        _toastRoutine = null;
    }

    private System.Collections.IEnumerator ResultRoutine()
    {
        _ui.ResultCanvasGroup.alpha = 0f;
        _ui.ResultPanelRect.localScale = Vector3.one * 0.88f;
        float elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = YarnMatchUiTheme.EaseInOut(Mathf.Clamp01(elapsed / 0.28f));
            _ui.ResultCanvasGroup.alpha = progress;
            _ui.ResultPanelRect.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, progress);
            yield return null;
        }
        _ui.ResultCanvasGroup.alpha = 1f;
        _ui.ResultPanelRect.localScale = Vector3.one;
        _resultRoutine = null;
    }
}
