using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Voxelmetric
{
    public static class Utils
    {
#if PLATFORM_ARCH_64
        [StructLayout(LayoutKind.Sequential, Pack = 64, Size = 64)]
        private struct CopyDataChunk64
        {
            private readonly long l1;
            private readonly long l2;
            private readonly long l3;
            private readonly long l4;
            private readonly long l5;
            private readonly long l6;
            private readonly long l7;
            private readonly long l8;
        }

        private static readonly CopyDataChunk64 zero64 = new CopyDataChunk64();
#else
        [StructLayout(LayoutKind.Sequential, Pack = 32, Size = 32)]
        private struct CopyDataChunk32
        {
            private readonly long l1;
            private readonly long l2;
            private readonly long l3;
            private readonly long l4;
        }

        private static readonly CopyDataChunk32 zero32 = new CopyDataChunk32();
#endif

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static unsafe void MemoryCopy(byte* pDst, byte* pSrc, uint length)
        {
            uint curr = 0;
#if PLATFORM_ARCH_64
            for (; curr + 64 <= length; curr += 64)
            {
                *(CopyDataChunk64*)(pDst + curr) = *(CopyDataChunk64*)(pSrc + curr);
            }
#else
            for (; curr + 32 <= length; curr += 32)
            {
                *(CopyDataChunk32*)(pDst + curr) = *(CopyDataChunk32*)(pSrc + curr);
            }
#endif
            for (; curr + 1 <= length; ++curr)
            {
                *(pDst + curr) = *(pSrc + curr);
            }
        }

        [SecurityCritical]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static unsafe void ZeroMemory(byte* pDst, int length)
        {
            int curr = 0;
#if PLATFORM_ARCH_64
            for (;
                curr + 64 <= length;
                curr += 64)
            {
                *(CopyDataChunk64*)(pDst + curr) = zero64;
            }
#else
            for (; curr + 32 <= length; curr += 32)
            {
                *(CopyDataChunk32*)(pDst + curr) = zero32;
            }
#endif
            for (; curr + 1 <= length; ++curr)
            {
                *(pDst + curr) = 0;
            }
        }
    }
}
