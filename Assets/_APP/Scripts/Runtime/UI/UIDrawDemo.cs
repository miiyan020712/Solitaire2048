using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDrawDemo : MonoBehaviour
{
    [Header("References")]
    public Canvas canvas;
    public RectTransform deckCard;
    public RectTransform wasteCard;
    public TMP_Text deckCount;
    public TMP_Text wasteValue;
    public Image flyCard;

    [Header("Optional")]
    public Button deckButton;          // ← DeckCardに付けたButtonをドラッグ
    public bool infiniteDemo = false;  // trueなら残0でも回す
    public float flyDuration = 0.25f;

    [Header("Demo Values (loop)")]
    public List<int> demoValues = new List<int> { 2, 4, 8, 16, 32, 64, 128 };

    int _idx = 0;
    bool _animating = false;

    // ★ 追加：元スケールとPingの多重起動防止
    Vector3 _deckBaseScale = Vector3.one;
    Coroutine _pingCo;

    void Awake()
    {
        if (deckCard) _deckBaseScale = deckCard.localScale;
    }

    int CurrentDeckCount()
    {
        if (deckCount && int.TryParse(deckCount.text, out var v)) return v;
        return 0;
    }

    public void OnDeckClicked()
    {
        if (_animating) return;

        int c = CurrentDeckCount();

        // ★ 残0は引かない。ボタンも無効化し、Pingは単発で
        if (!infiniteDemo && c <= 0)
        {
            if (deckButton) deckButton.interactable = false;

            if (_pingCo != null) StopCoroutine(_pingCo);
            _pingCo = StartCoroutine(PingDeck());
            return;
        }

        int value = demoValues.Count > 0 ? demoValues[_idx % demoValues.Count] : 2;
        _idx++;

        StartCoroutine(AnimateDraw(value, c));
    }

    IEnumerator AnimateDraw(int value, int currentCount)
    {
        _animating = true;

        // 引ける＝ボタンは押せる状態に
        if (deckButton) deckButton.interactable = true;

        // 表示カウント更新（0未満にしない）
        if (deckCount)
        {
            int next = Mathf.Max(0, currentCount - 1);
            deckCount.text = next.ToString();
            if (!infiniteDemo && deckButton && next <= 0)
                deckButton.interactable = false;
        }

        if (wasteValue) wasteValue.text = value.ToString();

        // 飛びカード
        flyCard.gameObject.SetActive(true);
        var canvasRect = (RectTransform)canvas.transform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 ToCanvasLocal(RectTransform rt)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(cam, rt.position),
                cam, out var lp);
            return lp;
        }

        var flyRT = flyCard.rectTransform;
        Vector2 from = ToCanvasLocal(deckCard);
        Vector2 to = ToCanvasLocal(wasteCard);
        Vector2 fromSize = deckCard.rect.size;
        Vector2 toSize = wasteCard.rect.size;

        flyRT.anchoredPosition = from;
        flyRT.sizeDelta = fromSize;

        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / flyDuration);
            flyRT.anchoredPosition = Vector2.Lerp(from, to, p);
            flyRT.sizeDelta = Vector2.Lerp(fromSize, toSize, p);
            yield return null;
        }

        flyCard.gameObject.SetActive(false);
        _animating = false;
    }

    // ★ 単発Ping：終わったら必ず元スケールへ
    IEnumerator PingDeck()
    {
        if (!deckCard) yield break;

        float dur = 0.18f;
        float amp = 0.12f;
        float t = 0f;

        // 念のため開始時に元へリセット
        deckCard.localScale = _deckBaseScale;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f + amp * Mathf.Sin((t / dur) * Mathf.PI); // 1→1+amp→1
            deckCard.localScale = _deckBaseScale * k;
            yield return null;
        }
        deckCard.localScale = _deckBaseScale;
        _pingCo = null;
    }
}
