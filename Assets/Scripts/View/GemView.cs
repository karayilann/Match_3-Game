using System.Runtime.CompilerServices;
using UnityEngine;

namespace View
{
    public sealed class GemView : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private Transform _cachedTransform;

        private void Awake()
        {
            _cachedTransform = transform;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector3 position)
        {
            _cachedTransform.localPosition = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPosition()
        {
            return _cachedTransform.localPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRotation(Quaternion rotation)
        {
            _cachedTransform.localRotation = rotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion GetRotation()
        {
            return _cachedTransform.localRotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetScale(Vector3 scale)
        {
            _cachedTransform.localScale = scale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetScale()
        {
            return _cachedTransform.localScale;
        }

        public void SetSprite(Sprite sprite)
        {
            _spriteRenderer.sprite = sprite;
        }

        public void OnSpawn()
        {
            SetScale(Vector3.one);
            SetRotation(Quaternion.identity);
        }

        public void OnDespawn() { }
    }
}
