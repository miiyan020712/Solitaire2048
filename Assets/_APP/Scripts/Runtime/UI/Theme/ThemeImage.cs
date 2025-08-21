using UnityEngine;
using UnityEngine.UI;

namespace App.UI.Themeing
{
    [RequireComponent(typeof(Image))]
    public class ThemeImage : MonoBehaviour
    {
        public ColorToken token = ColorToken.PanelBg;
        Image _img;

        void Awake(){ _img = GetComponent<Image>(); }
        public void Apply(ThemeData theme){
            if(!_img || theme==null) return;
            _img.color = theme.GetColor(token);
        }
    }
}
