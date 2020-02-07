using UnityEngine;
using UnityEngine.Scripting;
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
    public TextureCollection[] Textures { get; private set; }
    public Color32[] Colors { get; private set; }

    [Preserve]
    public CubeBlock() : base() { }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        Textures = ((CubeBlockConfig)config).textures;
        Colors = ((CubeBlockConfig)config).colors;
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        bool backface = DirectionUtils.IsBackface(face.side);
        int d = DirectionUtils.Get(face.side);

        Voxelmetric.Code.Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        Vector3[] verts = pools.vector3ArrayPool.PopExact(4);
        Vector4[] uvs = pools.vector4ArrayPool.PopExact(4);
        Color32[] cols = pools.color32ArrayPool.PopExact(4);

        {
            if (vertices == null)
            {
                Vector3 pos = face.pos;

                verts[0] = pos + BlockUtils.paddingOffsets[d][0];
                verts[1] = pos + BlockUtils.paddingOffsets[d][1];
                verts[2] = pos + BlockUtils.paddingOffsets[d][2];
                verts[3] = pos + BlockUtils.paddingOffsets[d][3];
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

            BlockUtils.PrepareTexture(verts, uvs, face.side, Textures, rotated);
            BlockUtils.AdjustColors(chunk, cols, face.light);

            RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;
            batcher.AddFace(face.materialID, verts, cols, uvs, backface);
        }

        pools.color32ArrayPool.Push(cols);
        pools.vector4ArrayPool.Push(uvs);
        pools.vector3ArrayPool.Push(verts);
    }
}
