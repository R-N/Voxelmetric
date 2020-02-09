using UnityEngine;

namespace Voxelmetric
{
    public interface IGeometryBatcher
    {
        void Clear();

        void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
        );

        void Commit(Vector3 position, Quaternion rotation, ref Bounds bounds
#if DEBUG
            , string debugName = null
#endif
            );

        bool Enabled { get; set; }
    }
}
