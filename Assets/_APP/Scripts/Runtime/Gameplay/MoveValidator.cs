using UnityEngine;

namespace App.Gameplay
{
    public static class MoveValidator
    {
        /// <summary>
        /// 指定列に value を『置けるか？』を判定（まだ実際には置かない）
        /// </summary>
        public static bool CanPlace(int column, int value, BoardState board, RuleSet rules, out string reason)
        {
            reason = null;
            if (board == null || rules == null) { reason = "no_state"; return false; }

            // 範囲 & 高さ
            if (column < 0 || column >= board.ColumnCount) { reason = "out_of_range"; return false; }
            if (board.Height(column) >= rules.maxHeight) { reason = "max_height"; return false; }

            // 空列
            if (board.IsEmpty(column))
            {
                if (rules.emptyColumn == RuleSet.EmptyPolicy.Only2 && value != 2)
                {
                    reason = "empty_only_2";
                    return false;
                }
                reason = "ok_empty";
                return true;
            }

            // 何か乗っている列：降順 or 同値合成
            int top = board.Top(column);

            // 同値合成OK
            if (rules.allowEqualMerge && value == top) { reason = "ok_equal_merge"; return true; }

            // 降順OK（value が top 以下）
            if (rules.allowDescending && value < top) { reason = "ok_descending"; return true; }

            reason = "order_violation";
            return false;
        }
    }
}
