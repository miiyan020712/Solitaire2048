using App.Gameplay;
using App.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using CardView = App.UI.Themeing.CardView;

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

        [Header("Layout")]
        [SerializeField] private int visualRowCapacity = 10;  // 画面に実際に入る段数（後でInspectorで調整）
        public int VisualRowCapacity => visualRowCapacity;

        [Header("Lock")] public bool inputLocked = false;
        public void SetLocked(bool v) => inputLocked = v;

        [Header("Drag")]
        public UIDragGhost dragGhost;   // ← インスペクタで UIDragGhost をドラッグ



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
            if (inputLocked) return false; // or return;

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
            if (inputLocked) return false; // or return;
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
            if (inputLocked) return false; // or return;
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

        // 列数
        public int ColumnCount => stacks.Length;

        // その列のカード数（CardView 子オブジェクト数ベース）
        public int GetColumnCount(int col) => stacks[col].childCount;

        // その列の「最上段（見た目の上）」の値
        public int GetTopValue(int col)
        {
            var stack = stacks[col];
            if (stack.childCount == 0) return int.MaxValue; // 空列は何でも載る扱い
            var topCard = stack.GetChild(0).GetComponent<CardView>();
            return topCard ? topCard.Value : int.MaxValue;
        }

        // その列の高さが threshold 以上のものがあるか
        public bool AnyColumnAtOrAbove(int threshold)
        {
            for (int i = 0; i < stacks.Length; i++)
                if (stacks[i].childCount >= threshold) return true;
            return false;
        }

        // 段生成用：列の最上段にカードを追加（基本合成しない）
        public void AddAtTop(int col, int value, bool animate = true, bool allowMergeOnSpawn = false)
        {
            var stack = stacks[col];
            var card = Instantiate(cardViewPrefab, stack);
            card.SetValue(value);

            // 見た目の「上」に来るよう先頭に移動
            card.transform.SetAsFirstSibling();

            // ここ！ () を付ける
            if (animate) StartCoroutine(PopIn(card.RectTransform()));
        }

        // ちょっとしたポップイン
        System.Collections.IEnumerator PopIn(RectTransform rt)
        {
            Vector3 from = Vector3.one * 0.01f;
            Vector3 to = Vector3.one;
            float t = 0f, dur = 0.12f;
            rt.localScale = from;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.localScale = Vector3.LerpUnclamped(from, to, Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            rt.localScale = to;
        }


        public int GetHeight(int col) => stacks[col].childCount;

        public bool CanPlaceAt(int col, int value, bool allowMergeOnSpawn)
        {
            var s = stacks[col];
            if (s.childCount == 0) return true;

            // 先頭が最上段（AddAtTopでSetAsFirstSiblingしている想定）
            var top = s.GetChild(0).GetComponent<CardView>();
            int topVal = top.Value;

            if (!allowMergeOnSpawn && value == topVal) return false; // 同値合成禁止ならNG
            if (ruleSet.allowDescending && value > topVal) return false; // 降順ルール

            return (value <= topVal);
        }

        public int GetStackCount(int column)
        {
            return stacks[column].childCount;   // その列のカード枚数
        }

        public int GetChildCount(int col)
        {
            return stacks[col].childCount;   // ← Stack の RectTransform の子数
        }

        // ======================================================
        // ★ ここから追加：トップ移動（列→列）
        // ======================================================

        /// <summary>fromCol のトップを toCol に置けるか（合体可）。</summary>
        public bool CanMoveTop(int fromCol, int toCol, out bool willMerge)
        {
            willMerge = false;
            if (inputLocked) return false;
            if (fromCol == toCol) return false;
            if (fromCol < 0 || fromCol >= stacks.Length) return false;
            if (toCol < 0 || toCol >= stacks.Length) return false;

            if (stacks[fromCol].childCount == 0) return false;

            int value = PeekTopValueFromChild0(fromCol);

            // 置き先の合法チェック（合体を許可）
            if (!CanPlaceAt(toCol, value, allowMergeOnSpawn: true)) return false;

            // to が空でなければ合体の可能性を返す
            if (stacks[toCol].childCount > 0)
            {
                int topTo = PeekTopValueFromChild0(toCol);
                willMerge = (ruleSet != null && ruleSet.allowEqualMerge && topTo == value);
            }
            return true;
        }

        /// <summary>
        /// fromCol のトップ1枚を toCol に移動して合体/連鎖も解決。成功時 true、加点は scoreGain に。
        /// </summary>
        public bool MoveTop(int fromCol, int toCol, bool animate, out int scoreGain)
        {
            scoreGain = 0;
            if (!CanMoveTop(fromCol, toCol, out _)) return false;

            // 1) 値を取得して元を削除
            int value = PopTopValueAndDestroyFromChild0(fromCol);

            // 2) 置き先で合体＆連鎖
            bool mergedAtLeastOnce = false;
            if (ruleSet != null && ruleSet.allowEqualMerge)
            {
                while (stacks[toCol].childCount > 0)
                {
                    int topVal = PeekTopValueFromChild0(toCol);
                    if (topVal != value) break;

                    // 先頭を外して破棄
                    var topRoot = stacks[toCol].GetChild(0);
                    topRoot.SetParent(null, false);
                    Destroy(topRoot.gameObject);

                    value *= 2;
                    scoreGain += value;
                    mergedAtLeastOnce = true;

                    if (!ruleSet.allowChainMerge) break;
                }
            }

            // 3) 最終値を toCol の先頭に生成
            var dst = stacks[toCol];
            var go = Instantiate(cardViewPrefab, dst);
            go.SetValue(value);
            go.transform.SetAsFirstSibling();

            var rt = go.RectTransform();
            if (animate && rt) StartCoroutine(PopIn(rt));

            // 4) 通知
            if (mergedAtLeastOnce)
            {
                OnMerged?.Invoke(value);
                if (rt) StartCoroutine(Pulse(rt));
            }
            else
            {
                OnPlacedNoMerge?.Invoke();
            }

            LastPlacedColumnIndex = toCol;

            // 5) トップだけ掴めるように更新（任意）
            RefreshTopDraggable();

            return true;
        }

        /// <summary>
        /// 各列の「最上段（child 0）」だけ Raycast を有効化。トップだけ掴める見た目用。
        /// </summary>
        public void RefreshTopDraggable()
        {
            for (int c = 0; c < stacks.Length; c++)
            {
                var st = stacks[c];
                int n = st.childCount;
                for (int i = 0; i < n; i++)
                {
                    var t = st.GetChild(i);
                    var cv = t.GetComponent<CardView>();
                    if (!cv || !cv.bg) continue;

                    bool isTop = (i == 0); // AddAtTop が SetAsFirstSibling なので child 0 が最上段

                    // Raycast はトップだけON（非トップはドラッグ不可）
                    cv.bg.raycastTarget = isTop;

                    // TopCardHandle の付与/有効化
                    var handle = t.GetComponent<TopCardHandle>();
                    if (isTop)
                    {
                        if (!handle) handle = t.gameObject.AddComponent<TopCardHandle>();
                        handle.enabled = true;
                        handle.columnIndex = c;
                        handle.cardView = cv;
                        handle.ghost = dragGhost; // ← ここが重要
                        handle.board = this;
                    }
                    else
                    {
                        if (handle) handle.enabled = false;
                    }
                }
            }
        }


        // ---- 追加ヘルパー ----

        int PeekTopValueFromChild0(int col)
        {
            var st = stacks[col];
            if (st.childCount == 0) return 0;
            return ReadValueFrom(st.GetChild(0));
        }

        int PopTopValueAndDestroyFromChild0(int col)
        {
            var st = stacks[col];
            var t = st.GetChild(0);
            int v = ReadValueFrom(t);
            t.SetParent(null, false);
            Destroy(t.gameObject);
            return v;
        }

        // CardView / TMP_Text どちらでも値を読めるように
        int ReadValueFrom(Transform cardRoot)
        {
            var cv = cardRoot.GetComponent<CardView>();
            if (cv != null) return cv.Value;

            var t = cardRoot.GetComponentInChildren<TMP_Text>();
            if (t != null && int.TryParse(t.text, out var v)) return v;

            return 0;
        }

        void SetCardValue(Transform cardRoot, int v)
        {
            var cv = cardRoot.GetComponent<CardView>();
            if (cv != null) { cv.SetValue(v); return; }
            var t = cardRoot.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = v.ToString();
        }
    }
}
