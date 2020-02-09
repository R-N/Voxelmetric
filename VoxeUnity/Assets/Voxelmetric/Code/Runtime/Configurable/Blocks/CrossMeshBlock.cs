using UnityEngine;
using UnityEngine.Scripting;

namespace Voxelmetric
{
    public class CrossMeshBlock : Block
    {
        private const float COEF = 1.0f / 64.0f;

        public TextureCollection Texture { get { return ((CrossMeshBlockConfig)config).texture; } }
        public Color32 Color { get { return ((CrossMeshBlockConfig)config).color; } }

        [Preserve]
        public CrossMeshBlock() : base() { }

        public override void OnInit(BlockProvider blockProvider)
        {
            base.OnInit(blockProvider);

            custom = true;
        }

        public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
        {
            LocalPools pools = Globals.WorkPool.GetPool(chunk.ThreadID);
            RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

            // Using the block positions hash is much better for random numbers than saving the offset and height in the block data
            int hash = localPos.GetHashCode();

            hash *= 39;
            float offsetX = (hash & 63) * COEF * Env.BLOCK_SIZE_HALF - Env.BLOCK_SIZE_HALF * 0.5f;

            hash *= 39;
            float offsetZ = (hash & 63) * COEF * Env.BLOCK_SIZE_HALF - Env.BLOCK_SIZE_HALF * 0.5f;

            // Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
            Vector3 vPos = localPos;
            vPos += new Vector3(offsetX, 0, offsetZ);

            float x1 = vPos.x - BlockUtils.blockPadding;
            float x2 = vPos.x + BlockUtils.blockPadding + Env.BLOCK_SIZE;
            float y1 = vPos.y - BlockUtils.blockPadding;
            float y2 = vPos.y + BlockUtils.blockPadding + Env.BLOCK_SIZE;
            float z1 = vPos.z - BlockUtils.blockPadding;
            float z2 = vPos.z + BlockUtils.blockPadding + Env.BLOCK_SIZE;

            Vector3[] verts = pools.vector3ArrayPool.PopExact(4);
            Vector4[] uvs = pools.vector4ArrayPool.PopExact(4);
            Color32[] colors = pools.color32ArrayPool.PopExact(4);

            {
                colors[0] = Color;
                colors[1] = Color;
                colors[2] = Color;
                colors[3] = Color;
            }

            {
                verts[0] = new Vector3(x1, y1, z2);
                verts[1] = new Vector3(x1, y2, z2);
                verts[2] = new Vector3(x2, y2, z1);
                verts[3] = new Vector3(x2, y1, z1);
                // Needs to have some vertices before being able to get a texture.
                BlockUtils.PrepareTexture(verts, uvs, Direction.north, Texture, false);
                batcher.AddFace(materialID, verts, colors, uvs, false);
            }
            {
                verts[0] = new Vector3(x2, y1, z1);
                verts[1] = new Vector3(x2, y2, z1);
                verts[2] = new Vector3(x1, y2, z2);
                verts[3] = new Vector3(x1, y1, z2);
                batcher.AddFace(materialID, verts, colors, uvs, false);
            }
            {
                verts[0] = new Vector3(x2, y1, z2);
                verts[1] = new Vector3(x2, y2, z2);
                verts[2] = new Vector3(x1, y2, z1);
                verts[3] = new Vector3(x1, y1, z1);
                batcher.AddFace(materialID, verts, colors, uvs, false);
            }
            {
                verts[0] = new Vector3(x1, y1, z1);
                verts[1] = new Vector3(x1, y2, z1);
                verts[2] = new Vector3(x2, y2, z2);
                verts[3] = new Vector3(x2, y1, z2);
                batcher.AddFace(materialID, verts, colors, uvs, false);
            }

            pools.color32ArrayPool.Push(colors);
            pools.vector4ArrayPool.Push(uvs);
            pools.vector3ArrayPool.Push(verts);
        }
    }
}
