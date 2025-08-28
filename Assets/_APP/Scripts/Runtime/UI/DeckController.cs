using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Gameplay;

namespace App.UI
{
    /// <summary>
    /// �f�b�L�̃N���b�N��Waste��1���h���[�A�z�u/����������͎����Ŏ��h���[�B
    /// UI�A�j���iFlyCard�j�� DeckCount/WasteValue �̍X�V�������ŒS���B
    /// </summary>
    public class DeckController : MonoBehaviour
    {
        [Header("Scene Refs")]
        public RectTransform canvasRT;
        public RectTransform deckCardRT;
        public RectTransform wasteCardRT;
        public TMP_Text deckCountText;
        public TMP_Text wasteValueText;
        public Image flyCard;                    // Canvas������FlyCard�i�q��TMP������ΐ����\���j

        [Header("Links")]
        public DeckService deckService;          // �R�D���W�b�N
        public App.Gameplay.BoardPlacer placer;  // �z�u/�����C�x���g���w��
        public App.UI.UITapAutoSuggest autoSuggest; // �C�ӁF��[��ɒ�Ă𑖂点��

        [Header("Options")]
        public bool drawOnStart = true;          // �N�������1���߂���
        public bool autoDrawAfterPlacement = true;

        [Header("Input Lock")]
        public bool drawLocked = false;
        public void SetDrawLocked(bool v) => drawLocked = v;
        public bool IsDrawLocked => drawLocked;

        // DeckController.cs
        public void LockDraw()  => SetDrawLocked(true);
        public void UnlockDraw()=> SetDrawLocked(false);


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

        // === UI����ĂԁiDeckCard��Button OnClick�j ===
        public void OnDeckClicked()
        {
            if (drawLocked) return;
            if (_busy) return;
            if (!IsWasteEmpty()) { StartCoroutine(Shake(wasteCardRT)); return; }
            if (deckService && deckService.Remaining == 0) { StartCoroutine(Shake(deckCardRT)); return; }

            StartCoroutine(DrawWithAnim());
        }

        void OnPlaced()
        {
            if (!autoDrawAfterPlacement) return;
            if (_busy) return;
            if (!IsWasteEmpty()) return;           // ���ɒl���c���Ă���Ȃ牽�����Ȃ�
            if (deckService && deckService.Remaining == 0) return;

            // ������ƊԂ�u���Ă��玩���h���[
            StartCoroutine(DrawWithAnim(0.08f));
        }

        IEnumerator DrawWithAnim(float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (deckService == null) yield break;
            if (!deckService.TryDraw(out int value)) yield break;

            _busy = true;
            UpdateDeckLabel();

            // FlyCard���o
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

            // �h���[��Ɏ�����Ă��������
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
