using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;                       // ← 追加（VerticalLayoutGroup 参照/並び制御）
using CardView = App.UI.Themeing.CardView; // カード見た目
using App.Gameplay;

namespace App.Gameplay
{
    /// <summary>
    /// ボードへの配置＆合成を担当。
    /// - placeAtBottom=true：各列の「最下段」にプレースして下から迎撃
    /// - placeAtBottom=false：従来の“最上段”にプレースして上から合成
    /// - OnMerged / OnPlacedNoMerge を発火（ScoreSystem などが購読）
    /// - Reverse Arrangement の ON/OFF を自動吸収（見た目の「手前」を常に取得）
    /// </summary>
    public class BoardPlacer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("各列の Stack（左→右）")]
        public RectTransform[] stacks;

        [Tooltip("CardView のプレハブ")]
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
        [SerializeField] private int visualRowCapacity = 10;  // 画面に実際に入る段数（RowSpawner が参照）
        public int VisualRowCapacity => visualRowCapacity;

        [Header("Lock")] 
        public bool inputLocked = false;
        public void SetLocked(bool v) => inputLocked = v;

        /// <summary>合成時に発火（合成後の最終値。例: 2+2→4 なら 4）</summary>
        public event System.Action<int> OnMerged;
        /// <summary>合成無しで新規配置したときに発火</summary>
        public event System.Action OnPlacedNoMerge;

        /// <summary>直近に配置を試みた列（演出側が参照したい場合に使用）</summary>
        public int LastPlacedColumnIndex { get; private set; } = -1;

        // ======================================================
        // Public API（デッキからの配置）
        // ======================================================

        /// <summary>
        /// 現在の Waste の値を、指定列に「置く or 合成」する。
        /// placeAtBottom=true のときは最下段迎撃、false のときは従来の最上段。
        /// </summary>
        public bool TryPlaceAt(int columnIndex)
        {
            if (inputLocked) return false;
            if (!IsReady() || columnIndex < 0 || columnIndex >= stacks.Length)
                return false;

            if (!int.TryParse(wasteValue ? wasteValue.text : null, out int value))
                return false;

            LastPlacedColumnIndex = columnIndex;

            if (placeAtBottom)
                return TryPlaceFromBottom(columnIndex, value);
            else
                return TryPlaceFromTop(columnIndex, value);
        }

        /// <summary>すべての Stack を空にする（エディタ・デバッグ用）。</summary>
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
            if (inputLocked) return false;

            var st = stacks[columnIndex];
            if (!st) return false;

            int merged = value;
            bool mergedAtLeastOnce = false;

            // 既存最下段から連続する同値を吸収
            while (true)
            {
                var bRT = GetBottomRT(columnIndex);
                if (!bRT) break;

                int bVal = ReadValue(bRT);
                if (bVal == merged)
                {
                    // 列から外して破棄（Destroy遅延対策）
                    bRT.SetParent(null, false);
                    Destroy(bRT.gameObject);

                    merged *= 2;
                    mergedAtLeastOnce = true;
                }
                else break;
            }

            // 最終値を最下段に新規挿入
            var go = Instantiate(cardViewPrefab).GetComponent<RectTransform>();
            WriteValue(go, merged);
            PlaceAsBottom(go, st);                 // ← 並び順に応じた「最下段」へ
            StartCoroutine(Pop(go));

            ClearWaste();
            if (mergedAtLeastOnce)
            {
                OnMerged?.Invoke(merged);
                StartCoroutine(Pulse(GetTopRT(columnIndex) ?? go)); // 演出的にトップ/今入れた方へ
            }
            else
            {
                OnPlacedNoMerge?.Invoke();
            }

            return true;
        }

        // ======================================================
        // Top placement（従来の最上段プレース）
        // ======================================================

        /// <summary>
        /// 従来の“最上段”に置くモード。MoveValidator に従って合法判定→同値連鎖（上から）。
        /// </summary>
        bool TryPlaceFromTop(int columnIndex, int value)
        {
            if (inputLocked) return false;

            // 合法判定（BoardState/MoveValidator を利用）
            var state = new BoardState(stacks);
            if (!MoveValidator.CanPlace(columnIndex, value, state, ruleSet, out _))
                return false;

            // 1) 最上段と同値ならその場で合成（連鎖あり）
            if (ruleSet != null && ruleSet.allowEqualMerge)
            {
                var topRT = GetTopRT(columnIndex);
                if (topRT && ReadValue(topRT) == value)
                {
                    int merged = value * 2;
                    WriteValue(topRT, merged);
                    StartCoroutine(Pulse(topRT));

                    if (ruleSet.allowChainMerge)
                    {
                        // その下（見た目で2番手）と連鎖
                        while (true)
                        {
                            var belowRT = GetSecondTopRT(columnIndex);
                            if (!belowRT) break;

                            int bVal = ReadValue(belowRT);
                            if (bVal == merged)
                            {
                                belowRT.SetParent(null, false);
                                Destroy(belowRT.gameObject);

                                merged *= 2;
                                WriteValue(topRT, merged);
                                StartCoroutine(Pulse(topRT));
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
            var go = Instantiate(cardViewPrefab).GetComponent<RectTransform>();
            WriteValue(go, value);
            PlaceAsTop(go, stack);
            StartCoroutine(Pop(go));

            ClearWaste();
            OnPlacedNoMerge?.Invoke();
            return true;
        }

        // ======================================================
        // 列トップ移動（列→列ドラッグ用）
        // ======================================================

public bool MoveTop(int fromCol, int toCol, bool animate, out bool merged)
{
    merged = false;
    if (fromCol == toCol) return false;

    var from = stacks[fromCol];
    var to   = stacks[toCol];
    if (!from || !to || from.childCount == 0) return false;

    // 移動元トップ（見た目の上端は index 0）
    var topRoot = from.GetChild(0) as RectTransform;
    var topCV   = topRoot.GetComponent<CardView>();
    int value   = topCV ? topCV.Value : 0;

    // まず元から外す（Destroy 遅延の影響を避ける）
    topRoot.SetParent(null, worldPositionStays: false);

    // 合体判定：移動先のトップが同値なら吸収 → 連鎖
    bool didMerge = false;
    int outValue = value;

    if (ruleSet != null && ruleSet.allowEqualMerge && to.childCount > 0)
    {
        var dstTopRoot = to.GetChild(0);
        var dstTopCV   = dstTopRoot.GetComponent<CardView>();
        if (dstTopCV && dstTopCV.Value == outValue)
        {
            // 先に先頭を外して破棄
            dstTopRoot.SetParent(null, false);
            Destroy(dstTopRoot.gameObject);

            outValue *= 2;
            didMerge = true;

            if (ruleSet.allowChainMerge)
            {
                while (to.childCount > 0)
                {
                    var nextRoot = to.GetChild(0);
                    var nextCV   = nextRoot.GetComponent<CardView>();
                    if (nextCV && nextCV.Value == outValue)
                    {
                        nextRoot.SetParent(null, false);
                        Destroy(nextRoot.gameObject);
                        outValue *= 2;
                    }
                    else break;
                }
            }
        }
    }

    // 最終的なカードを作る/更新して「見た目の先頭」に差し込む
    var cv = topCV ? topCV : topRoot.GetComponent<CardView>();
    if (!cv) cv = topRoot.gameObject.AddComponent<CardView>();
    cv.SetValue(outValue);

    topRoot.SetParent(to, false);
    topRoot.SetAsFirstSibling();                 // ★ 先頭（見た目の上）に来るよう固定
    if (animate) StartCoroutine(Pop(topRoot));   // 軽いポップ

    // ドラッグハンドルの列番号を更新（付いている場合）
    var handle = topRoot.GetComponentInChildren<TopCardHandle>(true);
    if (handle)
    {
        handle.columnIndex = toCol;
        handle.board       = this;
        if (!handle.cardView) handle.cardView = cv;
    }

    ClearWaste(); // （好み）移動でも Waste を空にしたい場合

    if (didMerge)
    {
        merged = true;
        OnMerged?.Invoke(outValue);
        StartCoroutine(Pulse(topRoot));
    }
    else
    {
        OnPlacedNoMerge?.Invoke();
    }
    return true;
}

// 移動の合法判定（列トップ → 別列）
public bool CanMoveTop(int fromCol, int toCol, out bool willMerge)
{
    willMerge = false;
    if (fromCol == toCol) return false;

    var from = stacks[fromCol];
    var to   = stacks[toCol];
    if (!from || from.childCount == 0) return false;

    // 移動元トップ値（index 0）
    var srcCV = from.GetChild(0).GetComponent<CardView>();
    if (!srcCV) return false;
    int v = srcCV.Value;

    if (!to || to.childCount == 0) return true; // 空列OK

    var dstCV = to.GetChild(0).GetComponent<CardView>();
    if (!dstCV) return true;

    if (ruleSet != null && ruleSet.allowEqualMerge && v == dstCV.Value)
    {
        willMerge = true;
        return true;
    }
    if (ruleSet != null && ruleSet.allowDescending && v > dstCV.Value)
        return false; // 降順違反

    return (v <= dstCV.Value);
}

        // ======================================================
        // RowSpawner からの行生成で使用
        // ======================================================

        /// <summary>段生成用：列の最上段にカードを追加（基本合成しない）</summary>
        public void AddAtTop(int col, int value, bool animate = true, bool allowMergeOnSpawn = false)
        {
            var stack = stacks[col];
            var cardRT = Instantiate(cardViewPrefab).GetComponent<RectTransform>();
            WriteValue(cardRT, value);

            PlaceAsTop(cardRT, stack);                 // 並び順に応じて自動調整
            if (animate) StartCoroutine(PopIn(cardRT));
            LayoutRebuilder.ForceRebuildLayoutImmediate(stack); // ★ 追加

            if (allowMergeOnSpawn)
            {
                var top = GetTopRT(col);
                var below = GetSecondTopRT(col);
                if (top && below && ReadValue(top) == ReadValue(below))
                {
                    int merged = ReadValue(top) * 2;
                    below.SetParent(null, false);
                    Destroy(below.gameObject);
                    WriteValue(top, merged);
                    StartCoroutine(Pulse(top));

                    if (ruleSet != null && ruleSet.allowChainMerge)
                    {
                        while (true)
                        {
                            var b2 = GetSecondTopRT(col);
                            if (!b2) break;
                            int bv = ReadValue(b2);
                            if (bv == merged)
                            {
                                b2.SetParent(null, false);
                                Destroy(b2.gameObject);
                                merged *= 2;
                                WriteValue(top, merged);
                                StartCoroutine(Pulse(top));
                            }
                            else break;
                        }
                    }

                    OnMerged?.Invoke(merged);
                }
            }
        }

        /// <summary>その列に Waste を置けるか（合体許可=allowMergeOnSpawn）</summary>
        public bool CanPlaceAt(int col, int value, bool allowMergeOnSpawn)
        {
            var s = stacks[col];
            if (!s || s.childCount == 0) return true;

            var top = GetTopRT(col);
            int topVal = ReadValue(top);

            if (!allowMergeOnSpawn && ruleSet != null && ruleSet.allowEqualMerge && value == topVal)
                return false; // 同値合成禁止ならNG

            if (ruleSet != null && ruleSet.allowDescending && value > topVal) 
                return false; // 降順ルール

            return (value <= topVal);
        }

        // ======================================================
        // 情報取得（RowSpawner/判定補助）
        // ======================================================

        public int ColumnCount => stacks != null ? stacks.Length : 0;

        public int GetStackCount(int col) => stacks[col].childCount;

        public int GetChildCount(int col) => GetStackCount(col);
        public int GetHeight(int col) => GetStackCount(col);

        /// <summary>その列の「最上段（見た目の先頭）」の値</summary>
public int GetTopValue(int col)
{
    var s = stacks[col];
    if (!s || s.childCount == 0) return int.MaxValue; // 空列は何でも載る扱い
    var top = s.GetChild(0).GetComponent<CardView>();
    return top ? top.Value : int.MaxValue;
}

        /// <summary>高さが threshold 以上の列があるか</summary>
        public bool AnyColumnAtOrAbove(int threshold)
        {
            if (stacks == null) return false;
            for (int i = 0; i < stacks.Length; i++)
                if (stacks[i] && stacks[i].childCount >= threshold) return true;
            return false;
        }

        // ======================================================
        // Helpers（並び順/値の読み書き/テキスト互換）
        // ======================================================

        bool IsReverse(RectTransform st)
        {
            var vg = st ? st.GetComponent<VerticalLayoutGroup>() : null;
            return vg != null && vg.reverseArrangement;
        }

        int TopIndex(RectTransform st)
        {
            if (!st || st.childCount == 0) return 0;
            return IsReverse(st) ? st.childCount - 1 : 0;
        }

        int SecondTopIndex(RectTransform st)
        {
            if (!st || st.childCount < 2) return -1;
            return IsReverse(st) ? st.childCount - 2 : 1;
        }

        int BottomIndex(RectTransform st)
        {
            if (!st || st.childCount == 0) return 0;
            return IsReverse(st) ? 0 : st.childCount - 1;
        }

        RectTransform GetTopRT(int col)
        {
            var st = stacks[col];
            if (!st || st.childCount == 0) return null;
            return st.GetChild(TopIndex(st)) as RectTransform;
        }

        RectTransform GetSecondTopRT(int col)
        {
            var st = stacks[col];
            int idx = SecondTopIndex(st);
            if (idx < 0) return null;
            return st.GetChild(idx) as RectTransform;
        }

        RectTransform GetBottomRT(int col)
        {
            var st = stacks[col];
            if (!st || st.childCount == 0) return null;
            return st.GetChild(BottomIndex(st)) as RectTransform;
        }

        void PlaceAsTop(RectTransform child, RectTransform parent)
        {
            child.SetParent(parent, false);
            if (IsReverse(parent)) child.SetAsLastSibling();
            else child.SetAsFirstSibling();
        }

        void PlaceAsBottom(RectTransform child, RectTransform parent)
        {
            child.SetParent(parent, false);
            if (IsReverse(parent)) child.SetAsFirstSibling();
            else child.SetAsLastSibling();
        }

        int ReadValue(RectTransform rt)
        {
            // CardView 優先
            var cv = rt ? rt.GetComponent<CardView>() : null;
            if (cv != null)
            {
                // CardView には Value プロパティがある前提
                var valField = typeof(CardView).GetField("value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (valField != null) return (int)valField.GetValue(cv); // 既存実装に合わせる
            }

            // フォールバック：子の TMP_Text
            var txt = rt ? rt.GetComponentInChildren<TMP_Text>() : null;
            if (txt && int.TryParse(txt.text, out int v)) return v;

            return 0;
        }

        void WriteValue(RectTransform rt, int v)
        {
            // CardView 優先
            var cv = rt ? rt.GetComponent<CardView>() : null;
            if (cv != null)
            {
                var m = typeof(CardView).GetMethod("SetValue", new[] { typeof(int) });
                if (m != null) { m.Invoke(cv, new object[] { v }); return; }
            }

            // フォールバック：子の TMP_Text
            var txt = rt ? rt.GetComponentInChildren<TMP_Text>() : null;
            if (txt) txt.text = v.ToString();
        }

        TMP_Text TopText(int col)
        {
            var rt = GetTopRT(col);
            return rt ? rt.GetComponentInChildren<TMP_Text>() : null;
        }

        TMP_Text BelowTopText(int col)
        {
            var rt = GetSecondTopRT(col);
            return rt ? rt.GetComponentInChildren<TMP_Text>() : null;
        }

        TMP_Text BottomText(int col)
        {
            var rt = GetBottomRT(col);
            return rt ? rt.GetComponentInChildren<TMP_Text>() : null;
        }

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

        // RowSpawner 互換の軽量 PopIn
        IEnumerator PopIn(RectTransform rt) => Pop(rt);
    }
}
