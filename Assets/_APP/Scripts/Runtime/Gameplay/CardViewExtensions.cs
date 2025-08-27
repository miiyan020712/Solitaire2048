using UnityEngine;
using App.UI.Themeing;        // ★ CardView はここにある

namespace App.Gameplay        // ★ あなたの Gameplay と同じ namespace に
{
    public static class CardViewExtensions
    {
        // GameObject に付いている CardView に値を流し込む
        public static void SetValue(this GameObject go, int v)
        {
            var cv = go.GetComponent<CardView>();
            if (cv != null) cv.SetValue(v);
        }

        // RectTransform を取り出すユーティリティ
        public static RectTransform RectTransform(this GameObject go)
        {
            return (RectTransform)go.transform;
        }
    }
}
