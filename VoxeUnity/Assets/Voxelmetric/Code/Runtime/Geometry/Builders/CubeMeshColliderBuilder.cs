using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders
{
    /// <summary>
    /// Generates a cubical collider mesh with merged faces
    /// </summary>
    public class CubeMeshColliderBuilder : MergedFacesMeshBuilder
    {
        public CubeMeshColliderBuilder(float scale, int sideSize) : base(scale, sideSize)
        {
        }

        protected override bool CanConsiderBlock(Block block)
        {
            return block.CanCollide;
        }

        protected override bool CanCreateBox(Block block, Block neighbor)
        {
            return block.physicMaterialID == neighbor.physicMaterialID;
        }

        protected override void BuildBox(Chunk chunk, Block block, int minX, int minY, int minZ, int maxX, int maxY,
            int maxZ)
        {
            // Order of vertices when building faces:
            //     1--2
            //     |  |
            //     |  |
            //     0--3

            int sizeWithPadding = sideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            ChunkBlocks blocks = chunk.Blocks;
            Chunk[] listeners = chunk.Neighbors;

            // Custom blocks have their own rules
            // TODO: Implement custom block colliders
            /*if (block.Custom)
            {
                for (int yy = minY; yy < maxY; yy++)
                {
                    for (int zz = minZ; zz < maxZ; zz++)
                    {
                        for (int xx = minX; xx < maxX; xx++)
                        {
                            ... // build collider here
                        }
                    }
                }

                return;
            }*/

            Vector3[] vertexData = pools.vector3ArrayPool.PopExact(4);
            bool[] mask = pools.boolArrayPool.PopExact(sideSize * sideSize);

            int n, w, h, l, k, maskIndex;

            #region Top face

            if (listeners[(int)Direction.up] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxY != sideSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, maxY, minZ, pow);
                int zOffset = sizeWithPadding - maxX + minX;

                // Build the mask
                for (int zz = minZ; zz < maxZ; ++zz, neighborIndex += zOffset)
                {
                    n = minX + zz * sideSize;
                    for (int xx = minX; xx < maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz < maxZ; ++zz)
                {
                    n = minX + zz * sideSize;
                    for (int xx = minX; xx < maxX;)
                    {
                        if (mask[n] == false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; zz + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, maxY, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.up][0];
                            vertexData[1] = new Vector3(xx, maxY, zz + h) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.up][1];
                            vertexData[2] = new Vector3(xx + w, maxY, zz + h) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.up][2];
                            vertexData[3] = new Vector3(xx + w, maxY, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.up][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.up));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        xx += w;
                        n += w;
                    }
                }
            }

            #endregion

            #region Bottom face

            if (listeners[(int)Direction.down] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minY != 0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY - 1, minZ, pow);
                int zOffset = sizeWithPadding - maxX + minX;

                // Build the mask
                for (int zz = minZ; zz < maxZ; ++zz, neighborIndex += zOffset)
                {
                    n = minX + zz * sideSize;
                    for (int xx = minX; xx < maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz < maxZ; ++zz)
                {
                    n = minX + zz * sideSize;
                    for (int xx = minX; xx < maxX;)
                    {
                        if (mask[n] == false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; zz + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, minY, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.down][0];
                            vertexData[1] = new Vector3(xx, minY, zz + h) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.down][1];
                            vertexData[2] = new Vector3(xx + w, minY, zz + h) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.down][2];
                            vertexData[3] = new Vector3(xx + w, minY, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.down][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.down));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        xx += w;
                        n += w;
                    }
                }
            }

            #endregion

            #region Right face

            if (listeners[(int)Direction.east] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxX != sideSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(maxX, minY, minZ, pow);
                int yOffset = sizeWithPaddingPow2 - (maxZ - minZ) * sizeWithPadding;

                // Build the mask
                for (int yy = minY; yy < maxY; ++yy, neighborIndex += yOffset)
                {
                    n = minZ + yy * sideSize;
                    for (int zz = minZ; zz < maxZ; ++zz, ++n, neighborIndex += sizeWithPadding)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy < maxY; ++yy)
                {
                    n = minZ + yy * sideSize;
                    for (int zz = minZ; zz < maxZ;)
                    {
                        if (mask[n] == false)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; zz + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; yy + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(maxX, yy, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.east][0];
                            vertexData[1] = new Vector3(maxX, yy + h, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.east][1];
                            vertexData[2] = new Vector3(maxX, yy + h, zz + w) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.east][2];
                            vertexData[3] = new Vector3(maxX, yy, zz + w) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.east][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.east));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        zz += w;
                        n += w;
                    }
                }
            }

            #endregion

            #region Left face

            if (listeners[(int)Direction.west] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minX != 0)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX - 1, minY, minZ, pow);
                int yOffset = sizeWithPaddingPow2 - (maxZ - minZ) * sizeWithPadding;

                // Build the mask
                for (int yy = minY; yy < maxY; ++yy, neighborIndex += yOffset)
                {
                    n = minZ + yy * sideSize;
                    for (int zz = minZ; zz < maxZ; ++zz, ++n, neighborIndex += sizeWithPadding)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy < maxY; ++yy)
                {
                    n = minZ + yy * sideSize;
                    for (int zz = minZ; zz < maxZ;)
                    {
                        if (mask[n] == false)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; zz + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; yy + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(minX, yy, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.west][0];
                            vertexData[1] = new Vector3(minX, yy + h, zz) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.west][1];
                            vertexData[2] = new Vector3(minX, yy + h, zz + w) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.west][2];
                            vertexData[3] = new Vector3(minX, yy, zz + w) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.west][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.west));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        zz += w;
                        n += w;
                    }
                }
            }

            #endregion

            #region Front face

            if (listeners[(int)Direction.north] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxZ != sideSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY, maxZ, pow);
                int yOffset = sizeWithPaddingPow2 - maxX + minX;

                // Build the mask
                for (int yy = minY; yy < maxY; ++yy, neighborIndex += yOffset)
                {
                    n = minX + yy * sideSize;
                    for (int xx = minX; xx < maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy < maxY; ++yy)
                {
                    n = minX + yy * sideSize;
                    for (int xx = minX; xx < maxX;)
                    {
                        if (mask[n] == false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; yy + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, yy, maxZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.north][0];
                            vertexData[1] = new Vector3(xx, yy + h, maxZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.north][1];
                            vertexData[2] = new Vector3(xx + w, yy + h, maxZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.north][2];
                            vertexData[3] = new Vector3(xx + w, yy, maxZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.north][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.north));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        xx += w;
                        n += w;
                    }
                }
            }

            #endregion

            #region Back face

            if (listeners[(int)Direction.south] != null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minZ != 0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY, minZ - 1, pow);
                int yOffset = sizeWithPaddingPow2 - maxX + minX;

                // Build the mask
                for (int yy = minY; yy < maxY; ++yy, neighborIndex += yOffset)
                {
                    n = minX + yy * sideSize;
                    for (int xx = minX; xx < maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                        {
                            mask[n] = true;
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy < maxY; ++yy)
                {
                    n = minX + yy * sideSize;
                    for (int xx = minX; xx < maxX;)
                    {
                        if (mask[n] == false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx + w < sideSize && mask[n + w] == m; w++)
                        {
                        }

                        // Compute height
                        for (h = 1; yy + h < sideSize; h++)
                        {
                            for (k = 0; k < w; k++)
                            {
                                maskIndex = n + k + h * sideSize;
                                if (mask[maskIndex] == false || mask[maskIndex] != m)
                                {
                                    goto cont;
                                }
                            }
                        }
                    cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, yy, minZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.south][0];
                            vertexData[1] = new Vector3(xx, yy + h, minZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.south][1];
                            vertexData[2] = new Vector3(xx + w, yy + h, minZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.south][2];
                            vertexData[3] = new Vector3(xx + w, yy, minZ) * scale +
                                            BlockUtils.paddingOffsets[(int)Direction.south][3];

                            chunk.ColliderGeometryHandler.Batcher.AddFace(block.physicMaterialID, vertexData, DirectionUtils.IsBackface(Direction.south));
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l < h; ++l)
                        {
                            maskIndex = n + l * sideSize;
                            for (k = 0; k < w; ++k, ++maskIndex)
                            {
                                mask[maskIndex] = false;
                            }
                        }

                        xx += w;
                        n += w;
                    }
                }
            }

            #endregion

            pools.boolArrayPool.Push(mask);
            pools.vector3ArrayPool.Push(vertexData);
        }
    }
}
