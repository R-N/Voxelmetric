using UnityEngine;

namespace Voxelmetric
{
    public struct AABB
    {
        public readonly float minX;
        public readonly float minY;
        public readonly float minZ;
        public readonly float maxX;
        public readonly float maxY;
        public readonly float maxZ;

        public AABB(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            minX = x1;
            minY = y1;
            minZ = z1;
            maxX = x2;
            maxY = y2;
            maxZ = z2;
        }

        public bool IsInside(float x, float y, float z)
        {
            return x > minX && x < maxX &&
                   y > minY && y < maxY &&
                   z > minY && z < maxZ;
        }

        public bool IsInside(ref Vector3 pos)
        {
            return pos.x > minX && pos.x < maxX &&
                   pos.y > minY && pos.y < maxY &&
                   pos.z > minZ && pos.z < maxZ;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is AABB item)
            {
                return item.minX == minX && item.minY == minY && item.minZ == minZ &&
                    item.maxX == maxX && item.maxY == maxY && item.maxZ == maxZ;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + minX.GetHashCode();
                hash = hash * 23 + minY.GetHashCode();
                hash = hash * 23 + minZ.GetHashCode();
                hash = hash * 23 + maxX.GetHashCode();
                hash = hash * 23 + maxY.GetHashCode();
                hash = hash * 23 + maxZ.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(AABB left, AABB right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AABB left, AABB right)
        {
            return !(left == right);
        }
    }
}
