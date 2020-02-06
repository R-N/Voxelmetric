using UnityEngine;
using UnityEngine.Scripting;
using Voxelmetric.Code;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;

public class SimpleBlock : Block
{
    [Preserve]
    public SimpleBlock() : base() { }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);

        Voxelmetric.Code.Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        Vector3[] verts = pools.vector3ArrayPool.PopExact(4);
        Color32[] cols = pools.color32ArrayPool.PopExact(4);

        {
            verts[0] = vertices[0];
            verts[1] = vertices[1];
            verts[2] = vertices[2];
            verts[3] = vertices[3];

            cols[0] = palette[face.block.type];
            cols[1] = palette[face.block.type];
            cols[2] = palette[face.block.type];
            cols[3] = palette[face.block.type];

            BlockUtils.AdjustColors(chunk, cols, face.light);

            RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;
            batcher.AddFace(face.materialID, verts, cols, backFace);
        }

        pools.color32ArrayPool.Push(cols);
        pools.vector3ArrayPool.Push(verts);
    }
}
