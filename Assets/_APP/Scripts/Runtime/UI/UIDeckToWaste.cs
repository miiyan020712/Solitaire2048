using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public class UIDeckToWaste : MonoBehaviour
    {
        [Header("Scene Refs")]
        public RectTransform canvasRT;     // Canvas
        public RectTransform deckCardRT;   // BottomBar/DeckView/DeckCard
        public RectTransform wasteCardRT;  // BottomBar/WasteView/WasteCard
        public TMP_Text deckCountText;     // BottomBar/DeckView/DeckCount
        public TMP_Text wasteValueText;    // BottomBar/WasteView/WasteValue
        public Image flyCard;              // Canvas/FlyCard（子にTMPがあるなら数字表示する）

        [Header("Demo Values")]
        public List<int> demoSequence = new List<int> { 2,2,4,4,8,8,16,16,32,64,128,256 };
        public bool loopSequence = true;
        public int remaining = 42;

        [Header("Link (optional)")]
        public UITapAutoSuggest autoSuggest; // 補充後に自動プレビューしたい場合に割り当て

        int _seqIndex = 0;
        bool _busy;

        void Start()
        {
            if (flyCard) flyCard.gameObject.SetActive(false);
            UpdateDeckCountLabel();
        }

        public void OnDeckClicked()
        {
            if (_busy) return;
            if (remaining <= 0) { StartCoroutine(Shake(deckCardRT)); return; }

            int value = NextValue();
            remaining = Mathf.Max(0, remaining - 1);
            UpdateDeckCountLabel();

            StartCoroutine(ShowFromDeckToWaste(value));
        }

        int NextValue()
        {
            if (demoSequence == null || demoSequence.Count == 0) return 2;
            int v = demoSequence[_seqIndex];
            _seqIndex++;
            if (loopSequence) _seqIndex %= demoSequence.Count;
            else _seqIndex = Mathf.Min(_seqIndex, demoSequence.Count - 1);
            return v;
        }

        IEnumerator ShowFromDeckToWaste(int value)
        {
            _busy = true;

            // FlyCard セット
            if (flyCard && canvasRT && deckCardRT && wasteCardRT)
            {
                var t = flyCard.GetComponentInChildren<TMP_Text>();
                if (t) t.text = value.ToString();

                flyCard.gameObject.SetActive(true);

                Vector2 a = WorldToCanvas(deckCardRT);
                Vector2 b = WorldToCanvas(wasteCardRT);

                yield return Move(flyCard.rectTransform, a, b, 0.18f);
                if (wasteValueText) wasteValueText.text = value.ToString();
                yield return new WaitForSeconds(0.02f);
                flyCard.gameObject.SetActive(false);
            }

            _busy = false;

            // 補充後に自動で“提案プレビュー”したければ
            if (autoSuggest) autoSuggest.OnWasteTapped();
        }

        Vector2 WorldToCanvas(RectTransform rt)
        {
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, sp, null, out var local);
            return local;
        }

        IEnumerator Move(RectTransform rt, Vector2 a, Vector2 b, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                rt.anchoredPosition = Vector2.LerpUnclamped(a, b, Mathf.SmoothStep(0, 1, u));
                yield return null;
            }
            rt.anchoredPosition = b;
        }

        void UpdateDeckCountLabel() { if (deckCountText) deckCountText.text = remaining.ToString(); }

        IEnumerator Shake(RectTransform rt)
        {
            if (!rt) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f, d = 0.15f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Sin(t * 40f) * (1f - t / d) * 5f;
                rt.anchoredPosition = basePos + new Vector2(s, 0);
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }
    }
}
