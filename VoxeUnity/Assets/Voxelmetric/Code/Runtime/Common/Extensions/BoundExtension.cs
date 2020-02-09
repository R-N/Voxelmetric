using UnityEngine;

namespace Voxelmetric
{
    public static class BoundExtension
    {
        public static bool Contains(this Bounds bounds, Bounds target)
        {
            return bounds.Contains(target.min) && bounds.Contains(target.max);
        }
    }
}
