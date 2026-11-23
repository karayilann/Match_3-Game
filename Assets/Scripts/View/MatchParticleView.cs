using System;
using UnityEngine;

namespace View
{
    public sealed class MatchParticleView : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private ParticleSystem _particleSystem;

        private Transform _cachedTransform;
        private Action<MatchParticleView> _onFinished;

        private void Awake()
        {
            _cachedTransform = transform;
        }

        public void Play(Vector3 worldPosition, Action<MatchParticleView> onFinished)
        {
            _onFinished = onFinished;
            _cachedTransform.localPosition = worldPosition;
            _particleSystem.Play(true);
        }

        private void OnParticleSystemStopped()
        {
            Action<MatchParticleView> callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(this);
        }

        public void OnSpawn() { }

        public void OnDespawn()
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}

