using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class ObjectPooler : MonoBehaviour
    {
        [SerializeField] private Dictionary<string, Queue<GameObject>> _poolDictionary;
        [SerializeField] private List<Pool> pools;

        private void CreatePool()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (var pool in pools)
            {
                var objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    var obj = Instantiate(pool.prefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                _poolDictionary.Add(pool.tag, objectPool);
            }
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if(_poolDictionary == null)
                CreatePool();
        
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            GameObject objectToSpawn = _poolDictionary[tag].Dequeue();

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            var pooledObj = objectToSpawn.GetComponent<IPooledObject>();

            pooledObj?.OnObjectSpawn();

            _poolDictionary[tag].Enqueue(objectToSpawn);
        
            return objectToSpawn;
        }
    
        [Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
        }
    }

    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}