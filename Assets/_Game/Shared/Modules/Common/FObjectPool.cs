/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-17
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Falcon.Shared.PoolManager
{
    public class FObjectPool<T> : ObjectPool<T>, IObjectPool<T> where T : Component
    {
        public HashSet<T> SpawnedObjects { get; set; } = new();

        public FObjectPool(T prefab, Transform parent = null) : base(
            createFunc: () => Object.Instantiate(prefab, parent),
            actionOnGet: obj => obj.gameObject.SetActive(true),
            actionOnRelease: obj => obj.gameObject.SetActive(false),
            actionOnDestroy: obj => Object.Destroy(obj.gameObject)
        )
        {
        }
        
        public new T Get()
        {
            var item = base.Get();
            SpawnedObjects.Add(item);
            return item;
        }

        public new PooledObject<T> Get(out T v)
        {
            var pooledObj = base.Get(out v);
            SpawnedObjects.Add(v);
            return pooledObj;
        }

        public new void Release(T element)
        {
            SpawnedObjects.Remove(element);
            base.Release(element);
        }

        public void Reset()
        {
            foreach (var obj in SpawnedObjects)
            {
                base.Release(obj);
            }

            SpawnedObjects.Clear();
        }

        public void Preload(int num)
        {
            for (int i = 0; i < num; i++)
            {
                Get();
            }
            Reset();
        }

        public int SpawnedCount => SpawnedObjects.Count;
    }
}