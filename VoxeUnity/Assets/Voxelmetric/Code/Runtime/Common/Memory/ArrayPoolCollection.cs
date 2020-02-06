using System;
using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Memory
{
    public class ArrayPoolCollection<T>
    {
        private readonly Dictionary<int, IArrayPool<T>> arrays;

        public ArrayPoolCollection(int size)
        {
            arrays = new Dictionary<int, IArrayPool<T>>(size);
        }

        public T[] PopExact(int size)
        {
            if (!arrays.TryGetValue(size, out IArrayPool<T> pool))
            {
                pool = new ArrayPool<T>(size, 4, 1);
                arrays.Add(size, pool);
            }

            return pool.Pop();
        }

        public T[] Pop(int size)
        {
            int length = GetRoundedSize(size);
            return PopExact(length);
        }

        public void Push(T[] array)
        {
            int length = array.Length;

            if (!arrays.TryGetValue(length, out IArrayPool<T> pool))
            {
                throw new InvalidOperationException("Couldn't find an array pool of length " + length.ToString());
            }

            pool.Push(array);
        }

        private const int ROUND_SIZE_BY = 100;

        protected static int GetRoundedSize(int size)
        {
            int rounded = (size / ROUND_SIZE_BY) * ROUND_SIZE_BY;
            return rounded + ROUND_SIZE_BY;
        }

        public override string ToString()
        {
            return arrays.Count.ToString();
        }
    }
}
