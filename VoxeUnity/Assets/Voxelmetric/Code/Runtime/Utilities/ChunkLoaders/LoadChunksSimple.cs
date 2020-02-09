using UnityEngine;
using UnityEngine.Profiling;

namespace Voxelmetric
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    public class LoadChunksSimple : LoadChunksBase
    {
        protected override void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ)
        {
            if (rangeX == 0 || rangeY == 0 || rangeZ == 0)
            {
                return;
            }

            Profiler.BeginSample("Cull");

            bool isLast = rangeX == 1 && rangeY == 1 && rangeZ == 1;

            int wx = x * Env.CHUNK_SIZE;
            int wy = y * Env.CHUNK_SIZE;
            int wz = z * Env.CHUNK_SIZE;

            // Stop if there is no further subdivision possible
            if (isLast)
            {
                Profiler.BeginSample("CullLast");

                // Update chunk's visibility information
                Vector3Int chunkPos = new Vector3Int(wx, wy, wz);
                Chunk chunk = world.GetChunk(ref chunkPos);
                if (chunk != null)
                {
                    // Update visibility information
                    bool isVisible = Planes.TestPlanesAABB(cameraPlanes, ref chunk.worldBounds);
                    chunk.NeedsRenderGeometry = isVisible;
                    chunk.PossiblyVisible = isVisible;
                }

                Profiler.EndSample(); // CullLast
                Profiler.EndSample(); // Cull
                return;
            }

            int rx = rangeX * Env.CHUNK_SIZE;
            int ry = rangeY * Env.CHUNK_SIZE;
            int rz = rangeZ * Env.CHUNK_SIZE;

            // Check whether the bouding box lies inside the camera's frustum
            AABB bounds2 = new AABB(wx, wy, wz, wx + rx, wy + ry, wz + rz);
            Planes.TestPlanesResult res = Planes.TestPlanesAABB2(cameraPlanes, ref bounds2);

            #region Full invisibility

            // Everything is invisible by default

            if (res == Planes.TestPlanesResult.Outside)
            {
                Profiler.EndSample();
                return;
            }

            #endregion

            #region Full visibility

            if (res == Planes.TestPlanesResult.Inside)
            {
                Profiler.BeginSample("CullFullVisible");

                // Full visibility. All chunks in this area need to be made visible
                for (int cy = wy; cy < wy + ry; cy += Env.CHUNK_SIZE)
                {
                    for (int cz = wz; cz < wz + rz; cz += Env.CHUNK_SIZE)
                    {
                        for (int cx = wx; cx < wx + rx; cx += Env.CHUNK_SIZE)
                        {
                            // Update chunk's visibility information
                            Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                            Chunk chunk = world.GetChunk(ref chunkPos);
                            if (chunk == null)
                            {
                                continue;
                            }

                            // Update visibility information
                            chunk.PossiblyVisible = true;
                            chunk.NeedsRenderGeometry = true;
                        }
                    }
                }

                Profiler.EndSample(); // CullLast
                Profiler.EndSample(); // Cull
                return;
            }

            #endregion

            #region Partial visibility

            int offX = rangeX;
            if (rangeX > 1)
            {
                offX = rangeX >> 1;
                rangeX -= offX;
            }
            int offY = rangeY;
            if (rangeY > 1)
            {
                offY = rangeY >> 1;
                rangeY -= offY;
            }
            int offZ = rangeZ;
            if (rangeZ > 1)
            {
                offZ = rangeZ >> 1;
                rangeZ -= offZ;
            }

            Profiler.EndSample();

            // Subdivide if possible
            // TODO: Avoid the recursion
            UpdateVisibility(x, y, z, offX, offY, offZ);
            UpdateVisibility(x + offX, y, z, rangeX, offY, offZ);
            UpdateVisibility(x, y, z + offZ, offX, offY, rangeZ);
            UpdateVisibility(x + offX, y, z + offZ, rangeX, offY, rangeZ);
            UpdateVisibility(x, y + offY, z, offX, rangeY, offZ);
            UpdateVisibility(x + offX, y + offY, z, rangeX, rangeY, offZ);
            UpdateVisibility(x, y + offY, z + offZ, offX, rangeY, rangeZ);
            UpdateVisibility(x + offX, y + offY, z + offZ, rangeX, rangeY, rangeZ);

            #endregion
        }

        protected override void OnProcessChunk(Chunk chunk)
        {
            Profiler.BeginSample("ProcessChunk");

            int xd = (viewerPos.x - chunk.Pos.x) / Env.CHUNK_SIZE;
            int yd = (viewerPos.y - chunk.Pos.y) / Env.CHUNK_SIZE;
            int zd = (viewerPos.z - chunk.Pos.z) / Env.CHUNK_SIZE;

            // Remove the chunk if it is too far away
            if (
                !ChunkLoadOrder.CheckXZ(xd, zd, horizontalChunkLoadRadius) ||
                !ChunkLoadOrder.CheckY(yd, verticalChunkLoadRadius)
                )
            {
                chunk.RequestRemoval();
            }
            else
            {
                // Dummy collider example - create a collider for chunks directly surrounding the viewer
                chunk.NeedsColliderGeometry = Helpers.Abs(xd) <= 1 && Helpers.Abs(zd) <= 1;

                if (!useFrustumCulling)
                {
                    // Update visibility information
                    chunk.NeedsRenderGeometry = true;
                    chunk.PossiblyVisible = true;
                }
            }

            Profiler.EndSample();
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled)
            {
                return;
            }

            float size = Env.CHUNK_SIZE * Env.BLOCK_SIZE;
            float halfSize = size * 0.5f;
            float smallSize = size * 0.25f;

            if (world != null && (diag_DrawWorldBounds || diag_DrawLoadRange))
            {
                foreach (Chunk chunk in updateRequests)
                {
                    if (diag_DrawWorldBounds)
                    {
                        // Make central chunks more apparent by using yellow color
                        bool isCentral = chunk.Pos.x == viewerPos.x || chunk.Pos.y == viewerPos.y || chunk.Pos.z == viewerPos.z;
                        Gizmos.color = isCentral ? Color.yellow : Color.blue;
                        Vector3 chunkCenter = new Vector3(
                            chunk.Pos.x + (Env.CHUNK_SIZE >> 1),
                            chunk.Pos.y + (Env.CHUNK_SIZE >> 1),
                            chunk.Pos.z + (Env.CHUNK_SIZE >> 1)
                            );
                        Vector3 chunkSize = new Vector3(Env.CHUNK_SIZE, Env.CHUNK_SIZE, Env.CHUNK_SIZE);
                        Gizmos.DrawWireCube(chunkCenter, chunkSize);
                    }

                    if (diag_DrawLoadRange)
                    {
                        int xd = (viewerPos.x - chunk.Pos.x) / Env.CHUNK_SIZE;
                        int zd = (viewerPos.z - chunk.Pos.z) / Env.CHUNK_SIZE;

                        if (ChunkLoadOrder.CheckXZ(xd, zd, horizontalChunkLoadRadius))
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x + halfSize, 0, chunk.Pos.z + halfSize),
                                new Vector3(size - 1f, 0, size - 1f)
                                );
                        }
                        else
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x + halfSize, 0, chunk.Pos.z + halfSize),
                                new Vector3(size - 1f, 0, size - 1f)
                                );
                        }

                        // Show generated chunks
                        if (chunk.IsStateCompleted(ChunkState.Generate))
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x + halfSize, 0, chunk.Pos.z + halfSize),
                                new Vector3(smallSize - 0.05f, 0, smallSize - 0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}
