using System;

namespace Voxelmetric
{
    public sealed class ObjectPoolAllocator<T> where T : class
    {
        public readonly Func<T, T> action;
        public readonly T arg;

        public ObjectPoolAllocator(Func<T, T> action)
        {
            this.action = action;
            arg = null;
        }

        public ObjectPoolAllocator(Func<T, T> action, T arg)
        {
            this.action = action;
            this.arg = arg;
        }
    }
}
