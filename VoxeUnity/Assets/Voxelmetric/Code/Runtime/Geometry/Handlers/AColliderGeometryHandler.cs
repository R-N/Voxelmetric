using UnityEngine;

namespace Voxelmetric
{
    public abstract class AColliderGeometryHandler : IGeometryHandler
    {
        public ColliderGeometryBatcher Batcher { get; private set; }

        protected AColliderGeometryHandler(string prefabName, PhysicMaterial[] materials)
        {
            Batcher = new ColliderGeometryBatcher(prefabName, materials);
        }

        public void Reset()
        {
            Batcher.Reset();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public abstract void Build();

        public abstract void Commit();
    }
}
