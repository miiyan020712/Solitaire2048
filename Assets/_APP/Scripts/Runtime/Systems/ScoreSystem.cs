using System.Collections;
using TMPro;
using UnityEngine;
using App.Gameplay;

namespace App.Systems
{
    public class ScoreSystem : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text scoreText;         // TopBar/ScoreArea/ScoreValue
        public RectTransform scoreTextRT;  // ↑のRectTransform（数値をポンっとさせる）
        public TMP_Text comboText;         // ComboBadge の TMP（"x1.0"）
        public GameObject comboBadgeGO;    // ComboBadge 全体（倍率1.0なら非表示に）

        public ScorePolicy policy;
        public BoardPlacer placer;         // （後で割当）BoardPlacer からイベント購読

        int   score = 0;
        float combo = 1f;
        float lastMergeTime = -999f;

        void Awake()
        {
            combo = policy ? policy.comboStart : 1f;
            UpdateScoreUI();
            UpdateComboUI();
        }

        void OnEnable()
        {
            if (placer != null)
            {
                placer.OnMerged += HandleMerged;
                placer.OnPlacedNoMerge += HandleNoMerge;
            }
        }
        void OnDisable()
        {
            if (placer != null)
            {
                placer.OnMerged -= HandleMerged;
                placer.OnPlacedNoMerge -= HandleNoMerge;
            }
        }

        void Update()
        {
            if (policy && combo > 1.0f && Time.unscaledTime - lastMergeTime > policy.comboTimeout)
            {
                ResetCombo();
            }
        }

        void HandleMerged(int mergedValue)
        {
            lastMergeTime = Time.unscaledTime;

            int add = (policy && policy.mergeScore == MergeScoreMode.NewValue)
                      ? mergedValue        // 2048式：できたタイルの値を加点
                      : mergedValue;       // 拡張余地
            add = Mathf.RoundToInt(add * combo);

            score += add;
            UpdateScoreUI();
            if (scoreTextRT) StartCoroutine(Punch(scoreTextRT));

            // コンボ上昇
            float step = policy ? policy.comboStep : 0.2f;
            float max  = policy ? policy.comboMax  : 3.0f;
            combo = Mathf.Min(combo + step, max);
            UpdateComboUI();
        }

        void HandleNoMerge()
        {
            if (policy && policy.resetOnNoMergePlacement) ResetCombo();
        }

        public void ResetAll()
        {
            score = 0;
            ResetCombo();
            UpdateScoreUI();
        }

        void ResetCombo()
        {
            combo = policy ? policy.comboStart : 1f;
            UpdateComboUI();
        }

        void UpdateScoreUI()
        {
            if (scoreText) scoreText.text = score.ToString("N0");
        }
        void UpdateComboUI()
        {
            if (comboText) comboText.text = $"x{combo:0.0}";
            if (comboBadgeGO) comboBadgeGO.SetActive(combo > 1.0f + 1e-4f);
        }

        IEnumerator Punch(RectTransform rt)
        {
            Vector3 a = Vector3.one, b = a * 1.15f;
            float t = 0f, d = 0.08f;
            while (t < d) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.Lerp(a, b, t / d); yield return null; }
            t = 0f;
            while (t < d) { t += Time.unscaledDeltaTime; rt.localScale = Vector3.Lerp(b, a, t / d); yield return null; }
            rt.localScale = a;
        }
    }
}
