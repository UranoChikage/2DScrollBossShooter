using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// 汎用ObjectPool
    /// </summary>
    public class ObjectPoolManager<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private Func<T> createFunc;
        private readonly Queue<T> pool = new Queue<T>();

        public ObjectPoolManager(Func<T> createFunc, T prefab, Transform parent)
        {
            this.createFunc = createFunc;
            this.prefab = prefab;
            this.parent = parent;
        }

        //取得
        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else
            {
                obj = createFunc();
            }

            obj.gameObject.SetActive(true);

            return obj;
        }

        //返却
        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}
