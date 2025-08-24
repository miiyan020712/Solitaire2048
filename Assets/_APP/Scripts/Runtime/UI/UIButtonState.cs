using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    public class UIButtonState : MonoBehaviour
    {
        public Button button;
        public CanvasGroup canvasGroup;   // 無ければ自動で付与
        public TMP_Text badgeText;        // 任意。個数の更新に使用

        void Reset(){
            button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void SetInteractable(bool on){
            if(button) button.interactable = on;
            if(canvasGroup){ canvasGroup.alpha = on ? 1f : 0.5f; canvasGroup.blocksRaycasts = on; }
        }

        public void SetCount(int n){ if(badgeText) badgeText.text = n.ToString(); SetInteractable(n > 0); }
    }
}
