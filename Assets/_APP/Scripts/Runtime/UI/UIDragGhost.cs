using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App.UI
{
    // WasteCardにアタッチ：ドラッグでゴーストを表示
    public class UIDragGhost : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Refs")]
        public RectTransform canvasRT;        // Canvas(RectTransform)
        public RectTransform dragLayer;       // DragLayer
        public GameObject cardViewPrefab;     // CardView.prefab
        public TMP_Text wasteValue;           // WasteValue(TMP)
        public List<RectTransform> columns;   // ColumnのRectTransform(ルート)
        public List<Image> dropHighlights;    // 各Column内のDropHighlight(Image)
        public float ghostAlpha = 0.85f;

        RectTransform _ghostRT;
        CanvasGroup _ghostCg;
        int _hoverIndex = -1;

        public void OnBeginDrag(PointerEventData e)
        {
            // ゴースト生成
            var go = Instantiate(cardViewPrefab, dragLayer);
            _ghostRT = go.GetComponent<RectTransform>();
            _ghostRT.anchorMin = _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);
            _ghostRT.pivot = new Vector2(0.5f, 0.5f);
            _ghostRT.sizeDelta = new Vector2(100, 140);
            _ghostCg = go.AddComponent<CanvasGroup>();
            _ghostCg.alpha = ghostAlpha;
            // 数字表示（CardViewがあればSetValue、無ければTMPに直書き）
            var cv = go.GetComponentInChildren<TMP_Text>();
            if (cv) cv.text = wasteValue ? wasteValue.text : "16";
            UpdateGhostPosition(e);
        }

        public void OnDrag(PointerEventData e)
        {
            UpdateGhostPosition(e);
            // どのColumn上にいるか簡易判定（RectTransformUtilityで当たり）
            int newIndex = -1;
            for (int i = 0; i < columns.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(columns[i], e.position, e.pressEventCamera))
                {
                    newIndex = i; break;
                }
            }
            if (newIndex != _hoverIndex)
            {
                SetHighlight(_hoverIndex, false);
                _hoverIndex = newIndex;
                SetHighlight(_hoverIndex, true);
            }
        }

        public void OnEndDrag(PointerEventData e)
        {
            // ハイライト解除
            SetHighlight(_hoverIndex, false);
            // ゴーストを元位置へ戻して消す（デモ）
            if (_ghostRT) StartCoroutine(FlyBackAndKill());
        }

        void UpdateGhostPosition(PointerEventData e)
        {
            if (!_ghostRT || !canvasRT) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, e.position, e.pressEventCamera, out local);
            _ghostRT.anchoredPosition = local;
        }

        void SetHighlight(int idx, bool on)
        {
            if (idx < 0 || idx >= dropHighlights.Count) return;
            if (dropHighlights[idx]) dropHighlights[idx].gameObject.SetActive(on);
        }

        IEnumerator FlyBackAndKill()
        {
            Vector3 a = _ghostRT.anchoredPosition;
            Vector3 b = Vector3.zero; // 画面中央に戻す例。WasteCardの位置に戻したい場合はその座標に置き換え
            float t = 0, d = 0.12f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float u = t / d;
                _ghostRT.anchoredPosition = Vector3.Lerp(a, b, u);
                if (_ghostCg) _ghostCg.alpha = Mathf.Lerp(ghostAlpha, 0f, u);
                yield return null;
            }
            Destroy(_ghostRT.gameObject);
            _ghostRT = null;
        }
    }
}
