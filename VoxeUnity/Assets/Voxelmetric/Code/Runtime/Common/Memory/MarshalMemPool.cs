using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Voxelmetric
{
    public class MarshalMemPool
    {
        //! Allocated memory in bytes
        private readonly int size;
        //! Position to the beggining of the buffer
        private readonly long buffer;
        //! Current position in allocate array (m_buffer+x)
        private long pos;

        public MarshalMemPool(int initialSize)
        {
            size = initialSize;
            // Allocate all memory we can
            buffer = (long)Marshal.AllocHGlobal(initialSize);
            pos = buffer;
        }

        ~MarshalMemPool()
        {
            // Release all allocated memory in the end
            Marshal.FreeHGlobal((IntPtr)buffer);
        }

        public IntPtr Pop(int size)
        {
            // Do not take more than we can give!
            Assert.IsTrue(pos + size < buffer + this.size);

            pos += size;
            return (IntPtr)pos;
        }

        public void Push(int size)
        {
            // Do not return than we gave!
            Assert.IsTrue(pos >= buffer);

            pos -= size;
        }

        public int Left
        {
            get { return size - (int)(pos - buffer); }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", (int)(pos - buffer), size);
        }
    }
}
