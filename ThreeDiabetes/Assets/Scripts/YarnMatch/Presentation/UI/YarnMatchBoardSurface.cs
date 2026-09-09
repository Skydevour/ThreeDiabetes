using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
internal sealed class YarnMatchBoardSurface : MaskableGraphic,
    IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private readonly List<YarnMatchBoardColumnGraphic> _columns = new List<YarnMatchBoardColumnGraphic>();
    private readonly Stack<Image> _freeTiles = new Stack<Image>();
    private readonly Dictionary<YarnMatchBoardCell, Image> _activeTiles = new Dictionary<YarnMatchBoardCell, Image>();
    private int _columnCount;
    private float _cellSize;
    private Action<YarnMatchBoardCell> _onHover;
    private YarnMatchBoardCell _hovered;
    private bool _inside;
    private Vector2 _pointer;
    private Camera _pointerCamera;

    internal float CellSize => _cellSize;

    internal void Configure(YarnMatchBoardModel board, float cellSize, Action<YarnMatchBoardCell> onHover)
    {
        foreach (var image in _activeTiles.Values)
        {
            image.gameObject.SetActive(false);
            _freeTiles.Push(image);
        }
        _activeTiles.Clear();
        _inside = false;
        _hovered = null;
        _onHover = onHover;
        _columnCount = board.Columns.Count;
        _cellSize = cellSize;
        canvasRenderer.cullTransparentMesh = false;
        rectTransform.sizeDelta = new Vector2(YarnMatchUiTheme.BoardViewportWidth, YarnMatchUiTheme.BoardViewportHeight);
        while (_columns.Count < _columnCount)
        {
            var column = YarnMatchUiPrimitives.CreateChild("Yarn Column", transform)
                .AddComponent<YarnMatchBoardColumnGraphic>();
            column.rectTransform.sizeDelta = rectTransform.sizeDelta;
            _columns.Add(column);
        }
        for (int i = 0; i < _columns.Count; i++)
        {
            _columns[i].gameObject.SetActive(i < _columnCount);
            if (i < _columnCount) _columns[i].Configure(board.Columns[i], Position(i, 0).x, cellSize);
        }
    }

    internal Vector2 Position(int column, int row) => new Vector2(
        (column - (_columnCount - 1) * 0.5f) * _cellSize,
        -YarnMatchUiTheme.BoardViewportHeight * 0.5f + (row + 0.5f) * _cellSize);

    internal Image AcquireTile(YarnMatchBoardCell cell)
    {
        Image image = _freeTiles.Count > 0 ? _freeTiles.Pop()
            : YarnMatchUiPrimitives.CreateImage("Collecting Yarn", transform,
                YarnMatchVisualFactory.GetSolidSprite(), Color.white, Vector2.one, Vector2.zero, false);
        image.raycastTarget = false;
        image.color = YarnMatchUiTheme.Palette[(int)cell.Color];
        image.rectTransform.sizeDelta = Vector2.one * _cellSize;
        image.rectTransform.anchoredPosition = Position(cell.Column, cell.Row);
        image.rectTransform.localScale = Vector3.one;
        image.rectTransform.SetAsLastSibling();
        image.gameObject.SetActive(true);
        _activeTiles.Add(cell, image);
        _columns[cell.Column].HiddenCell = cell;
        _columns[cell.Column].SetVerticesDirty();
        return image;
    }

    internal void ReleaseTile(YarnMatchBoardCell cell)
    {
        Image image = _activeTiles[cell];
        _activeTiles.Remove(cell);
        image.gameObject.SetActive(false);
        _freeTiles.Push(image);
    }

    internal void BeginDrop(int column)
    {
        _columns[column].HiddenCell = null;
        SetDrop(column, 0f);
    }

    internal void SetDrop(int column, float progress) => _columns[column].SetDropOffset(_cellSize * (1f - progress));

    internal void Highlight(YarnMatchBoardCell cell, float amount)
    {
        var column = _columns[cell.Column];
        column.HighlightCell = amount > 0f ? cell : null;
        column.HighlightAmount = amount;
        column.SetVerticesDirty();
    }

    public void OnPointerMove(PointerEventData data) => Track(data);
    public void OnPointerEnter(PointerEventData data) => Track(data);
    public void OnPointerDown(PointerEventData data) => Track(data);
    public void OnPointerUp(PointerEventData data)
    {
        if (data.pointerId >= 0) OnPointerExit(data);
    }
    public void OnPointerExit(PointerEventData data)
    {
        _inside = false;
        SetHover(null);
    }
    private void Track(PointerEventData data)
    {
        _pointer = data.position;
        _pointerCamera = data.enterEventCamera;
        _inside = true;
        RefreshHover();
    }
    private void LateUpdate()
    {
        if (_inside) RefreshHover();
    }
    private void RefreshHover()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, _pointer, _pointerCamera, out Vector2 point);
        int column = Mathf.FloorToInt(point.x / _cellSize + _columnCount * 0.5f);
        SetHover(column >= 0 && column < _columnCount ? _columns[column].Pick(point.y) : null);
    }
    private void SetHover(YarnMatchBoardCell cell)
    {
        if (_hovered == cell) return;
        _hovered = cell;
        _onHover?.Invoke(cell);
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        // One invisible hit surface replaces raycast components on every yarn tile.
        mesh.Clear();
        Rect area = rectTransform.rect;
        mesh.AddVert(new Vector3(area.xMin, area.yMin), Color.clear, Vector2.zero);
        mesh.AddVert(new Vector3(area.xMin, area.yMax), Color.clear, Vector2.zero);
        mesh.AddVert(new Vector3(area.xMax, area.yMax), Color.clear, Vector2.zero);
        mesh.AddVert(new Vector3(area.xMax, area.yMin), Color.clear, Vector2.zero);
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);
    }
    protected override void OnDisable()
    {
        _inside = false;
        SetHover(null);
        base.OnDisable();
    }
}
