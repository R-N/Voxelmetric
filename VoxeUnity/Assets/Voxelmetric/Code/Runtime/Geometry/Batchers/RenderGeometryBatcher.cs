using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Geometry.Buffers;

namespace Voxelmetric.Code.Geometry.Batchers
{
    public class RenderGeometryBatcher : IGeometryBatcher
    {
        private readonly string prefabName;
        //! Materials our meshes are to use
        private readonly Material[] materials;
        //! A list of buffers for each material
        public List<RenderGeometryBuffer>[] Buffers { get; }
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

        public RenderGeometryBatcher(string prefabName, Material[] materials)
        {
            this.prefabName = prefabName;
            this.materials = materials;

            int buffersCount = materials == null || materials.Length < 1 ? 1 : materials.Length;
            Buffers = new List<RenderGeometryBuffer>[buffersCount];

            for (int i = 0; i < Buffers.Length; i++)
            {
                /* TODO: Let's be optimistic and allocate enough room for just one buffer. It's going to suffice
                 * in >99% of cases. However, this prediction should maybe be based on chunk size rather then
                 * optimism. The bigger the chunk the more likely we're going to need to create more meshes to
                 * hold its geometry because of Unity's 65k-vertices limit per mesh. For chunks up to 32^3 big
                 * this should not be an issue, though.
                 */
                Buffers[i] = new List<RenderGeometryBuffer>(1)
                {
                    // Default render buffer
                    new RenderGeometryBuffer()
                };
            }

            objects = new List<GameObject>(1);

            Clear();
        }

