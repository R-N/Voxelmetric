using System;

namespace Voxelmetric
{
    public interface AThreadPoolItem : ITaskPoolItem
    {
        int ThreadID { get; }
    }

    public class ThreadPoolItem<T> : AThreadPoolItem
    {
        private Action<T> action;
        private T arg;
        private bool processed = false;

        public int ThreadID { get; private set; }
        public long Priority { get; private set; }

        public ThreadPoolItem() { }

        public ThreadPoolItem(ThreadPool pool, Action<T> action, T arg, long priority = long.MinValue)
        {
            this.action = action;
            this.arg = arg;
            ThreadID = pool.GenerateThreadID();
            Priority = priority;
        }

        public ThreadPoolItem(int threadID, Action<T> action, T arg, long time = long.MinValue)
        {
            this.action = action;
            this.arg = arg;
            ThreadID = threadID;
            Priority = time;
        }

        public void Set(ThreadPool pool, Action<T> action, T arg, long time = long.MinValue)
        {
            this.action = action;
            this.arg = arg;
            processed = false;
            ThreadID = pool.GenerateThreadID();
            Priority = time;
        }

        public void Set(int threadID, Action<T> action, T arg, long time = long.MaxValue)
        {
            this.action = action;
            this.arg = arg;
            processed = false;
            ThreadID = threadID;
            Priority = time;
        }

        public void Run()
        {
            if (!processed)
            {
                action(arg);
                processed = true;
            }
        }
    }
}
