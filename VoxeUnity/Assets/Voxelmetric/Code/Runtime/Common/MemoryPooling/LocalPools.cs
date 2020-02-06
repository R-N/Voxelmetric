using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Utilities.Noise;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools
    {
        public NoiseItem[] noiseItems;

        public readonly ArrayPoolCollection<Vector3> vector3ArrayPool = new ArrayPoolCollection<Vector3>(128);

        public readonly ArrayPoolCollection<Vector2> vector2ArrayPool = new ArrayPoolCollection<Vector2>(128);

        public readonly ArrayPoolCollection<Vector4> vector4ArrayPool = new ArrayPoolCollection<Vector4>(128);

        public readonly ArrayPoolCollection<Color32> color32ArrayPool = new ArrayPoolCollection<Color32>(128);

        public readonly ArrayPoolCollection<bool> boolArrayPool = new ArrayPoolCollection<bool>(128);

        public readonly ArrayPoolCollection<byte> byteArrayPool = new ArrayPoolCollection<byte>(128);

        public readonly ArrayPoolCollection<int> intArrayPool = new ArrayPoolCollection<int>(128);

        public readonly ArrayPoolCollection<float> floatArrayPool = new ArrayPoolCollection<float>(128);

        public readonly ArrayPoolCollection<BlockFace> blockFaceArrayPool = new ArrayPoolCollection<BlockFace>(128);

        public readonly MarshalMemPool marshaledPool = new MarshalMemPool(Env.CHUNK_SIZE_WITH_PADDING_POW_3 * 8); // Set to a multiple of chunk volume

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.ConcatFormat("Vec3Arr={0}", vector3ArrayPool.ToString());
            sb.ConcatFormat(",Vec2Arr={0}", vector2ArrayPool.ToString());
            sb.ConcatFormat(",C32Arr={0}", color32ArrayPool.ToString());
            sb.ConcatFormat(",BoolArr={0}", boolArrayPool.ToString());
            sb.ConcatFormat(",ByteArr={0}", byteArrayPool.ToString());
            sb.ConcatFormat(",IntArr={0}", intArrayPool.ToString());
            sb.ConcatFormat(",FloatArr={0}", floatArrayPool.ToString());
            sb.ConcatFormat(",BlockFaceArr={0}", blockFaceArrayPool.ToString());
            sb.ConcatFormat(",MarshaledBLeft={0}", marshaledPool.ToString());
            return sb.ToString();
        }
    }
}
