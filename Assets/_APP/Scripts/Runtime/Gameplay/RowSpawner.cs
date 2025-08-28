using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using App.UI;     

namespace App.Gameplay
{
    /// <summary>
    /// 一定間隔で上段に1行スポーンさせる。
    /// ・値は「下げない」…行を出すごとに最小値を段階的に引き上げる
    /// ・GameOver は列の枚数（childCount）で即時判定
    /// </summary>
    public class RowSpawner : MonoBehaviour
    {
        [Header("Refs")]
        public BoardPlacer board;                 // BoardPlacer をドラッグ
        public TimeBarController timeBar;         // TimeBar_BG 側の TimeBarController
        public RuleSet ruleSet;                   // 使用中の RuleSet

        [Header("Spawn Values (power of two)")]
        public int minValue = 2;                  // 初期の最小値（2）
        public int maxValue = 32;                 // 最高32まで
        [Range(0f, 1f)] public float emptyChancePerColumn = 0.25f;
        public bool snapToValid = true;           // 使わない場合も true でOK（今回は単純に置く）
        public bool allowMergeOnSpawn = false;    // スポーン直後に合成させたいなら true

        [Header("Pacing")]
        public float firstInterval = 5f;          // 最初の間隔
        public float minInterval = 2f;            // 最小間隔
        public float accelPerRow = -0.15f;        // 行ごとに短くする値（負で加速）

        [Header("Events (optional)")]
        public UnityEvent onGameOver;

        // ---- runtime ----
        bool _gameOver;
        float _interval;
        int _rowsSpawned;
        int _currentMin;                          // 「下げない」ための下限
        readonly int[] _pow2 = { 2,4,8,16,32,64,128,256,512,1024,2048 };

        void OnEnable()
        {
            _gameOver = false;
            _rowsSpawned = 0;
            _currentMin = Mathf.Clamp(minValue, 2, maxValue);
            _interval = firstInterval;

            StopAllCoroutines();
            StartCoroutine(SpawnLoop());
        }

        IEnumerator SpawnLoop()
        {
            // 1フレーム待って参照の遅延を回避
            yield return null;

if (timeBar) timeBar.Play(_interval);   // ← 追加（初回分）
while (!_gameOver)
{
    // 先頭の Play はそのままでも OK、二重に回したくなければここは消しても良い
    // if (timeBar) timeBar.Play(_interval);

    yield return new WaitForSeconds(_interval);
    if (_gameOver) yield break;

    SpawnOneRow();

    if ((_rowsSpawned + 1) % 4 == 0 && _currentMin < maxValue)
        _currentMin = Mathf.Min(maxValue, _currentMin * 2);

    _rowsSpawned++;
    _interval = Mathf.Max(minInterval, _interval + accelPerRow);

    // 次ループを待たずに“新しい間隔”で即リセットしたい場合はここで Play
    // if (timeBar) timeBar.Play(_interval);
}

        }

        void SpawnOneRow()
        {
            if (board == null) return;

            int cols = board.ColumnCount;

            for (int c = 0; c < cols; c++)
            {
                // 空にするスロット
                if (Random.value < emptyChancePerColumn) continue;

                int value = NextSpawnValue();

                // 上段にスポーン（見た目の一番上。合成は allowMergeOnSpawn で選択）
                board.AddAtTop(c, value, animate: true, allowMergeOnSpawn: allowMergeOnSpawn);
            }

            // スポーン直後に即時判定（見た目やレイアウトに依存しない）
            if (ReachedLimit())
            {
                GameOver();
            }
        }

        int NextSpawnValue()
        {
            // 下限 _currentMin ～ 上限 maxValue のパワーオブツーからランダム
            var choices = GetPow2Range(_currentMin, maxValue);
            // 念のため保険
            if (choices.Count == 0) return _currentMin;

            int idx = Random.Range(0, choices.Count);
            return choices[idx];
        }

        List<int> GetPow2Range(int from, int to)
        {
            var list = new List<int>(8);
            for (int i = 0; i < _pow2.Length; i++)
            {
                int v = _pow2[i];
                if (v >= from && v <= to) list.Add(v);
            }
            return list;
        }

bool ReachedLimit()
{
    // RuleSet より見た目優先。両方あるなら小さい方を使うのもアリ
    int uiLimit = (board != null) ? board.VisualRowCapacity : 12;
    int ruleLimit = (ruleSet != null) ? ruleSet.maxHeight : 999;
    int limit = Mathf.Min(uiLimit, ruleLimit);

    int cols = board.ColumnCount;
    for (int c = 0; c < cols; c++)
    {
        if (board.GetStackCount(c) >= limit)   // ← childCount で即判定
            return true;
    }
    return false;
}

        void GameOver()
        {
            if (_gameOver) return;
            _gameOver = true;

            // タイムバー停止（Pause(true) を明示）
            if (timeBar != null) timeBar.Pause(true);

            enabled = false;
            onGameOver?.Invoke();
            Debug.Log("[RowSpawner] GAME OVER");
        }
    }
}
