using UnityEngine;

namespace App.Gameplay
{
    public enum MergeScoreMode { NewValue /*2048式*/, /*拡張用*/ }

    [CreateAssetMenu(menuName = "Game/Score Policy")]
    public class ScorePolicy : ScriptableObject
    {
        [Header("Scoring")]
        public bool scoreOnMerge = true;
        public MergeScoreMode mergeScore = MergeScoreMode.NewValue;

        [Header("Combo")]
        public float comboStart = 1.0f;   // x1.0から開始
        public float comboStep  = 0.2f;   // 連続合成1回ごとに+0.2
        public float comboMax   = 3.0f;   // 上限x3.0
        public float comboTimeout = 3.0f; // 何秒合成が無ければx1.0に戻す
        public bool  resetOnNoMergePlacement = true; // 合成なしの配置でリセット
    }
}
