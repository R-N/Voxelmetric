namespace Voxelmetric
{
    public sealed class ModifyOpCuboid : ModifyOpRange
    {
        /// <summary>
        /// Performs a ranged set operation of cuboid shape
        /// </summary>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="min">Starting positon in local chunk coordinates</param>
        /// <param name="max">Ending position in local chunk coordinates</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public ModifyOpCuboid(BlockData blockData, Vector3Int min, Vector3Int max, bool setBlockModified,
            ModifyBlockContext parentContext = null) : base(blockData, min, max, setBlockModified, parentContext)
        {
        }

        protected override void OnSetBlocks(ChunkBlocks blocks)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(min.x, min.y, min.z);
            int yOffset = Env.CHUNK_SIZE_WITH_PADDING_POW_2 - (max.z - min.z + 1) * Env.CHUNK_SIZE_WITH_PADDING;
            int zOffset = Env.CHUNK_SIZE_WITH_PADDING - (max.x - min.x + 1);

            for (int y = min.y; y <= max.y; ++y, index += yOffset)
            {
                for (int z = min.z; z <= max.z; ++z, index += zOffset)
                {
                    for (int x = min.x; x <= max.x; ++x, ++index)
                    {
                        blocks.ProcessSetBlock(blockData, index, setBlockModified);
                    }
                }
            }
        }

        protected override void OnSetBlocksRaw(ChunkBlocks blocks, ref Vector3Int from, ref Vector3Int to)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(from.x, from.y, from.z);
            int yOffset = Env.CHUNK_SIZE_WITH_PADDING_POW_2 - (to.z - from.z + 1) * Env.CHUNK_SIZE_WITH_PADDING;
            int zOffset = Env.CHUNK_SIZE_WITH_PADDING - (to.x - from.x + 1);

            for (int y = from.y; y <= to.y; ++y, index += yOffset)
            {
                for (int z = from.z; z <= to.z; ++z, index += zOffset)
                {
                    for (int x = from.x; x <= to.x; ++x, ++index)
                    {
                        blocks.SetRaw(index, blockData);
                    }
                }
            }
        }
    }
}
