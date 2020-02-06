using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Geometry.Buffers
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class ColliderGeometryBuffer
    {
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector3> vertices = new List<Vector3>();

        /// <summary>
        ///     Clear the buffers
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
        }

        /// <summary>
        ///     Returns true is there are no data in internal buffers
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // There will always be at least some triangles so it's safe to check just for them
                return triangles.Count <= 0;
            }
        }

        /// <summary>
        ///     Returns true is capacity of internal buffers is non-zero
        /// </summary>
        public bool WasUsed
        {
            get
            {
                // There will always be at least some triangles so it's safe to check just for them
                return triangles.Capacity > 0;
            }
        }

        /// <summary>
        ///     Adds triangle indices for a quad
        /// </summary>
        public void AddIndices(int offset, bool backFace)
        {
            // 0--1
            // |\ |
            // | \|
            // 3--2
            if (backFace)
            {
                triangles.Add(offset - 4); // 0
                triangles.Add(offset - 1); // 3
                triangles.Add(offset - 2); // 2

                triangles.Add(offset - 2); // 2
                triangles.Add(offset - 3); // 1
                triangles.Add(offset - 4); // 0
            }
            else
            {
                triangles.Add(offset - 4); // 0
                triangles.Add(offset - 3); // 1
                triangles.Add(offset - 2); // 2

                triangles.Add(offset - 2); // 2
                triangles.Add(offset - 1); // 3
                triangles.Add(offset - 4); // 0
            }
        }

        /// <summary>
        ///     Adds a single triangle to the buffer
        /// </summary>
        public void AddIndex(int offset)
        {
            triangles.Add(offset);
        }

        /// <summary>
        ///     Adds vertices to the the buffer.
        /// </summary>
        public void AddVertices(Vector3[] vertices)
        {
            this.vertices.AddRange(vertices);
        }

        /// <summary>
        ///     Adds a single vertex to the the buffer.
        /// </summary>
        public void AddVertex(ref Vector3 vertex)
        {
            vertices.Add(vertex);
        }

        public void SetupMesh(Mesh mesh, bool calculateBounds)
        {
            // Prepare mesh
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, calculateBounds);
            mesh.uv = null;
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors32 = null;
            mesh.tangents = null;
            mesh.normals = null;
            mesh.RecalculateNormals();
        }
    }
}
