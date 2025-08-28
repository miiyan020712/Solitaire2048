using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public class TimeBarController : MonoBehaviour
    {
        [Header("Refs")]
        public Image fill;           // TimeBar_Fill を割り当てる

        [Header("State")]
        [SerializeField] float duration = 5f;

        float t;
        bool paused = true;

        // RowSpawner から呼ばれる想定
        public void Play(float seconds)
        {
            duration = Mathf.Max(0.01f, seconds);
            t = 0f;
            paused = false;
            UpdateFill();
        }

        public void Pause(bool pause = true)
        {
            paused = pause;
        }

        public void Stop()
        {
            paused = true;
            t = duration;
            UpdateFill();
        }

        void Update()
        {
            if (paused) return;
            t += Time.deltaTime;
            if (t > duration) t = duration;
            UpdateFill();
        }

        void UpdateFill()
        {
            if (!fill) return;
            // 左→右に縮む前提（Filled / Horizontal / Origin Left）
            float ratio = Mathf.Clamp01(t / duration);
            fill.fillAmount = 1f - ratio;
        }
    }
}
