using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Gameplay;

namespace App.UI
{
    /// <summary>
    /// デッキのクリック→Wasteに1枚ドロー、配置/合成成功後は自動で次ドロー。
    /// UIアニメ（FlyCard）と DeckCount/WasteValue の更新もここで担当。
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Header("Scene Refs")]
        public RectTransform canvasRT;
        public RectTransform deckCardRT;
        public RectTransform wasteCardRT;
        public TMP_Text deckCountText;
        public TMP_Text wasteValueText;
        public Image flyCard;                    // Canvas直下のFlyCard（子にTMPがあれば数字表示）

        [Header("Links")]
        public DeckService deckService;          // 山札ロジック
        public App.Gameplay.BoardPlacer placer;  // 配置/合成イベントを購読
        public App.UI.UITapAutoSuggest autoSuggest; // 任意：補充後に提案を走らせる

        [Header("Options")]
        public bool drawOnStart = true;          // 起動直後に1枚めくる
        public bool autoDrawAfterPlacement = true;

        bool _busy;

        void Awake()
        {
            if (flyCard) flyCard.gameObject.SetActive(false);
        }

        void Start()
        {
            UpdateDeckLabel();
            if (drawOnStart && IsWasteEmpty()) StartCoroutine(DrawWithAnim());
        }

        void OnEnable()
        {
            if (placer != null)
            {
                placer.OnMerged += _ => OnPlaced();
                placer.OnPlacedNoMerge += OnPlaced;
            }
        }
        void OnDisable()
        {
            if (placer != null)
            {
                placer.OnMerged -= _ => OnPlaced();
                placer.OnPlacedNoMerge -= OnPlaced;
            }
        }

        // === UIから呼ぶ（DeckCardのButton OnClick） ===
        public void OnDeckClicked()
        {
            if (_busy) return;
            if (!IsWasteEmpty()) { StartCoroutine(Shake(wasteCardRT)); return; }
            if (deckService && deckService.Remaining == 0) { StartCoroutine(Shake(deckCardRT)); return; }

            StartCoroutine(DrawWithAnim());
        }

        void OnPlaced()
        {
            if (!autoDrawAfterPlacement) return;
            if (_busy) return;
            if (!IsWasteEmpty()) return;           // 既に値が残っているなら何もしない
            if (deckService && deckService.Remaining == 0) return;

            // ちょっと間を置いてから自動ドロー
            StartCoroutine(DrawWithAnim(0.08f));
        }

        IEnumerator DrawWithAnim(float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (deckService == null) yield break;
            if (!deckService.TryDraw(out int value)) yield break;

            _busy = true;
            UpdateDeckLabel();

            // FlyCard演出
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
            else
            {
                if (wasteValueText) wasteValueText.text = value.ToString();
            }

            _busy = false;

            // ドロー後に自動提案したければ
            if (autoSuggest) autoSuggest.OnWasteTapped();
        }

        bool IsWasteEmpty() => !wasteValueText || string.IsNullOrEmpty(wasteValueText.text);

        void UpdateDeckLabel()
        {
            if (deckCountText && deckService) deckCountText.text = deckService.Remaining.ToString();
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
        IEnumerator Shake(RectTransform rt)
        {
            if (!rt) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f, d = 0.16f;
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
