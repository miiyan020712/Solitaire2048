using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using App.Gameplay; // 追加

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
        

        [Header("Rules")]
        public RuleSet ruleSet;
        public RectTransform[] stackArrayForState;  // Column_x/Stack を左→右の順で
       

        [Header("Highlight Colors")]
        public Color okColor = new Color(0f, 1f, 1f, 0.14f); // シアンっぽい
        public Color ngColor = new Color(1f, 0f, 0f, 0.16f); // 赤

        [Header("Placer")]
        public BoardPlacer boardPlacer;   // ここに BoardPlacer をドラッグ
        public UIFlash wasteFlash;        // NG時の赤フラッシュ（WasteFlash）※任意

        int _hoverIndex = -1;
        bool _hoverLegal = false;

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

    int newIndex = -1;
    bool newLegal = false;

    for (int i = 0; i < columns.Count; i++)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(columns[i], e.position, e.pressEventCamera))
        {
            newIndex = i;

            // ここで合法判定
            if (ruleSet && stackArrayForState != null && stackArrayForState.Length == columns.Count)
            {
                var state = new App.Gameplay.BoardState(stackArrayForState);
                int v = 0; int.TryParse(wasteValue ? wasteValue.text : "0", out v);
                newLegal = App.Gameplay.MoveValidator.CanPlace(i, v, state, ruleSet, out _);
            }
            else
            {
                newLegal = true; // 参照未設定時はとりあえずOK扱い
            }
            break;
        }
    }

    if (newIndex != _hoverIndex || newLegal != _hoverLegal)
    {
        // 旧ハイライトを消す
        SetHighlight(_hoverIndex, false, _hoverLegal);

        _hoverIndex = newIndex;
        _hoverLegal = newLegal;

        // 新ハイライトを色付きで点灯
        SetHighlight(_hoverIndex, true, _hoverLegal);
    }
}

void SetHighlight(int idx, bool on, bool legal)
{
    if (idx < 0 || idx >= dropHighlights.Count) return;
    var img = dropHighlights[idx];
    if (!img) return;
    img.color = legal ? okColor : ngColor;
    img.gameObject.SetActive(on);
}


        public void OnEndDrag(PointerEventData e)
{
    // 旧ハイライト消灯
    SetHighlight(_hoverIndex, false, _hoverLegal);

    // 合法なら置く、NGならWasteを赤フラ
    if (_hoverIndex >= 0)
    {
        bool placed = false;
        if (boardPlacer)
            placed = boardPlacer.TryPlaceAt(_hoverIndex);

        if (!placed && wasteFlash)
            wasteFlash.Play(); // NGフィードバック
    }

    // ゴーストは消す
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
