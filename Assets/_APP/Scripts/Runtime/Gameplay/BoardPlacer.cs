using System.Collections;
using TMPro;
using UnityEngine;

namespace App.Gameplay
{
    public class BoardPlacer : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform[] stacks;      // Column_x/Stack（左→右）
        public GameObject cardViewPrefab;   // CardView.prefab
        public TMP_Text wasteValue;         // Wasteの数値
        public RuleSet ruleSet;

        [Header("Anim")]
        public float popIn = 0.12f;
        public float pulse = 0.12f;

        // --- 追加ヘルパ ---
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

        // --- ここがメイン：置く or 合成 ---
        public bool TryPlaceAt(int columnIndex)
        {
            if (!wasteValue || string.IsNullOrEmpty(wasteValue.text)) return false;
            if (!int.TryParse(wasteValue.text, out int value)) return false;

            var state = new BoardState(stacks);
            if (!MoveValidator.CanPlace(columnIndex, value, state, ruleSet, out _))
                return false;

            // 1) 置く前に「同値合成」判定（単発 or 連鎖）
            if (ruleSet.allowEqualMerge)
            {
                var top = TopText(columnIndex);
                if (top && int.TryParse(top.text, out int topVal) && topVal == value)
                {
                    // 最上段を倍にする（新しいカードは作らない）
                    int newVal = value * 2;
                    top.text = newVal.ToString();
                    StartCoroutine(Pulse(top.rectTransform));

                    if (ruleSet.allowChainMerge)
                    {
                        // 下に同値が続く限り、最上段を育てつつ下段を消す
                        while (true)
                        {
                            var below = BelowTopText(columnIndex);
                            if (!below) break;

                            if (int.TryParse(below.text, out int bVal) && bVal == newVal)
                            {
                                // 下段を破棄、最上段をさらに倍
                                Destroy(below.transform.parent.gameObject);
                                newVal *= 2;
                                top.text = newVal.ToString();
                                StartCoroutine(Pulse(top.rectTransform));
                            }
                            else break;
                        }
                    }

                    // Waste を空に（補充は後の工程で）
                    wasteValue.text = "";
                    return true;
                }
            }

            // 2) 合成じゃない場合は新規に1枚生成して積む
            var stack = stacks[columnIndex];
            var go = Instantiate(cardViewPrefab, stack);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp) tmp.text = value.ToString();

            var rt = go.GetComponent<RectTransform>();
            if (rt) StartCoroutine(Pop(rt));

            wasteValue.text = ""; // 後でデッキから補充
            return true;
        }

        IEnumerator Pop(RectTransform rt)
        {
            Vector3 a = Vector3.one * 0.2f, b = Vector3.one;
            float t = 0f;
            while (t < popIn)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t / popIn));
                yield return null;
            }
            rt.localScale = b;
        }

        IEnumerator Pulse(RectTransform rt)
        {
            Vector3 a = Vector3.one * 0.9f, b = Vector3.one;
            float t = 0f;
            while (t < pulse)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Sin(t / pulse * Mathf.PI); // 0→1→0
                rt.localScale = Vector3.Lerp(b, a, u * 0.25f);
                yield return null;
            }
            rt.localScale = b;
        }
    }
}
