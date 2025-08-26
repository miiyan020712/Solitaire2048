using UnityEngine;

namespace App.Gameplay
{
    [System.Serializable]
    public struct WeightedEntry
    {
        public int value;      // 2,4,8,...
        [Min(0)] public int weight; // 出現重み
    }

    [CreateAssetMenu(menuName = "2048Solitaire/Deck Definition", fileName = "DeckDefinition")]
    public class DeckDefinition : ScriptableObject
    {
        [Header("Build")]
        [Min(1)] public int initialCount = 42;
        public WeightedEntry[] entries = new WeightedEntry[]
        {
            new WeightedEntry{ value=2,  weight=60 },
            new WeightedEntry{ value=4,  weight=30 },
            new WeightedEntry{ value=8,  weight=8  },
            new WeightedEntry{ value=16, weight=2  },
        };

        [Header("Behavior")]
        public bool reshuffleWhenEmpty = false;
        public int fixedSeed = 0;

        [Header("Opening Gate")]
        [Min(0)] public int openingDraws = 5;     // 最初の何ドロー制限するか
        [Min(2)] public int openingMaxValue = 4;  // その間の最大値（例：4）

        // ↓↓↓ ここに追加 ↓↓↓
        [Header("Startup")]
        public bool guaranteeFirstIsTwo = true;
        public int firstCardValue = 2;
        // ↑↑↑ ここまで ↑↑↑
    }
}
