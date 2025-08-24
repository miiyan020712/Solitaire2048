using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace App.Gameplay
{
    /// <summary>
    /// いまのUIから数値を読み取り、純粋な盤面データとして持つ軽量クラス
    /// （この段階では生成・破棄はしない。判定用に読むだけ）
    /// </summary>
    public class BoardState
    {
        readonly List<int>[] _cols;

        public int ColumnCount => _cols.Length;
        public BoardState(RectTransform[] stacks)
        {
            _cols = new List<int>[stacks.Length];
            for (int i = 0; i < stacks.Length; i++)
                _cols[i] = ReadColumn(stacks[i]);
        }

        static List<int> ReadColumn(RectTransform stack)
        {
            var list = new List<int>();
            if (!stack) return list;
            // 下から上へ並んでいる前提だが、上だけ分かればよい。
            // いちおう全段読む：Text(TMP)を探して数字に。
            for (int i = 0; i < stack.childCount; i++)
            {
                var child = stack.GetChild(i);
                var t = child.GetComponentInChildren<TMP_Text>();
                if (t && int.TryParse(t.text, out int v)) list.Add(v);
            }
            return list;
        }

        public int Height(int col) => InRange(col) ? _cols[col].Count : 0;
        public bool IsEmpty(int col) => Height(col) == 0;

        /// <summary>その列の一番上の値（無ければ0）</summary>
        public int Top(int col)
        {
            if (!InRange(col) || _cols[col].Count == 0) return 0;
            return _cols[col][_cols[col].Count - 1];
        }

        bool InRange(int c) => 0 <= c && c < _cols.Length;
    }
}
