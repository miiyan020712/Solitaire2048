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

        ThemeManager _themeMgr;

        void Awake()
        {
            _themeMgr = FindObjectOfType<ThemeManager>();
            Apply(); // 初期表示
        }

        public void SetValue(int v)
        {
            value = Mathf.Max(2, v);
            Apply();
        }

        void Apply()
        {
            if (valueText) valueText.text = value.ToString();

            var theme = _themeMgr ? _themeMgr.theme : null;
            if (theme && bg)
            {
                // 値に応じてカード色を取得（未定義はCardBg）
                bg.color = theme.GetValueColor(value);
            }

            // 数字フォント（numberFont）を使いたい場合：
            if (theme && theme.numberFont && valueText) valueText.font = theme.numberFont;
        }
    }
}
