using System;
using System.Collections.Generic;
using System.Text;

namespace Voxelmetric
{
    public sealed class ObjectPool<T> where T : class
    {
        //! Delegate handling allocation of memory
        private readonly ObjectPoolAllocator<T> objectAllocator;
        //! Delegate handling releasing of memory
        private readonly Action<T> objectDeallocator;
        //! Object storage
        private readonly List<T> objects;
        //! Index to the first available object in object pool
        private int objectIndex;
        //! Initial size of object pool. We never deallocate memory under this threshold
        private readonly int initialSize;
        //! If true object pool will try to release some of the unused memory if the difference in currently used size and capacity of pool is too big
        private readonly bool autoReleaseMemory;

        public int Capacity
        {
            get { return objects.Count; }
        }

        public ObjectPool(Func<T, T> allocator, int initialSize, bool autoReleaseMememory)
        {
            objectAllocator = new ObjectPoolAllocator<T>(allocator);
            objectDeallocator = null;
            this.initialSize = initialSize;
            autoReleaseMemory = autoReleaseMememory;
            objectIndex = 0;

            objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
            {
                objects.Add(objectAllocator.action(objectAllocator.arg));
            }
        }

        public ObjectPool(ObjectPoolAllocator<T> objectAllocator, int initialSize, bool autoReleaseMememory)
        {
            this.objectAllocator = objectAllocator;
            objectDeallocator = null;
            this.initialSize = initialSize;
            autoReleaseMemory = autoReleaseMememory;
            objectIndex = 0;

            objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
            {
                objects.Add(this.objectAllocator.action(this.objectAllocator.arg));
            }
        }

        public ObjectPool(Func<T, T> objectAllocator, Action<T> objectDeallocator, int initialSize)
        {
            this.objectAllocator = new ObjectPoolAllocator<T>(objectAllocator);
            this.objectDeallocator = objectDeallocator;
            this.initialSize = initialSize;
            autoReleaseMemory = true;
            objectIndex = 0;

            objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
            {
                objects.Add(this.objectAllocator.action(this.objectAllocator.arg));
            }
        }

        public ObjectPool(ObjectPoolAllocator<T> objectAllocator, Action<T> objectDeallocator, int initialSize)
        {
            this.objectAllocator = objectAllocator;
            this.objectDeallocator = objectDeallocator;
            this.initialSize = initialSize;
            autoReleaseMemory = true;
            objectIndex = 0;

            objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
            {
                objects.Add(this.objectAllocator.action(this.objectAllocator.arg));
            }
        }

        /// <summary>
        ///     Retrieves an object from the top of the pool
        /// </summary>
        public T Pop()
        {
            if (objectIndex >= objects.Count)
            {
                // Capacity limit has been reached, allocate new elemets
                objects.Add(objectAllocator.action(objectAllocator.arg));
                // Let Unity handle how much memory is going to be preallocated
                for (int i = objects.Count; i < objects.Capacity; i++)
                {
                    objects.Add(objectAllocator.action(objectAllocator.arg));
                }
            }

            return objects[objectIndex++];
        }

        /// <summary>
        ///     Returns an object back to pool
        /// </summary>
        public void Push(T item)
        {
            if (objectIndex <= 0)
            {
                throw new InvalidOperationException("Object pool is full");
            }

            // If we're using less then 1/4th of memory capacity, let's free half of the allocated memory.
            // We're doing it this way so that there's a certain threshold before allocating new memory.
            // We only deallocate if there's at least m_initialSize items allocated.
            if (autoReleaseMemory)
            {
                int thresholdCount = objects.Count >> 2;
                if (thresholdCount > initialSize && objectIndex <= thresholdCount)
                {
                    int halfCount = objects.Count >> 1;

                    // Use custom deallocation if deallocator is set
                    if (objectDeallocator != null)
                    {
                        for (int i = halfCount; i < objects.Count; i++)
                        {
                            objectDeallocator(objects[i]);
                        }
                    }

                    // Remove one half of unused items
                    objects.RemoveRange(halfCount, halfCount);
                }
            }

            objects[--objectIndex] = item;
        }

        /// <summary>
        ///    Releases all unused memory
        /// </summary>
        public void Compact()
        {
            // Use custom deallocation if deallocator is set
            if (objectDeallocator != null)
            {
                for (int i = objectIndex; i < objects.Count; i++)
                {
                    objectDeallocator(objects[i]);
                }
            }

            // Remove all unused items
            objects.RemoveRange(objectIndex, objects.Count - objectIndex);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.ConcatFormat("{0}/{1}", objectIndex, Capacity);
            return sb.ToString();
        }
    }
}
