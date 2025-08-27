using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI.Themeing
{
    public class CardView : MonoBehaviour
    {
        [Header("Refs")]
        public Image bg;          // 背景Image
        public TMP_Text valueText;// 中央の数字

        [Header("State")]
        [SerializeField] int value = 2;

        // ★ BoardPlacer から使いやすい読み取り専用プロパティ
        public int Value => value;
        public RectTransform Rect => (RectTransform)transform;

        ThemeManager _themeMgr;

        void Awake()
        {
            if (_themeMgr == null) _themeMgr = FindObjectOfType<ThemeManager>();
            Apply(); // 初期表示
        }

        #if UNITY_EDITOR
        // エディタで値を変えた時も即反映
        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (_themeMgr == null) _themeMgr = FindObjectOfType<ThemeManager>();
                Apply();
            }
        }
        #endif

        public void SetValue(int v)
        {
            value = Mathf.Max(2, v);
            Apply();
        }

        public void Apply()
        {
            if (valueText) valueText.text = value.ToString();

            var theme = _themeMgr ? _themeMgr.theme : null;
            if (theme && bg)
            {
                // 値に応じてカード色を取得（未定義はCardBg）
                bg.color = theme.GetValueColor(value);
            }

            // 数字フォント（numberFont）を使いたい場合
            if (theme && theme.numberFont && valueText) valueText.font = theme.numberFont;
        }
    }
}
