using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Board")]
    public int columns = 7;                 // 列数（5〜8で後で調整可）
    public bool beginnerHighlights = true;  // 合法手ハイライト（初心者モード）

    [Header("Empty Column (progressive)")]
    // 空列進化：2 → 2/4 → 2/4/8
    public bool useEmptyColumnProgression = true;
    public int upgradeAtClearCount1 = 1;    // 何回目に 2/4 になるか
    public int upgradeAtClearCount2 = 2;    // 何回目に 2/4/8 になるか

    [Header("Relief")]
    public int initialPasses = 3;           // 一手パスの初期回数
    public int passScoreCost = 50;          // パスのスコアコスト
    public int maxBonusStacks = 2;          // ボーナスドローの所持上限
    public int undoSteps = 1;               // アンドゥ可能手数
    public int undoScoreCost = 100;         // アンドゥのスコアコスト

    [Header("Scoring")]
    public bool useComboBonus = true;
    public float comboX1_2 = 1.2f;          // 同一手で2回合成
    public float comboX1_5 = 1.5f;          // 同一手で3回合成
    public float comboX2_0 = 2.0f;          // 同一手で4回以上

    [Header("Win/Lose")]
    public int winTarget = 2048;            // 勝利目標タイル
}
