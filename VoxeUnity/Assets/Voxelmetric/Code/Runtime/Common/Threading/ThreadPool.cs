using System;
using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public class ThreadPool
    {
        private bool started;
        private volatile int nextThreadIndex = 0;

        //! Threads used by thread pool
        private readonly TaskPool[] pools;

        //! Diagnostics
        private readonly StringBuilder sb = new StringBuilder(128);

        public ThreadPool()
        {
            started = false;

            // If the number of threads is not correctly specified, create as many as possible minus one (taking
            // all available core is not effective - there's still the main thread we should not forget).
            // Allways create at least one thread, however.
            int threadCnt = Features.useThreadPool ? Mathf.Max(Environment.ProcessorCount - 1, 1) : 1;
            pools = Helpers.CreateArray1D<TaskPool>(threadCnt);
            // NOTE: Normally, I would simply call CreateAndInitArray1D, however, any attempt to allocate memory
            // for TaskPool in this contructor ends up with Unity3D crashing :(
        }

        public int GenerateThreadID()
        {
            nextThreadIndex = GetThreadIDFromIndex(nextThreadIndex + 1);
            return nextThreadIndex;
        }

        public int GetThreadIDFromIndex(int index)
        {
            return Helpers.Mod(index, pools.Length);
        }

        public LocalPools GetPool(int index)
        {
            int id = GetThreadIDFromIndex(index);
            return pools[id].Pools;
        }

        public TaskPool GetTaskPool(int index)
        {
            return pools[index];
        }

        public void Start()
        {
            if (started)
            {
                return;
            }

            started = true;

            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = new TaskPool();
                pools[i].Start();
            }
        }

        public int Size
        {
            get { return pools.Length; }
        }

        public override string ToString()
        {
            sb.Length = 0;
            for (int i = 0; i < pools.Length - 1; i++)
            {
                sb.ConcatFormat("{0}, ", pools[i].ToString());
            }

            return sb.ConcatFormat("{0}", pools[pools.Length - 1].ToString()).ToString();
        }
    }
}
