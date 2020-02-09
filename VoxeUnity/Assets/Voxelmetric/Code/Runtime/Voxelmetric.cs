using System;
using System.Collections.Generic;

namespace Voxelmetric
{
    public static class Voxelmetric
    {
        //Used as a manager class with references to classes treated like singletons
        public static readonly VoxelmetricResources resources = new VoxelmetricResources();

        public static void SetBlockData(World world, ref Vector3Int pos, BlockData blockData, Action<ModifyBlockContext> onAction = null)
        {
            world.ModifyBlockData(ref pos, blockData, true, onAction);
        }

        public static void SetBlockData(World world, Vector3Int posFrom, Vector3Int posTo, BlockData blockData, Action<ModifyBlockContext> onAction = null)
        {
            world.ModifyBlockDataRanged(ref posFrom, ref posTo, blockData, true, onAction);
        }

        public static Block GetBlock(World world, ref Vector3Int pos)
        {
            return world.GetBlock(ref pos);
        }

        /// <summary>
        /// Sends a save request to all chunk currently loaded
        /// </summary>
        /// <param name="world">World holding chunks</param>
        /// <returns>List of chunks waiting to be saved.</returns>
        public static List<Chunk> SaveAll(World world)
        {
            if (world == null || !Features.useSerialization)
            {
                return null;
            }

            List<Chunk> chunksToSave = new List<Chunk>();

            foreach (Chunk chunk in world.Chunks)
            {
                // Ignore chunks that can't be saved at the moment
                if (!chunk.IsSavePossible)
                {
                    continue;
                }

                chunksToSave.Add(chunk);
                chunk.RequestSave();
            }

            return chunksToSave;
        }
    }
}
