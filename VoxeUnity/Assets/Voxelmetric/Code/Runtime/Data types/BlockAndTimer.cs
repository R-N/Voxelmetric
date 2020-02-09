namespace Voxelmetric
{
    public struct BlockAndTimer
    {
        public Vector3Int pos;
        public float time;

        public BlockAndTimer(Vector3Int pos, float time)
        {
            this.pos = pos;
            this.time = time;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is BlockAndTimer timer && timer.pos == pos && timer.time == time;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * pos.GetHashCode();
                hash = hash * 23 * time.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(BlockAndTimer left, BlockAndTimer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockAndTimer left, BlockAndTimer right)
        {
            return !(left == right);
        }
    }
}
