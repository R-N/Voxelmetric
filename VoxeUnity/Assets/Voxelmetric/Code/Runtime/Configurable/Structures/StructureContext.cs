namespace Voxelmetric
{
    public abstract class StructureContext
    {
        //! A chunk this structure belongs to
        public Vector3Int chunkPos;
        //! ID of associate structure
        public readonly int id;

        protected StructureContext(int id, ref Vector3Int chunkPos)
        {
            this.chunkPos = chunkPos;
            this.id = id;
        }

        public abstract void Apply(Chunk chunk);
    }
}
