using UnityEngine;

namespace Voxelmetric
{
    public class ChunkColliderGeometryHandler : AColliderGeometryHandler
    {
        private const string POOL_ENTRY_NAME = "Collidable";
        private readonly Chunk chunk;

        public ChunkColliderGeometryHandler(Chunk chunk, PhysicMaterial[] materials) : base(POOL_ENTRY_NAME, materials)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build()
        {
            Globals.TerrainMeshColliderBuilder.Build(chunk, out chunk.minBoundsC, out chunk.maxBoundsC);
        }

        public override void Commit()
        {
            if (chunk.Blocks.nonEmptyBlocks <= 0)
            {
                return;
            }

            // Prepare a bounding box for our geometry
            int minX = chunk.minBoundsC & 0xFF;
            int minY = (chunk.minBoundsC >> 8) & 0xFF;
            int minZ = (chunk.minBoundsC >> 16) & 0xFF;
            int maxX = chunk.maxBoundsC & 0xFF;
            int maxY = (chunk.maxBoundsC >> 8) & 0xFF;
            int maxZ = (chunk.maxBoundsC >> 16) & 0xFF;
            Bounds bounds = new Bounds(
                new Vector3((minX + maxX) >> 1, (minY + maxY) >> 1, (minZ + maxZ) >> 1),
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ)
            );

            // Generate a mesh
            Batcher.Commit(
                chunk.World.transform.rotation * chunk.Pos + chunk.World.transform.position,
                chunk.World.transform.rotation,
                ref bounds
#if DEBUG
                , chunk.Pos + "C"
#endif
                );
        }
    }
}
