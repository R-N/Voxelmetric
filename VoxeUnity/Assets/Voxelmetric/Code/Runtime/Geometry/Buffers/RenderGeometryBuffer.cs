using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voxelmetric.Code.Geometry.Buffers
{
    public class RenderGeometryBuffer
    {
        public readonly List<int> triangles = new List<int>();
        public readonly List<Vector3> vertices = new List<Vector3>();
        public List<Vector4> uV1s;
        public List<Color32> colors;
        public List<Vector4> tangents;

        /// <summary>
        ///     Clear the buffers
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
            if (uV1s != null)
            {
                uV1s.Clear();
            }

            if (colors != null)
            {
                colors.Clear();
            }

            if (tangents != null)
            {
                tangents.Clear();
            }
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

        public bool HasUV1
        {
            get { return uV1s != null; }
        }

        public bool HasColors
        {
            get { return uV1s != null; }
        }

        public bool HasTangents
        {
            get { return tangents != null; }
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

        public void SetupMesh(Mesh mesh, bool calculateBounds)
        {
            // Vertices & indices
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, calculateBounds);

            // UVs
            mesh.uv = null;
            if (uV1s != null)
            {
                Assert.IsTrue(uV1s.Count <= vertices.Count);
                if (uV1s.Count < vertices.Count)
                {
                    // Fill in UVs if necessary
                    if (uV1s.Capacity < vertices.Count)
                    {
                        uV1s.Capacity = vertices.Count;
                    }

                    int diff = vertices.Count - uV1s.Count;
                    for (int i = 0; i < diff; i++)
                    {
                        uV1s.Add(Vector4.zero);
                    }
                }
                mesh.SetUVs(0, uV1s);
            }
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;

            // Colors
            mesh.colors = null;
            if (colors != null)
            {
                Assert.IsTrue(colors.Count <= vertices.Count);
                if (colors.Count < vertices.Count)
                {
                    // Fill in colors if necessary
                    if (colors.Capacity < vertices.Count)
                    {
                        colors.Capacity = vertices.Count;
                    }

                    int diff = vertices.Count - colors.Count;
                    for (int i = 0; i < diff; i++)
                    {
                        colors.Add(new Color32(255, 255, 255, 255));
                    }
                }
                mesh.SetColors(colors);
            }
            else
            {
                // TODO: Use white color if no color data is supplied?
            }

            // Tangents
            mesh.tangents = null;
            if (tangents != null)
            {
                Assert.IsTrue(tangents.Count <= vertices.Count);
                if (tangents.Count < vertices.Count)
                {
                    // Fill in tangents if necessary
                    if (tangents.Capacity < vertices.Count)
                    {
                        tangents.Capacity = vertices.Count;
                    }

                    int diff = vertices.Count - tangents.Count;
                    for (int i = 0; i < diff; i++)
                    {
                        tangents.Add(Vector4.zero);
                    }
                }
                mesh.SetTangents(tangents);
            }

            // Normals
            mesh.normals = null;
            mesh.RecalculateNormals();
        }
    }
}
