using GravityLab.Core;
using UnityEngine;

namespace GravityLab.UI
{
    /// <summary>Base for full-screen UI states: fade + slight scale transition on a CanvasGroup (unscaled time).</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        CanvasGroup _group;
        Coroutine _transition;

        public bool IsVisible { get; private set; }
        protected RectTransform Root { get { return (RectTransform)transform; } }

        public void Setup(UIManager ui)
        {
            _group = GetComponent<CanvasGroup>();
            Build(ui);
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            gameObject.SetActive(false);
        }

        protected abstract void Build(UIManager ui);

        protected virtual void OnShow() { }

        public void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            OnShow();
            Tweener.Cancel(_transition);
            float from = _group.alpha;
            _transition = Tweener.Run(0.22f, t =>
            {
                float e = Tweener.EaseOutCubic(t);
                _group.alpha = Mathf.Lerp(from, 1f, e);
                float s = Mathf.Lerp(0.97f, 1f, e);
                transform.localScale = new Vector3(s, s, 1f);
            });
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            Tweener.Cancel(_transition);
            float from = _group.alpha;
            _transition = Tweener.Run(0.15f, t => _group.alpha = Mathf.Lerp(from, 0f, t), () => gameObject.SetActive(false));
        }
    }
}
