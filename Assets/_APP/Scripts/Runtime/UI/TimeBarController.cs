using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TimeBarController : MonoBehaviour
{
    [Header("Refs")]
    public Image fill;              // TimeBar_Fill をドラッグ
    public Gradient colorByRatio;   // 空でもOK（任意）

    [Header("Timer")]
    public float duration = 5f;     // 1ウェーブの秒数
    public bool autoStart = true;

    [Header("Events")]
    public UnityEvent onTimeout;    // 0になった時（次の段追加トリガー等）

    float _t;        // 経過時間
    bool _running;

    void Start()
    {
        if (autoStart) ResetAndStart();
    }

    void Update()
    {
        if (!_running || duration <= 0f) return;

        _t += Time.deltaTime;
        float ratio = Mathf.Clamp01(1f - _t / duration); // 1→0
        if (fill) fill.fillAmount = ratio;

        if (fill && colorByRatio != null)
            fill.color = colorByRatio.Evaluate(ratio);

        if (_t >= duration)
        {
            _running = false;
            onTimeout?.Invoke();    // ここで段追加の処理へ
        }
    }

    public void ResetAndStart(float? newDuration = null)
    {
        if (newDuration.HasValue) duration = Mathf.Max(0.01f, newDuration.Value);
        _t = 0f;
        if (fill) fill.fillAmount = 1f;
        _running = true;
    }

    public void Pause(bool pause) => _running = !pause && duration > 0f;
}
