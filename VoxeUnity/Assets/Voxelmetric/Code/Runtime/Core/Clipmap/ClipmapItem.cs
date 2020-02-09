namespace Voxelmetric
{
    public struct ClipmapItem
    {
        public int lod;
        public bool isInVisibleRange;

        public override bool Equals(object obj)
        {
            return obj != null && obj is ClipmapItem item && item.lod == lod && item.isInVisibleRange == isInVisibleRange;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + lod.GetHashCode();
                hash = hash * 23 + isInVisibleRange.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(ClipmapItem left, ClipmapItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ClipmapItem left, ClipmapItem right)
        {
            return !(left == right);
        }
    }
}
