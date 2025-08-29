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
        public List<RectTransform> columns;   // ColumnのRectTransform(ルート)（左→右）
        public List<Image> dropHighlights;    // 各Column内のDropHighlight(Image)（左→右）
        public float ghostAlpha = 0.85f;

        public void SetEnabled(bool on) => enabled = on;

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

        [Header("Waste Drag Only")]
        public RectTransform wasteCardHitArea; // ← BottomBar/WasteView/WasteCard をドラッグ

        // 追加：UIカメラ管理
        Canvas _canvas;
        Camera _eventCam;

        int _hoverIndex = -1;
        bool _hoverLegal = false;

        // 追加：ドラッグ元の種別管理
        enum DragSource { None, Waste, Column }
        DragSource _source = DragSource.None;
        int _fromCol = -1;
        int _fromValue = 0;

        void Awake()
        {
            _canvas = canvasRT ? canvasRT.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        }

        Camera GetUICamera()
        {
            if (_canvas == null) return null;
            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

        // =========================================================
        // Waste からの標準ドラッグ（既存）
        // =========================================================
        public void OnBeginDrag(PointerEventData e)
        {
            // 列トップドラッグ中は Waste のドラッグ開始を無視
            if (_source == DragSource.Column) return;

            // ★ イベントカメラを確定
            _eventCam = e?.pressEventCamera ?? GetUICamera();

            // Waste カード上で始まっていなければ無視
            if (wasteCardHitArea != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(
                    wasteCardHitArea, e.position, _eventCam)) // ★ null → _eventCam
            {
                return;
            }

            _source = DragSource.Waste;

            // ゴースト生成
            var go = Instantiate(cardViewPrefab, dragLayer);
            _ghostRT = go.GetComponent<RectTransform>();
            _ghostRT.anchorMin = _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);
            _ghostRT.pivot = new Vector2(0.5f, 0.5f);
            _ghostRT.sizeDelta = new Vector2(100, 140);
            _ghostCg = go.AddComponent<CanvasGroup>();
            _ghostCg.alpha = ghostAlpha;

            // ★ ゴーストがレイキャストを妨げないように
            _ghostCg.blocksRaycasts = false;
            _ghostCg.interactable = false;

            // 数字表示
            int v = 0; int.TryParse(wasteValue ? wasteValue.text : "0", out v);
            _fromValue = v;

            var cvCmp = go.GetComponentInChildren<App.UI.Themeing.CardView>(true);
            if (cvCmp) cvCmp.SetValue(v);
            else
            {
                var cv = go.GetComponentInChildren<TMP_Text>(true);
                if (cv) cv.text = v.ToString();
            }

            UpdateGhostPosition(e);
        }


        public void OnDrag(PointerEventData e)
        {
            // Waste ドラッグ以外（= Column ドラッグ中）はここを通らない
            if (_source != DragSource.Waste) return;

            UpdateGhostPosition(e);

            EvaluateHover(
                e.position,
                _fromValue,
                fromColumn: false,
                fromCol: -1,
                out int newIndex,
                out bool newLegal
            );

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

        public void OnEndDrag(PointerEventData e)
        {
            // Waste ドラッグ以外（= Column ドラッグ終了時）はここを通らない
            if (_source != DragSource.Waste) return;

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

            _source = DragSource.None;
            _fromCol = -1;
        }

        // =========================================================
        // 列トップからのドラッグ（新規）
        // TopCardHandle から呼ばれる
        // =========================================================

        /// <summary>列トップドラッグ開始。</summary>
        public void BeginFromColumn(int fromColumn, RectTransform source, int value)
        {
            // 任意：Wasteドラッグが走っていたら強制キャンセル
            if (_source == DragSource.Waste)
            {
                SetHighlight(_hoverIndex, false, _hoverLegal);
                if (_ghostRT) Destroy(_ghostRT.gameObject);
                _ghostRT = null;
                _hoverIndex = -1; _hoverLegal = false;
            }

            _source = DragSource.Column;
            _fromCol = fromColumn;
            _fromValue = value;

            _eventCam = GetUICamera(); // ★ Canvasから取得

            var go = Instantiate(cardViewPrefab, dragLayer);
            _ghostRT = go.GetComponent<RectTransform>();
            _ghostRT.anchorMin = _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);
            _ghostRT.pivot = new Vector2(0.5f, 0.5f);
            _ghostRT.sizeDelta = new Vector2(100, 140);
            _ghostCg = go.AddComponent<CanvasGroup>();
            _ghostCg.alpha = ghostAlpha;
            _ghostCg.blocksRaycasts = false; // ★
            _ghostCg.interactable = false;   // ★

            var cvCmp = go.GetComponentInChildren<App.UI.Themeing.CardView>(true);
            if (cvCmp) cvCmp.SetValue(value);
            else
            {
                var cv = go.GetComponentInChildren<TMP_Text>(true);
                if (cv) cv.text = value.ToString();
            }

            // ★ 位置計算に _eventCam を使う
            if (canvasRT && source)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(_eventCam, source.position);
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, screen, _eventCam, out localPos))
                {
                    _ghostRT.anchoredPosition = localPos;
                }
            }

            SetHighlight(_hoverIndex, false, _hoverLegal);
            _hoverIndex = -1;
            _hoverLegal = false;
        }


        /// <summary>列トップドラッグ中のマウス/タッチ座標更新。</summary>
        public void UpdateFromColumn(Vector2 screenPos)
        {
            if (_source != DragSource.Column) return;

            // ゴースト位置
            if (_ghostRT && canvasRT)
            {
                Vector2 localPos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT, screenPos, _eventCam, out localPos))
                {
                    _ghostRT.anchoredPosition = localPos;
                }
            }


            EvaluateHover(screenPos, _fromValue, fromColumn: true, fromCol: _fromCol,
                          out int newIndex, out bool newLegal);

            if (newIndex != _hoverIndex || newLegal != _hoverLegal)
            {
                SetHighlight(_hoverIndex, false, _hoverLegal);
                _hoverIndex = newIndex;
                _hoverLegal = newLegal;
                SetHighlight(_hoverIndex, true, _hoverLegal);
            }
        }

        /// <summary>列トップドラッグ終了（ドロップ）。</summary>
        public void EndFromColumn(Vector2 screenPos)
        {
            if (_source != DragSource.Column) return;

            // 旧ハイライト消灯
            SetHighlight(_hoverIndex, false, _hoverLegal);

            if (_hoverIndex >= 0 && _hoverLegal && boardPlacer)
            {
                // 移動確定：合体・連鎖は BoardPlacer.MoveTop に委譲
                if (!boardPlacer.MoveTop(_fromCol, _hoverIndex, animate: true, out var _))
                {
                    if (wasteFlash) wasteFlash.Play(); // 失敗時の簡易フィードバック
                }
            }

            // ゴースト破棄
            if (_ghostRT) StartCoroutine(FlyBackAndKill());

            _source = DragSource.None;
            _fromCol = -1;
        }

        // ---------------------------------------------------------
        // 共通：ホバー判定
        // ---------------------------------------------------------
        void EvaluateHover(Vector2 screenPos, int value, bool fromColumn, int fromCol,
                           out int hitIndex, out bool legal)
        {
            hitIndex = -1;
            legal = false;

            // 配列長の取り違い防止
            int count = Mathf.Min(columns != null ? columns.Count : 0,
                                  dropHighlights != null ? dropHighlights.Count : 0);

            for (int i = 0; i < count; i++)
            {
                var col = columns[i];
                if (!col) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(columns[i], screenPos, _eventCam))
                {
                    hitIndex = i;

                    if (boardPlacer)
                    {
                        legal = fromColumn
                            ? boardPlacer.CanMoveTop(fromCol, i, out _)
                            : boardPlacer.CanPlaceAt(i, value, allowMergeOnSpawn: true);
                    }
                    else if (ruleSet && stackArrayForState != null && stackArrayForState.Length == count)
                    {
                        if (fromColumn && i == fromCol) { legal = false; }
                        else
                        {
                            var state = new App.Gameplay.BoardState(stackArrayForState);
                            legal = App.Gameplay.MoveValidator.CanPlace(i, value, state, ruleSet, out _);
                        }
                    }
                    else
                    {
                        legal = true; // 参照が未配線ならOK扱い
                    }
                    break;
                }
            }
        }

        // ---------------------------------------------------------

        void UpdateGhostPosition(PointerEventData e)
        {
            if (!_ghostRT || !canvasRT) return;

            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, e.position, _eventCam, out localPos))
            {
                _ghostRT.anchoredPosition = localPos;
            }
        }

        void SetHighlight(int idx, bool on, bool legal)
        {
            if (idx < 0 || dropHighlights == null || idx >= dropHighlights.Count) return;
            var img = dropHighlights[idx];
            if (!img) return;
            img.color = legal ? okColor : ngColor;
            img.gameObject.SetActive(on);
        }

        void SetHighlight(int idx, bool on)
        {
            if (idx < 0 || dropHighlights == null || idx >= dropHighlights.Count) return;
            if (dropHighlights[idx]) dropHighlights[idx].gameObject.SetActive(on);
        }

        IEnumerator FlyBackAndKill()
        {
            // 帰還先を中央ではなく WasteCard にしたい場合はここを差し替えてOK
            Vector3 a = _ghostRT.anchoredPosition;
            Vector3 b = Vector3.zero; // 画面中央
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
