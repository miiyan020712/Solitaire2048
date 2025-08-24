using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    // ターゲットImageの色を一瞬つけてフェードアウト（オーバーレイ用）
    public class UIFlash : MonoBehaviour
    {
        public Image target;                 // 透明なImage推奨（UISprite 1px 白）
        public Color flashColor = new Color(1, 0, 0, 0.35f);
        public float inDuration = 0.06f;
        public float outDuration = 0.18f;

        Color _orig;
        void Awake()
        {
            if (!target) target = GetComponent<Image>();
            _orig = target ? target.color : Color.clear;
            if (target) target.color = new Color(_orig.r, _orig.g, _orig.b, 0); // 透明開始
        }

        public void Play()
        {
            if (gameObject.activeInHierarchy) StartCoroutine(Co());
        }

        IEnumerator Co()
        {
            if (!target) yield break;

            // 立ち上がり
            float t = 0f;
            Color from = target.color, to = flashColor;
            while (t < inDuration)
            {
                t += Time.unscaledDeltaTime;
                target.color = Color.Lerp(from, to, t / inDuration);
                yield return null;
            }
            target.color = to;

            // フェードアウト
            t = 0f; from = target.color; to = new Color(_orig.r, _orig.g, _orig.b, 0);
            while (t < outDuration)
            {
                t += Time.unscaledDeltaTime;
                target.color = Color.Lerp(from, to, t / outDuration);
                yield return null;
            }
            target.color = to;
        }
    }
}
