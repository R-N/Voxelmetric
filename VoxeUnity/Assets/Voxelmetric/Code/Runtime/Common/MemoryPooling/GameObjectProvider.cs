using System;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Voxelmetric
{
    [AddComponentMenu("VoxelMetric/Singleton/GameObjectProvider")]
    public sealed class GameObjectProvider : MonoSingleton<GameObjectProvider>
    {
        private GameObject go;
        [FormerlySerializedAs("ObjectPools")]
        public ObjectPoolEntry[] objectPools = new ObjectPoolEntry[0];

        private readonly StringBuilder stringBuilder = new StringBuilder();

        public GameObject ProviderGameObject
        {
            get { return go; }
        }

        // Called after the singleton instance is created
        private void Awake()
        {
            go = new GameObject("GameObjects");
            go.transform.parent = gameObject.transform;

            // Iterate pool entries and create a pool of prefabs for each of them
            foreach (ObjectPoolEntry pool in Instance.objectPools)
            {
                if (pool.Prefab == null)
                {
                    Debug.LogError("No prefab specified in one of the object pool's entries");
                    continue;
                }

                pool.Init(go, pool.Prefab);
            }
        }

        // Returns a pool of a given name if it exists
        public static ObjectPoolEntry GetPool(string poolName)
        {
            foreach (ObjectPoolEntry pool in Instance.objectPools)
            {
                if (pool.Name == poolName)
                {
                    return pool;
                }
            }
            return null;
        }

        public static void PushObject(string poolName, GameObject go)
        {
            if (go == null)
            {
                throw new ArgumentNullException(string.Format("Trying to pool a null game object in pool {0}", poolName));
            }

            ObjectPoolEntry pool = GetPool(poolName);
            if (pool == null)
            {
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));
            }

            pool.Push(go);
        }

        public static GameObject PopObject(string poolName)
        {
            ObjectPoolEntry pool = GetPool(poolName);
            if (pool == null)
            {
                throw new InvalidOperationException(string.Format("Object pool {0} does not exist", poolName));
            }

            return pool.Pop();
        }

        public override string ToString()
        {
            stringBuilder.Length = 0;

            stringBuilder.Append("ObjectPools ");
            foreach (ObjectPoolEntry entry in objectPools)
            {
                stringBuilder.ConcatFormat("{0},", entry.ToString());
            }

            return stringBuilder.ToString();
        }

        [Serializable]
        public class ObjectPoolEntry
        {
            public string Name;
            public GameObject Prefab;
            public int InitialSize = 128;

            [HideInInspector] public ObjectPool<GameObject> Cache;

            private GameObject parentGo;

            public ObjectPoolEntry()
            {
                //Name = string.Empty;
                //InitialSize = 0;
                parentGo = null;
                Prefab = null;
                Cache = null;
            }

            public void Init(GameObject parentGo, GameObject prefab)
            {
                this.parentGo = parentGo;
                Prefab = prefab;

                Cache = new ObjectPool<GameObject>(
                    arg =>
                    {
                        GameObject newGO = Instantiate(Prefab);
                        newGO.name = Prefab.name;
                        newGO.SetActive(false);
                        newGO.transform.parent = this.parentGo.transform; // Make this object a parent of the pooled object
                        return newGO;
                    },
                    InitialSize,
                    false
                    );
            }

            public void Push(GameObject go)
            {
                // Deactive object, reset its' transform and physics data
                go.SetActive(false);

                Rigidbody rbody = go.GetComponent<Rigidbody>();
                if (rbody != null)
                {
                    rbody.velocity = Vector3.zero;
                }

                // Place a pointer to our object to the back of our cache list
                Cache.Push(go);
            }

            public GameObject Pop()
            {
                GameObject go = Cache.Pop();

                // Reset transform and active it
                //go.transform.parent = null;
                Assert.IsTrue(!go.activeSelf, "Popped an active gameObject!");
                go.SetActive(true);

                return go;
            }

            public override string ToString()
            {
                return string.Format("{0}={1}", Name, Cache);
            }
        }
    }
}
