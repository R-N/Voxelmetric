using System.IO;

namespace Voxelmetric
{
    public interface IBinarizable
    {
        bool Binarize(BinaryWriter bw);
        bool Debinarize(BinaryReader br);
    }
}
