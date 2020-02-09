using UnityEngine;

namespace Voxelmetric
{
    public class ChunkRenderGeometryHandler : ARenderGeometryHandler
    {
        private const string POOL_ENTRY_NAME = "Renderable";
        private readonly Chunk chunk;

        public ChunkRenderGeometryHandler(Chunk chunk, Material[] materials) : base(POOL_ENTRY_NAME, materials)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build()
        {
            Globals.TerrainMeshBuilder.Build(chunk, out chunk.minBounds, out chunk.maxBounds);
        }

        public override void Commit()
        {
            if (chunk.Blocks.nonEmptyBlocks <= 0)
            {
                return;
            }

            // Prepare a bounding box for our geometry
            int minX = chunk.minBounds & 0xFF;
            int minY = (chunk.minBounds >> 8) & 0xFF;
            int minZ = (chunk.minBounds >> 16) & 0xFF;
            int maxX = chunk.maxBounds & 0xFF;
            int maxY = (chunk.maxBounds >> 8) & 0xFF;
            int maxZ = (chunk.maxBounds >> 16) & 0xFF;
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
                , chunk.Pos.ToString()
#endif
                );
        }
    }
}