        public void Reset()
        {
            // Buffers need to be reallocated. Otherwise, more and more memory would be consumed by them. This is
            // because internal arrays grow in capacity and we can't simply release their memory by calling Clear().
            // Objects and renderers are fine, because there's usually only 1 of them. In some extreme cases they
            // may grow more but only by 1 or 2 (and only if Env.ChunkPow>5).
            for (int i = 0; i < Buffers.Length; i++)
            {
                List<RenderGeometryBuffer> geometryBuffer = Buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    if (geometryBuffer[j].WasUsed)
                    {
                        geometryBuffer[j] = new RenderGeometryBuffer();
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
            for (int i = 0; i < Buffers.Length; i++)
            {
                List<RenderGeometryBuffer> geometryBuffer = Buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    geometryBuffer[j].Clear();
                }
            }

            ReleaseOldData();
            enabled = false;
        }

        private static void PrepareColors(ref List<Color32> colors, List<Vector3> vertices, int initialVertexCount)
        {
            if (colors == null)
            {
                colors = new List<Color32>(vertices.Capacity);
            }
            else if (colors.Count < initialVertexCount)
            {
                // Fill in colors if necessary
                colors.Capacity = vertices.Capacity;
                int diff = initialVertexCount - colors.Count;
                for (int i = 0; i < diff; i++)
                {
                    colors.Add(new Color32());
                }
            }
        }

        private static void PrepareUVs(ref List<Vector4> uvs, List<Vector3> vertices, int initialVertexCount)
        {
            if (uvs == null)
            {
                uvs = new List<Vector4>(vertices.Capacity);
            }
            else if (uvs.Count < initialVertexCount)
            {
                // Fill in colors if necessary
                uvs.Capacity = vertices.Capacity;
                int diff = initialVertexCount - uvs.Count;
                for (int i = 0; i < diff; i++)
                {
                    uvs.Add(Vector4.zero);
                }
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="colors">Colors to be processed</param>
        /// <param name="offset">Offset to apply to verts</param>
        public void AddMeshData(int materialID, int[] tris, Vector3[] verts, Color32[] colors, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess > 0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000 - buffer.vertices.Count;
                if (leftInBuffer <= 0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        colors = new List<Color32>()
                    };

                    buffer.triangles.Capacity = left;
                    buffer.vertices.Capacity = left;
                    buffer.colors.Capacity = left;

                    holder.Add(buffer);
                }
                else
                {
                    left = Math.Min(left, leftInBuffer);
                }

                int max = startOffset + left;
                int maxTris = (max * 3) >> 1;
                int offsetTri = (startOffset * 3) >> 1;

                // Add vertices
                int initialVertexCount = buffer.vertices.Count;
                for (int i = startOffset; i < max; i++)
                {
                    buffer.vertices.Add(verts[i] + offset);
                }

                // Add colors
                PrepareColors(ref buffer.colors, buffer.vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                {
                    buffer.colors.Add(colors[i]);
                }

                // Add triangles
                for (int i = offsetTri; i < maxTris; i++)
                {
                    buffer.triangles.Add(tris[i] + initialVertexCount);
                }

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="uvs">UVs to be processed</param>
        /// <param name="texture">Texture coordinates</param>
        /// <param name="offset">Offset to apply to vertices</param>
        public void AddMeshData(int materialID, int[] tris, Vector3[] verts, Vector4[] uvs, ref Rect texture, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess > 0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000 - buffer.vertices.Count;
                if (leftInBuffer <= 0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        uV1s = new List<Vector4>()
                    };

                    buffer.triangles.Capacity = left;
                    buffer.vertices.Capacity = left;
                    buffer.uV1s.Capacity = left;

                    holder.Add(buffer);
                }
                else
                {
                    left = Math.Min(left, leftInBuffer);
                }

                int max = startOffset + left;
                int maxTris = (max * 3) >> 1;
                int offsetTri = (startOffset * 3) >> 1;

                // Add vertices
                int initialVertexCount = buffer.vertices.Count;
                for (int i = startOffset; i < max; i++)
                {
                    buffer.vertices.Add(verts[i] + offset);
                }

                // Add UVs
                PrepareUVs(ref buffer.uV1s, buffer.vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                {
                    // Adjust UV coordinates according to provided texture atlas
                    buffer.uV1s.Add(new Vector4((uvs[i].x * texture.width) + texture.x, (uvs[i].y * texture.height) + texture.y));
                }

                // Add triangles
                for (int i = offsetTri; i < maxTris; i++)
                {
                    buffer.triangles.Add(tris[i] + initialVertexCount);
                }

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="colors">Colors to be processed</param>
        /// <param name="uvs">UVs to be processed</param>
        /// <param name="texture">Texture coordinates</param>
        /// <param name="offset">Offset to apply to vertices</param>
        public void AddMeshData(int materialID, int[] tris, Vector3[] verts, Color32[] colors, Vector4[] uvs, ref Rect texture, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess > 0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000 - buffer.vertices.Count;
                if (leftInBuffer <= 0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        uV1s = new List<Vector4>(),
                        colors = new List<Color32>()
                    };

                    buffer.triangles.Capacity = left;
                    buffer.vertices.Capacity = left;
                    buffer.uV1s.Capacity = left;
                    buffer.colors.Capacity = left;

                    holder.Add(buffer);
                }
                else
                {
                    left = Math.Min(left, leftInBuffer);
                }

                int max = startOffset + left;
                int maxTris = (max * 3) >> 1;
                int offsetTri = (startOffset * 3) >> 1;

                // Add vertices
                int initialVertexCount = buffer.vertices.Count;
                for (int i = startOffset; i < max; i++)
                {
                    buffer.vertices.Add(verts[i] + offset);
                }

                // Add UVs
                PrepareUVs(ref buffer.uV1s, buffer.vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                {
                    // Adjust UV coordinates according to provided texture atlas
                    buffer.uV1s.Add(new Vector4((uvs[i].x * texture.width) + texture.x, (uvs[i].y * texture.height) + texture.y));
                }

                // Add colors
                PrepareColors(ref buffer.colors, buffer.vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                {
                    buffer.colors.Add(colors[i]);
                }

                // Add triangles
                for (int i = offsetTri; i < maxTris; i++)
                {
                    buffer.triangles.Add(tris[i] + initialVertexCount);
                }

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="uvs">An array of 4 UV coordinates</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Vector4[] uvs, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    uV1s = new List<Vector4>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.vertices.Count;
            buffer.vertices.AddRange(verts);

            // Add indices
            buffer.AddIndices(buffer.vertices.Count, backFace);

            // Add UVs
            PrepareUVs(ref buffer.uV1s, buffer.vertices, initialVertexCount);
            buffer.uV1s.AddRange(uvs);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="colors">An array of 4 colors</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Color32[] colors, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    colors = new List<Color32>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.vertices.Count;
            buffer.vertices.AddRange(verts);

            // Add colors
            PrepareColors(ref buffer.colors, buffer.vertices, initialVertexCount);
            buffer.colors.AddRange(colors);

            // Add indices
            buffer.AddIndices(buffer.vertices.Count, backFace);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="colors">An array of 4 colors</param>
        /// <param name="uvs">An array of 4 UV coordinates</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Color32[] colors, Vector4[] uvs, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            List<RenderGeometryBuffer> holder = Buffers[materialID];
            RenderGeometryBuffer buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    uV1s = new List<Vector4>(),
                    colors = new List<Color32>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.vertices.Count;
            buffer.vertices.AddRange(verts);

            // Add UVs
            PrepareUVs(ref buffer.uV1s, buffer.vertices, initialVertexCount);
            buffer.uV1s.AddRange(uvs);

            // Add colors
            PrepareColors(ref buffer.colors, buffer.vertices, initialVertexCount);
            buffer.colors.AddRange(colors);

            // Add indices
            buffer.AddIndices(buffer.vertices.Count, backFace);
        }

        public void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j < Buffers.Length; j++)
            {
                List<RenderGeometryBuffer> holder = Buffers[j];
                Material material = (materials == null || materials.Length < 1) ? null : materials[j];

                for (int i = 0; i < holder.Count; i++)
                {
                    RenderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                    {
                        continue;
                    }

                    GameObject go = GameObjectProvider.PopObject(prefabName);
                    Assert.IsTrue(go != null);
                    if (go != null)
                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.meshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length <= 0);
                        buffer.SetupMesh(mesh, true);

                        MeshFilter filter = go.GetComponent<MeshFilter>();
                        filter.sharedMesh = null;
                        filter.sharedMesh = mesh;
                        Transform t = filter.transform;
                        t.position = position;
                        t.rotation = rotation;

                        Renderer renderer = go.GetComponent<Renderer>();
                        renderer.enabled = true;
                        renderer.sharedMaterial = material;

                        objects.Add(go);
                    }

                    buffer.Clear();
                }
            }
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation, ref Bounds bounds
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j < Buffers.Length; j++)
            {
                List<RenderGeometryBuffer> holder = Buffers[j];
                Material material = (materials == null || materials.Length < 1) ? null : materials[j];

                for (int i = 0; i < holder.Count; i++)
                {
                    RenderGeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                    {
                        continue;
                    }

                    GameObject go = GameObjectProvider.PopObject(prefabName);
                    Assert.IsTrue(go != null);
                    if (go != null)
                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.meshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length <= 0);
                        buffer.SetupMesh(mesh, false);
                        mesh.bounds = bounds;

                        MeshFilter filter = go.GetComponent<MeshFilter>();
                        filter.sharedMesh = null;
                        filter.sharedMesh = mesh;
                        Transform t = filter.transform;
                        t.position = position;
                        t.rotation = rotation;

                        Renderer renderer = go.GetComponent<Renderer>();
                        renderer.enabled = true;
                        renderer.sharedMaterial = material;

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

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh.Clear(false);
                Globals.MemPools.meshPool.Push(filter.sharedMesh);
                filter.sharedMesh = null;

                Renderer renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = null;

                GameObjectProvider.PushObject(prefabName, go);
            }

            objects.Clear();
        }
    }
}
