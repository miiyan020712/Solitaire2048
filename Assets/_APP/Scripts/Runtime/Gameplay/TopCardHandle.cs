// TopCardHandle.cs
using App.Gameplay;
using App.UI;
using App.UI.Themeing;
using UnityEngine;
using UnityEngine.EventSystems;

public class TopCardHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("このカードが属する列。-1 のままでOK（自動で解決します）")]
    public int columnIndex = -1;

    public CardView   cardView;  // 自動で自分を拾います
    public UIDragGhost ghost;    // シーンの UIDragGhost を自動で拾います（手で入れてもOK）
    public BoardPlacer board;    // シーンの BoardPlacer を自動で拾います（手で入れてもOK）

    bool _dragging;

    void Awake()
    {
        if (!cardView) cardView = GetComponent<CardView>();
    }

    void OnEnable()
    {
        if (!board) board = FindObjectOfType<BoardPlacer>(true);
        if (!ghost) ghost = FindObjectOfType<UIDragGhost>(true);
        ResolveColumnIndex();
    }

    void OnTransformParentChanged()
    {
        // 親(Stack)が変わったら列を再計算
        ResolveColumnIndex();
    }

    void ResolveColumnIndex()
    {
        if (!board || columnIndex >= 0) return;
        var stack = transform.parent as RectTransform;     // このカードの親は Column_x/Stack
        if (!stack) return;

        var stacks = board.stacks;                         // BoardPlacer の public 配列を利用
        for (int i = 0; i < stacks.Length; i++)
        {
            if (stacks[i] == stack) { columnIndex = i; break; }
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!ghost || !board || !cardView) return;
        ResolveColumnIndex();
        if (columnIndex < 0) return;
        if (board.GetStackCount(columnIndex) == 0) return;

        // プレハブ内の RectTransform を直接取得（拡張メソッドに依存しない）
        var rt = cardView.GetComponent<RectTransform>();

        ghost.BeginFromColumn(columnIndex, rt, cardView.Value);
        _dragging = true;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging || !ghost) return;
        ghost.UpdateFromColumn(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging || !ghost) return;
        ghost.EndFromColumn(e.position);
        _dragging = false;
    }
}
