// TopCardHandle.cs
using App.Gameplay;
using App.UI;
using App.UI.Themeing;
using UnityEngine;
using UnityEngine.EventSystems;

public class TopCardHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public int columnIndex;
    public CardView cardView;   // そのカード
    public UIDragGhost ghost;   // 既存のゴースト（Canvas側にあるやつ）
    public BoardPlacer board;   // 参照

    public void OnPointerDown(PointerEventData e)
    {
        if (!board || board.GetStackCount(columnIndex) == 0) return;
        if (!ghost || !cardView) return;

        // ゴースト開始（列トップから）
        ghost.BeginFromColumn(
            columnIndex,
            cardView.gameObject.RectTransform(),  // ← gameObject にして呼ぶ
            cardView.Value
        );

    }

    public void OnDrag(PointerEventData e)
    {
        if (!ghost) return;
        ghost.UpdateFromColumn(e.position);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!ghost) return;
        ghost.EndFromColumn(e.position);
    }
}
