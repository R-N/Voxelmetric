using System;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class StructureLayer : TerrainLayer
{
    protected GeneratedStructure structure;
    public float Chance { get; set; }

    protected override void SetUp(LayerConfigObject config)
    {
        string configStructure = string.Empty;
        if (config is StructureLayerConfigObject structureConfig)
        {
            configStructure = structureConfig.Structure;
        }

        // Config files for random layers MUST define these properties
        Type structureType = Type.GetType(configStructure + ", " + typeof(GeneratedStructure).Assembly, false);
        if (structureType == null)
        {
            Debug.LogError("Could not create structure " + configStructure);
            return;
        }

        structure = (GeneratedStructure)Activator.CreateInstance(structureType);
    }

    public override void Init(LayerConfigObject config)
    {
        structure.Init(world);
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        return heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        return heightSoFar;
    }

    public override void GenerateStructures(Chunk chunk, int layerIndex)
    {
        //if (chunk.pos.x!=-30 || chunk.pos.y!=30 || chunk.pos.z!=0) return;

        int minX = chunk.Pos.x;
        int maxX = chunk.Pos.x + Env.CHUNK_SIZE_1;
        int minZ = chunk.Pos.z;
        int maxZ = chunk.Pos.z + Env.CHUNK_SIZE_1;

        int structureID = 0;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                float chanceAtPos = Randomization.RandomPrecise(pos.GetHashCode(), 44);

                if (Chance > chanceAtPos)
                {
                    if (Randomization.RandomPrecise(pos.Add(1, 0, 0).GetHashCode(), 44) > chanceAtPos &&
                        Randomization.RandomPrecise(pos.Add(-1, 0, 0).GetHashCode(), 44) > chanceAtPos &&
                        Randomization.RandomPrecise(pos.Add(0, 0, 1).GetHashCode(), 44) > chanceAtPos &&
                        Randomization.RandomPrecise(pos.Add(0, 0, -1).GetHashCode(), 44) > chanceAtPos)
                    {
                        int xx = Helpers.Mod(x, Env.CHUNK_SIZE);
                        int zz = Helpers.Mod(z, Env.CHUNK_SIZE);
                        int height = Helpers.FastFloor(terrainGen.GetTerrainHeightForChunk(chunk, xx, zz));

                        if (chunk.Pos.y <= height && chunk.Pos.y + Env.CHUNK_SIZE_1 >= height)
                        {
                            Vector3Int worldPos = new Vector3Int(x, height, z);
                            structure.Build(chunk, structureID++, ref worldPos, this);
                        }
                    }
                }
            }
        }
    }
}
