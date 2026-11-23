using System.Collections.Generic;
using UnityEngine;

namespace Core.Pooling
{
    public sealed class PoolManager<T> where T : Component, IPoolableObject
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly T _prefab;
        private readonly Transform _parent;

        public PoolManager(T prefab, Transform parent, int prewarmCount = 0)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        public T Get()
        {
            T instance = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
            instance.gameObject.SetActive(true);
            instance.OnSpawn();
            return instance;
        }

        public void Release(T instance)
        {
            instance.OnDespawn();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(_parent);
            _pool.Enqueue(instance);
        }

        private T CreateInstance()
        {
            return Object.Instantiate(_prefab, _parent);
        }
    }
}
