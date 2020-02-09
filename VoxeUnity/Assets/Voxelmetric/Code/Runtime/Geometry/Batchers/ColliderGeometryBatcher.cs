using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voxelmetric
{
    public class ColliderGeometryBatcher : IGeometryBatcher
    {
        private readonly string prefabName;
        //! Materials our meshes are to use
        private readonly PhysicMaterial[] materials;
        //! A list of buffers for each material
        private readonly List<ColliderGeometryBuffer>[] buffers;
        //! GameObjects used to hold our geometry
        private readonly List<GameObject> objects;

        private bool enabled = false;
        public bool Enabled
        {
            set
            {
                if (value != enabled)
                {
                    for (int i = 0; i < objects.Count; i++)
                    {
                        objects[i].SetActive(value);
                    }
                }
                enabled = value;
            }
            get { return enabled; }
        }

        public ColliderGeometryBatcher(string prefabName, PhysicMaterial[] materials)
        {
            this.prefabName = prefabName;
            this.materials = materials;

            int buffersLen = (materials == null || materials.Length < 1) ? 1 : materials.Length;
            buffers = new List<ColliderGeometryBuffer>[buffersLen];
            for (int i = 0; i < buffers.Length; i++)
            {
                /* TODO: Let's be optimistic and allocate enough room for just one buffer. It's going to suffice
                 * in >99% of cases. However, this prediction should maybe be based on chunk size rather then
                 * optimism. The bigger the chunk the more likely we're going to need to create more meshes to
                 * hold its geometry because of Unity's 65k-vertices limit per mesh. For chunks up to 32^3 big
                 * this should not be an issue, though.
                 */
                buffers[i] = new List<ColliderGeometryBuffer>(1)
                {
                    // Default render buffer
                    new ColliderGeometryBuffer()
                };
            }

            objects = new List<GameObject>();

            Clear();
        }

        public void Reset()
        {
            // Buffers need to be reallocated. Otherwise, more and more memory would be consumed by them. This is
            // because internal arrays grow in capacity and we can't simply release their memory by calling Clear().
            // Objects and renderers are fine, because there's usually only 1 of them. In some extreme cases they
            // may grow more but only by 1 or 2 (and only if Env.ChunkPow>5).
            for (int i = 0; i < buffers.Length; i++)
            {
                List<ColliderGeometryBuffer> geometryBuffer = buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    if (geometryBuffer[j].WasUsed)
                    {
                        geometryBuffer[j] = new ColliderGeometryBuffer();
                    }
                }
            }

            ReleaseOldData();
            enabled = false;
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            foreach (List<ColliderGeometryBuffer> holder in buffers)
            {
                for (int i = 0; i < holder.Count; i++)
                {
                    holder[i].Clear();
                }
            }

            ReleaseOldData();
            enabled = false;
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            List<ColliderGeometryBuffer> holder = buffers[materialID];
            ColliderGeometryBuffer buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.vertices.Count + 4 > 65000)
            {
                buffer = new ColliderGeometryBuffer();
                holder.Add(buffer);
            }

            // Add vertices
            buffer.AddVertices(verts);

            // Add indices
            buffer.AddIndices(buffer.vertices.Count, backFace);
        }

        /// <summary>
        ///     Creates a mesh and commits it to the engine. Bounding box is calculated from vertices
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
            )
        {
            ReleaseOldData();

            for (int j = 0; j < buffers.Length; j++)
            {
                List<ColliderGeometryBuffer> holder = buffers[j];
                PhysicMaterial material = (materials == null || materials.Length < 1) ? null : materials[j];

                for (int i = 0; i < holder.Count; i++)
                {
                    ColliderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                    {
                        continue;
                    }

                    // Create a game object for collider. Unfortunatelly, we can't use object pooling
                    // here. Unity3D would have to rebake the geometry of the old object because of a
                    // change in its position and that is very time consuming.
                    GameObject prefab = GameObjectProvider.GetPool(prefabName).Prefab;
                    GameObject go = Object.Instantiate(prefab);
                    go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.meshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length <= 0);
                        buffer.SetupMesh(mesh, true);

                        MeshCollider collider = go.GetComponent<MeshCollider>();
                        collider.enabled = true;
                        collider.sharedMesh = null;
                        collider.sharedMesh = mesh;
                        Transform t = collider.transform;
                        t.position = position;
                        t.rotation = rotation;
                        collider.sharedMaterial = material;

                        objects.Add(go);
                    }

                    buffer.Clear();
                }
            }
        }

        /// <summary>
        ///     Creates a mesh and commits it to the engine. Bounding box set according to value passed in bounds
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation, ref Bounds bounds
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j < buffers.Length; j++)
            {
                List<ColliderGeometryBuffer> holder = buffers[j];
                PhysicMaterial material = (materials == null || materials.Length < 1) ? null : materials[j];

                for (int i = 0; i < holder.Count; i++)
                {
                    ColliderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                    {
                        continue;
                    }

                    // Create a game object for collider. Unfortunatelly, we can't use object pooling
                    // here. Unity3D would have to rebake the geometry of the old object because of a
                    // change in its position and that is very time consuming.
                    GameObject prefab = GameObjectProvider.GetPool(prefabName).Prefab;
                    GameObject go = Object.Instantiate(prefab);
                    go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.meshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length <= 0);
                        buffer.SetupMesh(mesh, false);
                        mesh.bounds = bounds;

                        MeshCollider collider = go.GetComponent<MeshCollider>();
                        collider.enabled = true;
                        collider.sharedMesh = null;
                        collider.sharedMesh = mesh;
                        Transform t = collider.transform;
                        t.position = position;
                        t.rotation = rotation;
                        collider.sharedMaterial = material;

                        objects.Add(go);
                    }

                    buffer.Clear();
                }
            }
        }

        private void ReleaseOldData()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject go = objects[i];
                // If the component does not exist it means nothing else has been added as well
                if (go == null)
                {
                    continue;
                }

#if DEBUG
                go.name = prefabName;
#endif

                MeshCollider collider = go.GetComponent<MeshCollider>();
                collider.sharedMesh.Clear(false);
                Globals.MemPools.meshPool.Push(collider.sharedMesh);
                collider.sharedMesh = null;
                collider.sharedMaterial = null;

                Object.DestroyImmediate(go);
            }

            objects.Clear();
        }
    }
}
