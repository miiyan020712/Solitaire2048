using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace App.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonPulse : MonoBehaviour
    {
        [SerializeField] float scaleDown = 0.92f;
        [SerializeField] float dur = 0.06f;

        Button _btn; Vector3 _orig;
        void Awake(){ _btn = GetComponent<Button>(); _orig = transform.localScale; _btn.onClick.AddListener(Pulse); }
        void OnDestroy(){ _btn.onClick.RemoveListener(Pulse); }

        void Pulse(){ if(!gameObject.activeInHierarchy) return; StartCoroutine(Co()); }
        IEnumerator Co(){
            yield return LerpScale(_orig, _orig * scaleDown, dur);
            yield return LerpScale(transform.localScale, _orig, dur);
        }
        IEnumerator LerpScale(Vector3 a, Vector3 b, float t){
            float e=0f; while(e<t){ e+=Time.unscaledDeltaTime; transform.localScale=Vector3.Lerp(a,b,e/t); yield return null; }
            transform.localScale=b;
        }
    }
}
