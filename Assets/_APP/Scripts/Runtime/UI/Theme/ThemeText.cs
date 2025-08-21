using TMPro;
using UnityEngine;

namespace App.UI.Themeing
{
    [RequireComponent(typeof(TMP_Text))]
    public class ThemeText : MonoBehaviour
    {
        public ColorToken token = ColorToken.TextPrimary;
        public bool useNumberFont = false;
        TMP_Text _txt;

        void Awake(){ _txt = GetComponent<TMP_Text>(); }
        public void Apply(ThemeData theme){
            if(!_txt || theme==null) return;
            _txt.color = theme.GetColor(token);
            if (useNumberFont && theme.numberFont) _txt.font = theme.numberFont;
            else if(!useNumberFont && theme.bodyFont) _txt.font = theme.bodyFont;
        }
    }
}
