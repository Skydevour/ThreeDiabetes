using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
internal sealed class YarnMatchBoardColumnGraphic : MaskableGraphic
{
    private List<YarnMatchBoardCell> _cells;
    private float _x;
    private float _size;
    private float _dropOffset;
    internal YarnMatchBoardCell HiddenCell;
    internal YarnMatchBoardCell HighlightCell;
    internal float HighlightAmount;

    public override Texture mainTexture => YarnMatchVisualFactory.GetCellPatternTexture();

    internal void Configure(List<YarnMatchBoardCell> cells, float x, float size)
    {
        _cells = cells;
        _x = x;
        _size = size;
        _dropOffset = 0f;
        HiddenCell = null;
        HighlightCell = null;
        raycastTarget = false;
        SetVerticesDirty();
    }

    internal void SetDropOffset(float offset)
    {
        _dropOffset = offset;
        SetVerticesDirty();
    }

    internal YarnMatchBoardCell Pick(float y)
    {
        int row = Mathf.FloorToInt((y + YarnMatchUiTheme.BoardViewportHeight * 0.5f - _dropOffset) / _size);
        int low = 0, high = _cells.Count - 1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            YarnMatchBoardCell cell = _cells[middle];
            if (cell.Row == row) return cell == HiddenCell ? null : cell;
            if (cell.Row < row) low = middle + 1;
            else high = middle - 1;
        }
        return null;
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (_cells == null) return;
        float bottom = -YarnMatchUiTheme.BoardViewportHeight * 0.5f;
        float top = -bottom;
        float left = _x - _size * 0.5f;
        foreach (YarnMatchBoardCell cell in _cells)
        {
            float y = bottom + cell.Row * _size + _dropOffset;
            if (y > top + _size) break;
            if (cell == HiddenCell || y + _size < bottom - _size) continue;
            Color tint = YarnMatchUiTheme.Palette[(int)cell.Color];
            if (cell == HighlightCell) tint = Color.Lerp(tint, Color.white, HighlightAmount);
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, y), tint, new Vector2(0f, 0f));
            mesh.AddVert(new Vector3(left, y + _size), tint, new Vector2(0f, 1f));
            mesh.AddVert(new Vector3(left + _size, y + _size), tint, new Vector2(1f, 1f));
            mesh.AddVert(new Vector3(left + _size, y), tint, new Vector2(1f, 0f));
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
