using System;

namespace Voxelmetric
{
    public interface ITaskPoolItem
    {
        long Priority { get; }
        void Run();
    }

    public class TaskPoolItem<T> : ITaskPoolItem
    {
        private Action<T> action;
        private T arg;
        private bool processed = false;

        public long Priority { get; private set; }

        public TaskPoolItem() { }

        public TaskPoolItem(Action<T> action, T arg, long priority = long.MinValue)
        {
            this.action = action;
            this.arg = arg;
            Priority = priority;
        }

        public void Set(Action<T> action, T arg, long priority = long.MinValue)
        {
            this.action = action;
            this.arg = arg;
            Priority = priority;
            processed = false;
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
