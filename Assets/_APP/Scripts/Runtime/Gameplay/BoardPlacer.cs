using System.Collections;
using TMPro;
using UnityEngine;

namespace App.Gameplay
{
    /// <summary>
    /// ボードへの配置＆合成を担当。
    /// - placeAtBottom=true：各列の「最下段」にプレースして下から迎撃（推奨）
    ///   * Stack(VerticalLayoutGroup) は「Reverse Arrangement」を ON にしておくと見た目が合う
    ///   * 同値は“下から”連鎖吸収（連続する同値だけが対象）
    /// - placeAtBottom=false：従来の“最上段”にプレースして上から合成
    /// - OnMerged / OnPlacedNoMerge を発火（ScoreSystem などが購読）
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

        [Header("Placement Mode")]
        [Tooltip("true: 最下段迎撃モード / false: 従来の最上段プレース")]
        public bool placeAtBottom = true;

        [Header("Animation")]
        [Tooltip("新規配置のポップ時間")]
        public float popIn = 0.12f;
        [Tooltip("合成時のパルス時間")]
        public float pulse = 0.12f;

        /// <summary>合成時に発火（合成後の最終値。例: 2+2→4 なら 4）</summary>
        public event System.Action<int> OnMerged;
        /// <summary>合成無しで新規配置したときに発火</summary>
        public event System.Action OnPlacedNoMerge;

        /// <summary>直近に配置を試みた列（演出側が参照したい場合に使用）</summary>
        public int LastPlacedColumnIndex { get; private set; } = -1;

        // ======================================================
        // Public API
        // ======================================================

        /// <summary>
        /// 現在の Waste の値を、指定列に「置く or 合成」する。
        /// placeAtBottom=true のときは最下段迎撃、false のときは従来の最上段。
        /// </summary>
        public bool TryPlaceAt(int columnIndex)
        {
            if (!IsReady() || columnIndex < 0 || columnIndex >= stacks.Length)
                return false;

            if (!int.TryParse(wasteValue?.text, out int value))
                return false;

            LastPlacedColumnIndex = columnIndex;

            if (placeAtBottom)
                return TryPlaceFromBottom(columnIndex, value);
            else
                return TryPlaceFromTop(columnIndex, value);
        }

        /// <summary>
        /// すべての Stack を空にする（エディタ・デバッグ用）。
        /// </summary>
        public void ClearAllStacks()
        {
            if (stacks == null) return;
            foreach (var s in stacks)
            {
                if (!s) continue;
                for (int i = s.childCount - 1; i >= 0; i--)
                {
                    var child = s.GetChild(i);
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        // ======================================================
        // Bottom placement（最下段迎撃）
        // ======================================================

        /// <summary>
        /// 最下段に挿入し、下から同値連鎖で吸収して倍化。
        /// 連続して並ぶ同値だけが対象（間に異なる値があればそこで止まる）。
        /// </summary>
        bool TryPlaceFromBottom(int columnIndex, int value)
        {
            var st = stacks[columnIndex];
            if (!st) return false;

            // 迎撃モードでは MoveValidator を通さず常にOK（ゲームデザイン側で制御）
            int merged = value;
            bool mergedAtLeastOnce = false;

            // A) 既存の“最下段から”見て、連続する同値をすべて吸収→倍化
            while (true)
            {
                var t = BottomText(columnIndex);
                if (t && int.TryParse(t.text, out int bVal) && bVal == merged)
                {
                    // 下端カードを即座に列から外して破棄（Destroy遅延対策）
                    var bottomRoot = t.transform.parent as RectTransform;
                    if (bottomRoot)
                    {
                        bottomRoot.SetParent(null, false);
                        Destroy(bottomRoot.gameObject);
                    }
                    merged *= 2;
                    mergedAtLeastOnce = true;

                    // 連鎖を強めたい場合は、この時点でパルスを出してもOK
                }
                else break;
            }

            // B) 最終値を“最下段”に新規挿入（SetSiblingIndex(0)）
            var go = Instantiate(cardViewPrefab, st);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = merged.ToString();
            go.transform.SetSiblingIndex(0); // ★ これで見た目の下端に来る（Reverse Arrangement 前提）

            var rt = go.GetComponent<RectTransform>();
            if (rt) StartCoroutine(Pop(rt));

            // C) 演出・通知・後処理
            ClearWaste();
            if (mergedAtLeastOnce)
            {
                // “段階ごと”に加点したい場合は、上の while 内で Invoke する方式に変更可
                OnMerged?.Invoke(merged);
                if (rt) StartCoroutine(Pulse(rt));
            }
            else
            {
                OnPlacedNoMerge?.Invoke();
            }

            return true;
        }

        TMP_Text BottomText(int col)
        {
            var st = stacks[col];
            if (!st || st.childCount == 0) return null;
            var bottom = st.GetChild(0);
            return bottom.GetComponentInChildren<TMP_Text>();
        }

        // ======================================================
        // Top placement（従来の最上段プレース）
        // ======================================================

        /// <summary>
        /// 従来の“最上段”に置くモード。MoveValidator に従って合法判定→同値連鎖（上から）。
        /// </summary>
        bool TryPlaceFromTop(int columnIndex, int value)
        {
            // 合法判定（BoardState/MoveValidator を利用）
            var state = new BoardState(stacks);
            if (!MoveValidator.CanPlace(columnIndex, value, state, ruleSet, out _))
                return false;

            // 1) 最上段と同値ならその場で合成（連鎖あり）
            if (ruleSet != null && ruleSet.allowEqualMerge)
            {
                var topText = TopText(columnIndex);
                if (topText && int.TryParse(topText.text, out int topVal) && topVal == value)
                {
                    int merged = value * 2;
                    topText.text = merged.ToString();
                    StartCoroutine(Pulse(topText.rectTransform));

                    if (ruleSet.allowChainMerge)
                    {
                        while (true)
                        {
                            var below = BelowTopText(columnIndex);
                            if (!below) break;

                            if (int.TryParse(below.text, out int bVal) && bVal == merged)
                            {
                                // 直下を列から外してから破棄（Destroy遅延対策）
                                var belowRoot = below.transform.parent as RectTransform;
                                if (belowRoot)
                                {
                                    belowRoot.SetParent(null, false);
                                    Destroy(belowRoot.gameObject);
                                }

                                merged *= 2;
                                topText.text = merged.ToString();
                                StartCoroutine(Pulse(topText.rectTransform));
                            }
                            else break;
                        }
                    }

                    ClearWaste();
                    OnMerged?.Invoke(merged);
                    return true;
                }
            }

            // 2) 合成しない場合は普通に最上段に追加
            var stack = stacks[columnIndex];
            var go = Instantiate(cardViewPrefab, stack);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = value.ToString();

            var rt = go.GetComponent<RectTransform>();
            if (rt) StartCoroutine(Pop(rt));

            ClearWaste();
            OnPlacedNoMerge?.Invoke();
            return true;
        }

        TMP_Text TopText(int col)
        {
            var st = stacks[col];
            if (!st || st.childCount == 0) return null;
            var top = st.GetChild(st.childCount - 1);
            return top.GetComponentInChildren<TMP_Text>();
        }

        TMP_Text BelowTopText(int col)
        {
            var st = stacks[col];
            if (!st || st.childCount < 2) return null;
            var below = st.GetChild(st.childCount - 2);
            return below.GetComponentInChildren<TMP_Text>();
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

        void ClearWaste()
        {
            if (wasteValue) wasteValue.text = "";
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
