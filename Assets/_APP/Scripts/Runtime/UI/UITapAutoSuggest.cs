using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public class UITapAutoSuggest : MonoBehaviour
    {
        [Header("Scene Refs")]
        public RectTransform canvasRT;       // Canvas (RectTransform)
        public RectTransform wasteCardRT;    // BottomBar/WasteView/WasteCard
        public TMP_Text     wasteValue;      // BottomBar/WasteView/WasteValue
        public Image        flyCard;         // Canvas/FlyCard (子にTMPがあるなら文字も入れる)

        [Tooltip("各列のStack(縦並びの親)を左→右の順に")]
        public List<RectTransform> stacks = new List<RectTransform>();

        [Tooltip("各列のDropHighlight(Image)を同じ順番で")]
        public List<Image> highlights = new List<Image>();

        [Header("Anim")]
        public float previewTravel = 0.18f;
        public float previewBack   = 0.15f;
        public float showHighlightSec = 0.35f;
        public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);

        bool busy;

        // ==== 公開API：Waste をタップ時に呼ぶ ====
        public void OnWasteTapped()
        {
            if (busy) return;
            int waste = ParseInt(wasteValue?.text);
            int target = PickColumnIndex(waste);
            StartCoroutine(PreviewTo(target, waste));
        }

        // ---- 疑似ロジック：良さそうな列を1つ選ぶ（あとで本実装に差し替える）----
        int PickColumnIndex(int waste)
{
    int n = stacks.Count;
    int center = n / 2;

    int best = -1;
    float bestScore = float.NegativeInfinity;

    for (int i = 0; i < n; i++)
    {
        var st = stacks[i];
        int count = st ? st.childCount : 0;
        int top = ReadTopValue(st); // 0 = 空扱い

        float score = 0f;

        // 1) 空列評価（空列は2以外は置けない想定なので弱め/強めを選べる）
        if (count == 0)
            score += (waste == 2) ? 200f : -30f;

        // 2) 合成（同値）を最優先
        if (top == waste && count > 0) score += 150f;

        // 3) 降順“合法”に近いほど高評価（近いほど＋大）
        if (top > waste) score += 80f - Mathf.Log(Mathf.Max(1, top - waste), 2f) * 10f;

        // 4) 逆順（top < waste）は減点
        if (top > 0 && top < waste) score -= 40f;

        // 5) 低い列を少し優遇
        score -= count * 0.6f;

        // 6) 中央寄せ（端に寄りすぎない）
        score -= Mathf.Abs(i - center) * 2.5f;

        // 7) 同点ブレイク用の微小乱数
        score += Random.value * 0.01f;

        if (score > bestScore) { bestScore = score; best = i; }
    }

    return best; // -1 なら見つからず
}


        int ReadTopValue(RectTransform stack)
        {
            if (!stack || stack.childCount == 0) return 0;
            var last = stack.GetChild(stack.childCount - 1);
            var tmp = last.GetComponentInChildren<TMP_Text>();
            if (tmp && int.TryParse(tmp.text, out int v)) return v;
            return 0;
        }

        IEnumerator PreviewTo(int idx, int value)
        {
            busy = true;
            // まず全消灯
            for (int i = 0; i < highlights.Count; i++)
                if (highlights[i]) highlights[i].gameObject.SetActive(false);

            if (idx < 0 || idx >= stacks.Count) {
                // 置けない時の簡易フィードバック（赤点滅＋揺れ）
                yield return StartCoroutine(ShakeWaste());
                busy = false; yield break;
            }

            // ハイライト点灯
            if (highlights[idx]) {
                highlights[idx].gameObject.SetActive(true);
                StartCoroutine(AutoOffHighlight(highlights[idx], showHighlightSec));
            }

            // FlyCard をWaste位置→対象列へ往復
            if (flyCard && canvasRT && wasteCardRT)
            {
                // 文字を入れたい場合（FlyCardの子にTMPがある前提）
                var t = flyCard.GetComponentInChildren<TMP_Text>();
                if (t) t.text = value.ToString();

                flyCard.gameObject.SetActive(true);

                Vector2 start = WorldToCanvas(wasteCardRT);
                Vector2 goal  = TargetTopCenter(stacks[idx]);  // 列の上辺 ちょい下を目標に

                yield return MoveUI(flyCard.rectTransform, start, goal, previewTravel);
                yield return new WaitForSeconds(0.04f);
                yield return MoveUI(flyCard.rectTransform, goal, start, previewBack);

                flyCard.gameObject.SetActive(false);
            }

            busy = false;
        }

        // 列の「上中央 少し下」を求める
        Vector2 TargetTopCenter(RectTransform col)
        {
            // DropHighlight の上辺中心-少しオフセット、が視覚的に分かりやすい
            RectTransform rt = col;
            if (!rt) return Vector2.zero;
            var wh = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners); // 0:BL 1:TL 2:TR 3:BR
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            Vector3 offset = new Vector3(0, -70f, 0);    // カード高さの半分くらい下
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, topCenter + offset);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, null, out var local);
            return local;
        }

        Vector2 WorldToCanvas(RectTransform rt)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, null, out var local);
            return local;
        }

        IEnumerator MoveUI(RectTransform rt, Vector2 a, Vector2 b, float d)
        {
            float t = 0f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / d);
                rt.anchoredPosition = Vector2.LerpUnclamped(a, b, ease.Evaluate(u));
                yield return null;
            }
            rt.anchoredPosition = b;
        }

        IEnumerator AutoOffHighlight(Image img, float sec)
        {
            yield return new WaitForSeconds(sec);
            if (img) img.gameObject.SetActive(false);
        }

        IEnumerator ShakeWaste()
        {
            var rt = wasteCardRT;
            if (!rt) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f, dur = 0.18f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Sin(t * 40f) * (1f - t/dur) * 6f; // 減衰する横揺れ
                rt.anchoredPosition = basePos + new Vector2(s, 0);
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }

        int ParseInt(string s) { return int.TryParse(s, out var v) ? v : 0; }
    }
}
