using UnityEngine;

namespace App.Gameplay
{
    // ゲームのルール値をまとめた入れ物（ScriptableObject）
    [CreateAssetMenu(fileName = "RuleSet", menuName = "2048Solitaire/Rule Set")]
    public class RuleSet : ScriptableObject
    {
        [Header("Board")]
        [Tooltip("1列の最大段数（この数以上は置けない）")]
        public int maxHeight = 12;

        public enum EmptyPolicy { Only2, Any }
        [Header("Empty Column")]
        [Tooltip("空列に置ける条件：とりあえず『2のみ』で進める")]
        public EmptyPolicy emptyColumn = EmptyPolicy.Only2;

        [Header("Order / Merge")]
        [Tooltip("降順（上が大きい）であれば置いてOKにするか")]
        public bool allowDescending = true;

        [Tooltip("同値は合成可能として『置ける』扱いにするか")]
        public bool allowEqualMerge = true;

        // スコア係数・コンボ等は後で追加予定
    }
}
