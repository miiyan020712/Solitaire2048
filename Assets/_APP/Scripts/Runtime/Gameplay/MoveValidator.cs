using UnityEngine;

namespace App.Gameplay
{
    public static class MoveValidator
    {
        /// <summary>
        /// 指定列に value を『置けるか？』を判定（まだ実際には置かない）
        /// </summary>
        public static bool CanPlace(int columnIndex, int value, BoardState state, RuleSet rules, out string reason)
        {
            reason = "unknown";

            // --- 空列：今回は「制限撤廃」 ---
            if (state.IsEmpty(columnIndex))
            {
                reason = "ok_empty";
                return true;
                // もし旧仕様の「Only2」enumを残しているなら:
                // return (rules.emptyColumn == EmptyColumnPolicy.Any) || (rules.emptyColumn == EmptyColumnPolicy.Only2 && value == 2);
            }

            // --- 非空列：トップ値取得 ---
            int top = state.Top(columnIndex);

            // --- 同値合成は常にOK（実際の合成は BoardPlacer 側で実行） ---
            if (rules.allowEqualMerge && value == top)
            {
                reason = "ok_equal_merge";
                return true;
            }

            // --- 並び順（降順 or 昇順）---
            if (rules.allowDescending)
            {
                // 降順: 置く値 <= トップ ならOK
                if (value <= top) { reason = "ok_descending"; return true; }
                reason = "order_violation"; // 例: 4の上に8 を禁止
                return false;
            }
            else
            {
                // （参考）昇順運用に切り替える場合はこちら
                if (value >= top) { reason = "ok_ascending"; return true; }
                reason = "order_violation";
                return false;
            }
        }

    }
}
