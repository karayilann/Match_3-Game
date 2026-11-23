using System;
using PrimeTween;
using UnityEngine;

namespace View.Popups
{
    /// <summary>
    /// The component lives on an always-active host while _root (the dimmed backdrop) is toggled.
    /// A view on the object it deactivates would never run Awake/OnEnable, so signal
    /// subscriptions and the first Open call would silently fail.
    /// </summary>
    public abstract class PopupView : MonoBehaviour
    {
        private const float OPEN_DURATION = 0.3f;
        private const float CLOSE_DURATION = 0.15f;

        [Header("Popup")]
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _dialogContainer;

        private Tween _scaleTween;
        private bool _isClosing;

        public event Action OnClosed;

        public bool IsOpen => _root != null && _root.activeSelf && !_isClosing;

        protected virtual void Awake()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_scaleTween.isAlive)
            {
                _scaleTween.Stop();
            }

            _isClosing = false;
        }

        public virtual void Open()
        {
            if (_root == null)
            {
                return;
            }

            if (_scaleTween.isAlive)
            {
                _scaleTween.Stop();
            }

            _isClosing = false;
            _root.SetActive(true);

            if (_dialogContainer != null)
            {
                _dialogContainer.localScale = Vector3.zero;
                _scaleTween = Tween.Scale(_dialogContainer, Vector3.one, OPEN_DURATION, Ease.OutBack);
            }
        }

        public void Close()
        {
            if (_root == null)
            {
                return;
            }

            _isClosing = true;
            OnClosed?.Invoke();

            if (_scaleTween.isAlive)
            {
                _scaleTween.Stop();
            }

            if (_dialogContainer == null || !_root.activeSelf || _dialogContainer.localScale.x <= 0.001f)
            {
                _root.SetActive(false);
                _isClosing = false;
                return;
            }

            _scaleTween = Tween.Scale(_dialogContainer, Vector3.zero, CLOSE_DURATION, Ease.InQuad)
                .OnComplete(this, static target =>
                {
                    target._root.SetActive(false);
                    target._isClosing = false;
                });
        }
    }
}
