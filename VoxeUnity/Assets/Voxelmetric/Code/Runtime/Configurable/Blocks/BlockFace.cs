namespace Voxelmetric
{
    public struct BlockFace // 26B (32B)
    {
        public Vector3Int pos; // 12B
        public Block block; // 8B
        public int materialID; // 4B
        public Direction side; // 1B
        public BlockLightData light; //1B

        public override bool Equals(object obj)
        {
            if (obj != null && obj is BlockFace face)
            {
                return face.pos.Equals(pos) && face.block.type == block.type && face.materialID == materialID && face.side == side && face.light.Equals(light);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * pos.GetHashCode();
                hash = hash * 23 * block.type.GetHashCode();
                hash = hash * 23 * materialID.GetHashCode();
                hash = hash * 23 * side.GetHashCode();
                hash = hash * 23 * light.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(BlockFace left, BlockFace right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockFace left, BlockFace right)
        {
            return !(left == right);
        }
    }
}
