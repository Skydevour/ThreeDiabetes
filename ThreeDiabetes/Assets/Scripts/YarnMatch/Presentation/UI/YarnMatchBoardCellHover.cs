using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class YarnMatchBoardCellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action<PointerEventData> _onEnter;
    private Action<PointerEventData> _onExit;
    private bool _isPointerOver;

    internal void Configure(Action<PointerEventData> onEnter, Action<PointerEventData> onExit)
    {
        _onEnter = onEnter;
        _onExit = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerOver = true;
        _onEnter?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerOver = false;
        _onExit?.Invoke(eventData);
    }

    private void OnDisable()
    {
        if (!_isPointerOver)
        {
            return;
        }

        _isPointerOver = false;
        _onExit?.Invoke(null);
    }
}
