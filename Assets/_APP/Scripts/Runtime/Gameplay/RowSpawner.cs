using System.Linq;
using UnityEngine;

namespace App.Gameplay
{
public class RowSpawner : MonoBehaviour
{
    [Header("Refs")]
    public BoardPlacer board;          // 既存の配置管理
    public TimeBarController timeBar;  // NF1-01で作成
    public RuleSet ruleSet;            // 既存のルールアセット（MaxHeight使用）

    [Header("Spawn Values (power of two)")]
    public int minValue = 2;           // 2のべき乗のみ扱う前提
    public int maxValue = 32;          // 32まで（調整可）
    [Range(0f,1f)] public float emptyChancePerColumn = 0.25f; // 1列あたり「出現しない」確率
    public bool snapToValid = true;    // 置けない値は自動で丸める（下げる）
    public bool allowMergeOnSpawn = false; // 追加段では合成しない（基本false推奨）

    [Header("Pacing")]
    public float firstInterval = 5f;   // 開始間隔
    public float minInterval   = 2f;   // 下限
    public float accelPerRow   = -0.15f; // 1段ごとに間隔短縮量（-0.15秒ずつなど）

    [Header("Events (optional)")]
    public UnityEngine.Events.UnityEvent onGameOver;

    int rowsSpawned = 0;

    void OnEnable()
    {
        if (timeBar) timeBar.onTimeout.AddListener(OnTimeout);
    }
    void OnDisable()
    {
        if (timeBar) timeBar.onTimeout.RemoveListener(OnTimeout);
    }
    void Start()
    {
        RestartTimer();
    }

    void OnTimeout()
    {
        AddRow();
        rowsSpawned++;
        RestartTimer();
    }

    void RestartTimer()
    {
        if (!timeBar) return;
        float next = Mathf.Max(minInterval, firstInterval + accelPerRow * rowsSpawned);
        timeBar.ResetAndStart(next);
    }

    // ---- 段の追加本体 ----
    public void AddRow()
    {
        if (!board) return;

        for (int col = 0; col < board.ColumnCount; col++)
        {
            // もう一杯なら何もしない（後でGameOver判定）
            if (board.GetColumnCount(col) >= ruleSet.maxHeight) continue;

            // 空列にするか？
            if (Random.value < emptyChancePerColumn) continue;

            int v = RollValue();
            if (snapToValid)
            {
                v = SnapValueForColumn(col, v);
                if (v <= 0) continue; // どうしても置けない場合はスキップ
            }

            board.AddAtTop(col, v, animate: true, allowMergeOnSpawn: allowMergeOnSpawn);
        }

        // 追加後に敗北チェック
        if (board.AnyColumnAtOrAbove(ruleSet.maxHeight))
        {
            if (onGameOver != null) onGameOver.Invoke();
            // TODO: NF4 でゲームオーバー演出へ遷移
        }
    }

    // 2,4,8,16,32 から重み付きランダム（軽く低い値を出やすく）
    int RollValue()
    {
        int[] pool = new[] { 2, 4, 8, 16, 32 }
            .Where(x => x >= minValue && x <= maxValue).ToArray();

        // 低い値ほど出やすく：weight = 1 / log2(value)
        float[] w = pool.Select(x => 1f / Mathf.Log(x, 2f)).ToArray();
        float sum = w.Sum();
        float r = Random.value * sum;
        float acc = 0f;
        for (int i = 0; i < pool.Length; i++)
        {
            acc += w[i];
            if (r <= acc) return pool[i];
        }
        return pool[pool.Length - 1];
    }

    // 置けない値は「top以下」になるまで半減して合わせる
    int SnapValueForColumn(int col, int v)
    {
        int count = board.GetColumnCount(col);
        if (count == 0) return v; // 空列は何でもOK（今回のルール）

        int top = board.GetTopValue(col); // 列の最上段（見た目の上側）値
        if (v <= top) return v;

        while (v > top && v > 2) v /= 2; // 2まで下げて試す
        return (v <= top) ? v : -1;
    }
}
}
