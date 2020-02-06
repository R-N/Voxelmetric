using UnityEngine;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class RandomLayer : TerrainLayer
{
    private BlockData blockToPlace;
    public float Chance { get; set; }

    protected override void SetUp(LayerConfigObject config)
    {
        // Config files for random layers MUST define these properties
        Block block = world.blockProvider.GetBlock(config.BlockName);
        blockToPlace = new BlockData(block.type, block.solid);
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        Vector3 lpos = new Vector3(chunk.Pos.x + x, heightSoFar + 1f, chunk.Pos.z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        return Chance > posChance ? heightSoFar + 1 : heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        Vector3 lpos = new Vector3(chunk.Pos.x + x, heightSoFar + 1f, chunk.Pos.z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        if (Chance > posChance)
        {
            SetBlocks(chunk, x, z, (int)heightSoFar, (int)(heightSoFar + 1f), blockToPlace);

            return heightSoFar + 1;
        }

        return heightSoFar;
    }
}
