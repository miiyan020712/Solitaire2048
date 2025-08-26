using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace App.Systems
{
    /// <summary>
    /// 一定秒ごとに OnTick を発火するカウントダウンタイマー。
    /// - Time.deltaTime（可変）/ unscaled（固定）を選べる
    /// - AddTime(秒) で次のティックを遅らせる（時間ボーナス用）
    /// - UI: Image.fillAmount / 任意で残り秒テキスト
    /// </summary>
    public class FallTimer : MonoBehaviour
    {
        [Header("Config")]
        [Min(0.2f)] public float tickSeconds = 5f;    // 1ティックの長さ
        public bool autoStart = true;
        public bool loop = true;
        public bool useUnscaledTime = false;          // trueにするとTime.timeScaleの影響を受けない

        [Header("UI (optional)")]
        public Image fillImage;                       // タイムバーのFill（Image Type=Filled）
        public TMP_Text countdownLabel;               // 残り秒の表示（任意）
        public bool showTenths = true;                // 0.1秒刻みで表示するか

        [Header("Events")]
        public UnityEvent OnTick;                     // 0になったら発火

        [Header("Debug")]
        public bool logTick = false;

        float _elapsed;       // 経過時間（0→tickSeconds）
        bool _running;

        public float RemainingSeconds => Mathf.Max(0f, tickSeconds - _elapsed);
        public float Normalized => Mathf.Clamp01(RemainingSeconds / Mathf.Max(0.0001f, tickSeconds));

        void OnEnable()
        {
            UpdateUI();
            if (autoStart) StartTimer();
        }

        void Update()
        {
            if (!_running) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _elapsed += dt;

            if (_elapsed >= tickSeconds)
            {
                // ティック発火
                if (logTick) Debug.Log($"[FallTimer] Tick ({tickSeconds:0.##}s)");
                OnTick?.Invoke();

                if (loop)
                {
                    _elapsed = 0f;          // リセットして続行
                }
                else
                {
                    _elapsed = tickSeconds; // 打ち止め
                    _running = false;
                }
            }

            UpdateUI();
        }

        // === Public API ===

        /// <summary>タイマー開始（リセットしてスタート）</summary>
        public void StartTimer(float? customTickSeconds = null)
        {
            if (customTickSeconds.HasValue)
                tickSeconds = Mathf.Max(0.2f, customTickSeconds.Value);

            _elapsed = 0f;
            _running = true;
            UpdateUI();
        }

        /// <summary>一時停止（値は保持）</summary>
        public void Pause()  => _running = false;

        /// <summary>再開</summary>
        public void Resume() => _running = true;

        /// <summary>停止＋リセット</summary>
        public void StopAndReset()
        {
            _running = false;
            _elapsed = 0f;
            UpdateUI();
        }

        /// <summary>次のティックまでの残り時間を増やす（遅らせる）。負値で前倒し。</summary>
        public void AddTime(float seconds)
        {
            _elapsed = Mathf.Max(0f, _elapsed - seconds);
            UpdateUI();
        }

        /// <summary>ティック長を変更（次回から反映）</summary>
        public void SetTickSeconds(float seconds)
        {
            tickSeconds = Mathf.Max(0.2f, seconds);
            _elapsed = Mathf.Min(_elapsed, tickSeconds);
            UpdateUI();
        }

        // === UI ===

        void UpdateUI()
        {
            if (fillImage)
            {
                // 残り割合でFill（時間が減る＝Fillも減る）
                fillImage.fillAmount = Normalized;
            }

            if (countdownLabel)
            {
                float s = RemainingSeconds;
                countdownLabel.text = showTenths ? s.ToString("0.0") : Mathf.CeilToInt(s).ToString();
            }
        }
    }
}
