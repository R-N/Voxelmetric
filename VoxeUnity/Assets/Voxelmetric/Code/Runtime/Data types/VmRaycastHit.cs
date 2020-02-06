using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Data_types
{
    public struct VmRaycastHit
    {
        public Vector3Int vector3Int;
        public Vector3Int adjacentPos;
        public Vector3 dir;
        public Vector3 scenePos;
        public World world;
        public float distance;
        public Block block;

        public override bool Equals(object obj)
        {
            if (obj != null && obj is VmRaycastHit hit)
            {
                return hit.vector3Int == vector3Int && hit.adjacentPos == adjacentPos && hit.dir == dir && hit.scenePos == scenePos
                    && hit.distance == distance && hit.block.type == block.type;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * vector3Int.GetHashCode();
                hash = hash * 23 * adjacentPos.GetHashCode();
                hash = hash * 23 * dir.GetHashCode();
                hash = hash * 23 * scenePos.GetHashCode();
                hash = hash * 23 * distance.GetHashCode();
                hash = hash * 23 * block.type.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(VmRaycastHit left, VmRaycastHit right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VmRaycastHit left, VmRaycastHit right)
        {
            return !(left == right);
        }
    }
}
