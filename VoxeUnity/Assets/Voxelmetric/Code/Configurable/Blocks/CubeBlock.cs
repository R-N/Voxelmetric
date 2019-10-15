﻿using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;

public class CubeBlock : Block
{
    public TextureCollection[] textures { get; private set; }
    public Color32[] Colors { get; private set; }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        textures = ((CubeBlockConfig)Config).textures;
        Colors = ((CubeBlockConfig)Config).colors;
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        bool backface = DirectionUtils.IsBackface(face.side);
        int d = DirectionUtils.Get(face.side);

        Voxelmetric.Code.Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        Vector3[] verts = pools.Vector3ArrayPool.PopExact(4);
        Vector2[] uvs = pools.Vector2ArrayPool.PopExact(4);
        Color32[] cols = pools.Color32ArrayPool.PopExact(4);

        {
            if (vertices == null)
            {
                Vector3 pos = face.pos;

                verts[0] = pos + BlockUtils.PaddingOffsets[d][0];
                verts[1] = pos + BlockUtils.PaddingOffsets[d][1];
                verts[2] = pos + BlockUtils.PaddingOffsets[d][2];
                verts[3] = pos + BlockUtils.PaddingOffsets[d][3];
            }
            else
            {
                verts[0] = vertices[0];
                verts[1] = vertices[1];
                verts[2] = vertices[2];
                verts[3] = vertices[3];
            }

            cols[0] = Colors[d];
            cols[1] = Colors[d];
            cols[2] = Colors[d];
            cols[3] = Colors[d];

            BlockUtils.PrepareTexture(chunk, ref face.pos, uvs, face.side, textures, rotated);
            BlockUtils.AdjustColors(chunk, cols, face.light);

            RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;
            batcher.AddFace(face.materialID, verts, cols, uvs, backface);
        }

        pools.Color32ArrayPool.Push(cols);
        pools.Vector2ArrayPool.Push(uvs);
        pools.Vector3ArrayPool.Push(verts);
    }
}
