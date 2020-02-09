using System;

namespace Voxelmetric
{
    [Flags]
    public enum ChunkPoolItemState : ushort
    {
        None = 0,

        TaskPI = 0x01,  //! A task pool item
        ThreadPI = 0x02, //! A thread pool item
    }
}
