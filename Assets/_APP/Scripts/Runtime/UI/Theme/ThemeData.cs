using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace App.UI.Themeing
{
    // UIで使う「役割」の列挙
    public enum ColorToken
    {
        CanvasBg,     // 画面全体の背景
        PanelBg,      // Top/Bottomのパネル
        BoardBg,      // 盤面背景
        CardBg,       // カードの面
        ButtonPrimary,
        ButtonSecondary,
        Accent,       // 強調（バッジ等）
        TextPrimary,
        TextSecondary
    }

    [CreateAssetMenu(menuName = "_App/Theme/ThemeData")]
    public class ThemeData : ScriptableObject
    {
        [Header("Palette")]
        public Color canvasBg   = new Color32(0xF5,0xF5,0xF7,255);
        public Color panelBg    = new Color32(0xE6,0xE7,0xEA,255);
        public Color boardBg    = new Color32(0xD9,0xDE,0xF2,255);
        public Color cardBg     = new Color32(0xFF,0xFF,0xFF,255);
        public Color btnPrimary = new Color32(0x16,0xA3,0xB9,255);
        public Color btnSecondary = new Color32(0x88,0x8B,0x97,255);
        public Color accent     = new Color32(0xFF,0xCC,0x00,255);
        public Color textPrimary   = new Color32(0x22,0x24,0x2B,255);
        public Color textSecondary = new Color32(0x55,0x59,0x66,255);

        [Header("TextMeshPro Fonts")]
        public TMP_FontAsset bodyFont;   // 文章やラベル
        public TMP_FontAsset numberFont; // カードの数字など

        // 2048用：値ごとの色（未設定は cardBg を使う）
        [Header("Card Value Colors")]
        public List<int> valueKeys = new(){2,4,8,16,32,64,128,256,512,1024,2048};
        public List<Color> valueColors = new(){
            new Color32(238,228,218,255), // 2
            new Color32(237,224,200,255), // 4
            new Color32(242,177,121,255), // 8
            new Color32(245,149,99,255),  // 16
            new Color32(246,124,95,255),  // 32
            new Color32(246,94,59,255),   // 64
            new Color32(237,207,114,255), // 128
            new Color32(237,204,97,255),  // 256
            new Color32(237,200,80,255),  // 512
            new Color32(237,197,63,255),  // 1024
            new Color32(237,194,46,255)   // 2048
        };

        public Color GetColor(ColorToken token) => token switch {
            ColorToken.CanvasBg => canvasBg,
            ColorToken.PanelBg => panelBg,
            ColorToken.BoardBg => boardBg,
            ColorToken.CardBg => cardBg,
            ColorToken.ButtonPrimary => btnPrimary,
            ColorToken.ButtonSecondary => btnSecondary,
            ColorToken.Accent => accent,
            ColorToken.TextPrimary => textPrimary,
            ColorToken.TextSecondary => textSecondary,
            _ => Color.white
        };

        public Color GetValueColor(int v)
        {
            int idx = valueKeys.IndexOf(v);
            return (idx >= 0 && idx < valueColors.Count) ? valueColors[idx] : cardBg;
        }
    }
}
