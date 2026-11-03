using UnityEngine;

namespace NeonDash
{
    /// <summary>Base for the full-screen UI layers (lobby, HUD, result, records, pause).</summary>
    public abstract class UiScreen : MonoBehaviour
    {
        protected NeonDashApp App { get; private set; }
        protected RectTransform Root { get; private set; }
        protected CanvasGroup Group { get; private set; }

        public void Init(NeonDashApp app)
        {
            App = app;
            Root = (RectTransform)transform;
            UiKit.Stretch(Root);
            Group = gameObject.AddComponent<CanvasGroup>();
            Group.blocksRaycasts = true;
            Construct();
            gameObject.SetActive(false);
        }

        protected abstract void Construct();

        public static T Create<T>(NeonDashApp app, Transform parent, string name) where T : UiScreen
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<T>();
            screen.Init(app);
            return screen;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            UiKit.FadeIn(Group, 0.22f);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
