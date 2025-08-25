using System.Collections;
using TMPro;
using UnityEngine;

namespace App.Gameplay
{
    /// <summary>
    /// ボードへの配置＆合成を担当。
    /// - MoveValidator で合法判定
    /// - 合成が起きたら最上段の値を倍化（連鎖は RuleSet.allowChainMerge）
    /// - 合成/非合成をイベントで通知（ScoreSystem などが購読）
    /// - 簡易アニメ（Pop / Pulse）
    /// </summary>
    public class BoardPlacer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("各列の Stack（左→右）")]
        public RectTransform[] stacks;
        [Tooltip("CardView のプレハブ（子に TMP_Text がある想定）")]
        public GameObject cardViewPrefab;
        [Tooltip("Waste の数値テキスト（BottomBar/WasteView/WasteValue）")]
        public TMP_Text wasteValue;
        [Tooltip("ルール（降順/空列/合成連鎖など）")]
        public RuleSet ruleSet;

        [Header("Animation")]
        [Tooltip("新規配置のポップ時間")]
        public float popIn = 0.12f;
        [Tooltip("合成時のパルス時間")]
        public float pulse = 0.12f;

        // --- Score/Combo などが購読するためのイベント ---
        /// <summary>合成時に発火（合成後の値を渡す。例: 2+2→4 なら 4）</summary>
        public event System.Action<int> OnMerged;
        /// <summary>合成なしで新規配置したときに発火</summary>
        public event System.Action OnPlacedNoMerge;

        // ======================================================
        // Public API
        // ======================================================

        /// <summary>
        /// 現在の Waste の値を、指定列に「置く or 合成」。
        /// MoveValidator に通らなければ false。
        /// </summary>
        public bool TryPlaceAt(int columnIndex)
        {
            if (!IsReady() || columnIndex < 0 || columnIndex >= stacks.Length)
                return false;

            // Waste の値を読む
            if (!int.TryParse(wasteValue.text, out int value))
                return false;

            // 合法判定
            var state = new BoardState(stacks);
            if (!MoveValidator.CanPlace(columnIndex, value, state, ruleSet, out _))
                return false;

            // 1) 置く前に「同値合成」チェック（最上段の値と同じなら合成）
            if (ruleSet != null && ruleSet.allowEqualMerge)
            {
                var topText = GetTopText(columnIndex);
                if (topText != null && int.TryParse(topText.text, out int topVal) && topVal == value)
                {
                    int merged = value * 2;
                    topText.text = merged.ToString();

                    // 合成アニメ（パルス）
                    StartCoroutine(Pulse(topText.rectTransform));

                    // 連鎖設定があれば、下に同値が続く限り倍化＆下段削除
if (ruleSet.allowChainMerge)
{
    while (true)
    {
        var below = GetBelowTopText(columnIndex);
        if (!below) break;

        if (int.TryParse(below.text, out int bVal) && bVal == merged)
        {
            var belowRoot = below.transform.parent as RectTransform;
            if (belowRoot != null)
            {
                belowRoot.SetParent(null, false); // ← 即時にスタックから外す
                Destroy(belowRoot.gameObject);    // ← 実際の破棄はフレーム末
            }

            merged *= 2;
            topText.text = merged.ToString();
            StartCoroutine(Pulse(topText.rectTransform));
        }
        else break;
    }
}


                    // Waste を消費（補充は別システムで）
                    ClearWaste();
                    OnMerged?.Invoke(merged);
                    return true;
                }
            }

            // 2) 合成しない場合は新規に 1 枚生成して積む
            var stack = stacks[columnIndex];
            var go = Instantiate(cardViewPrefab, stack);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = value.ToString();

            var rt = go.GetComponent<RectTransform>();
            if (rt != null) StartCoroutine(Pop(rt));

            ClearWaste();
            OnPlacedNoMerge?.Invoke();
            return true;
        }

        /// <summary>
        /// （任意）すべての Stack を空にする。ランタイム用の簡易クリア。
        /// </summary>
        public void ClearAllStacks()
        {
            if (stacks == null) return;
            foreach (var s in stacks)
            {
                if (s == null) continue;
                for (int i = s.childCount - 1; i >= 0; i--)
                {
                    var child = s.GetChild(i);
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        // ======================================================
        // Helpers
        // ======================================================

        bool IsReady()
        {
            return stacks != null && stacks.Length > 0 &&
                   cardViewPrefab != null &&
                   wasteValue != null &&
                   ruleSet != null;
        }

        TMP_Text GetTopText(int col)
        {
            var st = stacks[col];
            if (st == null || st.childCount == 0) return null;
            var top = st.GetChild(st.childCount - 1);
            return top.GetComponentInChildren<TMP_Text>();
        }

        TMP_Text GetBelowTopText(int col)
        {
            var st = stacks[col];
            if (st == null || st.childCount < 2) return null;
            var below = st.GetChild(st.childCount - 2);
            return below.GetComponentInChildren<TMP_Text>();
        }

        void ClearWaste()
        {
            if (wasteValue != null) wasteValue.text = "";
        }

        // ======================================================
        // Simple Animations
        // ======================================================

        IEnumerator Pop(RectTransform rt)
        {
            Vector3 start = Vector3.one * 0.2f;
            Vector3 end = Vector3.one;
            float t = 0f;
            while (t < popIn)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, popIn));
                rt.localScale = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, u));
                yield return null;
            }
            rt.localScale = end;
        }

        IEnumerator Pulse(RectTransform rt)
        {
            Vector3 a = Vector3.one * 0.92f;
            Vector3 b = Vector3.one;
            float t = 0f;
            while (t < pulse)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Sin(Mathf.Clamp01(t / Mathf.Max(0.0001f, pulse)) * Mathf.PI); // 0→1→0
                rt.localScale = Vector3.Lerp(b, a, u * 0.25f);
                yield return null;
            }
            rt.localScale = b;
        }
    }
}
