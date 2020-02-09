using UnityEngine;

namespace Voxelmetric
{
    //TODO: ConnectedMeshBlock
    public class ConnectedMeshBlock : CustomMeshBlock
    {
        public new ConnectedMeshBlockConfig MeshConfig
        {
            get { return (ConnectedMeshBlockConfig)config; }
        }

        public override void OnInit(BlockProvider blockProvider)
        {
            base.OnInit(blockProvider);

            if (MeshConfig.connectsToTypes == null)
            {
                MeshConfig.connectsToTypes = new int[MeshConfig.connectsToNames.Length];
                for (int i = 0; i < MeshConfig.connectsToNames.Length; i++)
                {
                    MeshConfig.connectsToTypes[i] = blockProvider.GetType(MeshConfig.connectsToNames[i]);
                }
            }
        }

        public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
        {
            //CustomMeshBlockConfig.CustomMeshBlockData d = MeshConfig.DataDir[(int)face.side];

            //int[] tris = d.tris;
            //if (tris == null)
            //{
            //    return;
            //}

            //Vector3[] verts = d.verts;
            //Vector4[] uvs = d.uvs;
            //Voxelmetric.Code.Load_Resources.Textures.TextureCollection textures = d.textures;
            //Color32[] colors = d.colors;

            //Rect rect;
            //ChunkBlocks blocks = chunk.Blocks;

            //RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

            //Vector3Int sidePos = face.pos + face.side;
            //if (MeshConfig.connectsToSolid && blocks.Get(ref sidePos).Solid)
            //{
            //    rect = textures.GetTexture(chunk, ref face.pos, face.side);
            //    batcher.AddMeshData(face.materialID, tris, verts, colors, uvs, ref rect, face.pos);
            //}
            //else if (MeshConfig.connectsToTypes.Length != 0)
            //{
            //    int neighborType = blocks.Get(ref sidePos).Type;
            //    for (int i = 0; i < MeshConfig.connectsToTypes.Length; i++)
            //    {
            //        if (neighborType == MeshConfig.connectsToTypes[i])
            //        {
            //            rect = textures.GetTexture(chunk, ref face.pos, face.side);
            //            batcher.AddMeshData(face.materialID, tris, verts, colors, uvs, ref rect, face.pos);
            //            break;
            //        }
            //    }
            //}

            //CustomMeshBlockConfig.CustomMeshBlockData d2 = MeshConfig.Data;
            //rect = d2.textures.GetTexture(chunk, ref face.pos, Direction.down);
            //batcher.AddMeshData(face.materialID, d2.tris, d2.verts, d2.colors, d2.uvs, ref rect, face.pos);
        }

        public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
        {
            //for (int d = 0; d < 6; d++)
            //{
            //    Direction dir = DirectionUtils.Get(d);

            //    BlockFace face = new BlockFace
            //    {
            //        block = null,
            //        pos = localPos,
            //        side = dir,
            //        light = new BlockLightData(),
            //        materialID = materialID
            //    };

            //    BuildFace(chunk, null, null, ref face, false);
            //}

            //RenderGeometryBatcher batcher = chunk.RenderGeometryHandler.Batcher;

            //CustomMeshBlockConfig.CustomMeshBlockData d2 = MeshConfig.Data;
            //Rect texture = d2.textures.GetTexture(chunk, ref localPos, Direction.down);
            //batcher.AddMeshData(materialID, d2.tris, d2.verts, d2.colors, d2.uvs, ref texture, localPos);
        }
    }
}
