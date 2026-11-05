using System;
using UnityEngine;
using UnityEngine.UI;

namespace DropStack.UI
{
    public class UIToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Text stateText;
        [SerializeField] private ArcadeSurface track;
        public bool IsOn { get; private set; } = true;
        public event Action<bool> OnValueChanged;
        private float knobX;
        private void Awake() { if(button==null) button=GetComponent<Button>(); button.onClick.AddListener(Toggle); }
        public void SetState(bool state,bool triggerEvent=true)
        {
            IsOn=state;
            if(stateText!=null) { stateText.text=state?"ON":"OFF"; stateText.rectTransform.anchoredPosition=new Vector2(state?-18:18,0); stateText.color=state?new Color(.58f,1,.34f):new Color(.6f,.69f,.78f); }
            if(track!=null) track.SetEdge(state?new Color(.4f,.8f,.25f):new Color(.24f,.32f,.4f));
            if(!Application.isPlaying || !triggerEvent) { knobX=state?27:-27; if(knob!=null) knob.anchoredPosition=new Vector2(knobX,0); }
            if(triggerEvent) { ArcadeFeedback.Pulse(12); OnValueChanged?.Invoke(state); }
        }
        public void SetInteractable(bool value) { if(button!=null) button.interactable=value; if(!value&&stateText!=null) { stateText.text="N/A"; stateText.color=new Color(.6f,.69f,.78f); } }
        public void Toggle() => SetState(!IsOn,true);
        private void Update()
        {
            knobX=ArcadeFeedback.MotionEnabled?Mathf.Lerp(knobX,IsOn?27:-27,1-Mathf.Exp(-22*Time.unscaledDeltaTime)):(IsOn?27:-27);
            if(knob!=null) knob.anchoredPosition=new Vector2(knobX,0);
        }
    }
}
