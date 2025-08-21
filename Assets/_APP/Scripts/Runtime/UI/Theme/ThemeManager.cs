using System.Linq;
using UnityEngine;

namespace App.UI.Themeing
{
    public class ThemeManager : MonoBehaviour
    {
        public ThemeData theme;

        public void ApplyTheme()
        {
            if(!theme) return;
            // 子孫の Themable を一括反映
            foreach (var ti in GetComponentsInChildren<ThemeImage>(true)) ti.Apply(theme);
            foreach (var tt in GetComponentsInChildren<ThemeText>(true))  tt.Apply(theme);

            // 画面背景（Canvas直下など）もあればここで調整可
        }

        // エディタからもすぐ反映したい人向け
        void OnValidate(){ if (Application.isEditor && !Application.isPlaying) ApplyTheme(); }
        void Start(){ ApplyTheme(); }
    }
}
