using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public sealed class TaskPool : IDisposable
    {
        //! Each thread contains an object pool
        public LocalPools Pools { get; }

        private readonly object objectLock = new object();

        private List<ITaskPoolItem> items; // list of tasks
        private List<ITaskPoolItem> itemsP; // list of tasks

        private readonly List<ITaskPoolItem> itemsTmp; // temporary list of tasks
        private readonly List<ITaskPoolItem> itemsTmpP; // temporary list of tasks

        private readonly AutoResetEvent @event; // event for notifing worker thread about work
        private readonly Thread thread; // worker thread

        private bool stop;
        private bool hasPriorityItems;

        //! Diagnostics
        private int curr, max, currP, maxP;
        private readonly StringBuilder sb = new StringBuilder(32);

        public TaskPool()
        {
            Pools = new LocalPools();

            items = new List<ITaskPoolItem>();
            itemsP = new List<ITaskPoolItem>();

            itemsTmp = new List<ITaskPoolItem>();
            itemsTmpP = new List<ITaskPoolItem>();

            @event = new AutoResetEvent(false);
            thread = new Thread(ThreadFunc)
            {
                IsBackground = true
            };

            stop = false;
            hasPriorityItems = false;

            curr = max = currP = maxP = 0;
        }

        ~TaskPool()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            Stop();

            if (disposing)
            {
                // dispose managed resources
                @event.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Stop()
        {
            stop = true;
            @event.Set();
        }

        public void AddItem(ITaskPoolItem item)
        {
            Assert.IsNotNull(item);
            itemsTmp.Add(item);
        }

        public void AddItem<T>(Action<T> action, long priority = long.MinValue) where T : class
        {
            Assert.IsNotNull(action);
            itemsTmp.Add(new TaskPoolItem<T>(action, null, priority));
        }

        public void AddItem<T>(Action<T> action, T arg, long time = long.MinValue)
        {
            Assert.IsNotNull(action);
            itemsTmp.Add(new TaskPoolItem<T>(action, arg, time));
        }

        public void AddPriorityItem(ITaskPoolItem item)
        {
            Assert.IsNotNull(item);
            itemsTmpP.Add(item);
        }

        public void AddPriorityItem<T>(Action<T> action, long priority = long.MinValue) where T : class
        {
            Assert.IsNotNull(action);
            itemsTmpP.Add(new TaskPoolItem<T>(action, null, priority));
        }

        public void AddPriorityItem<T>(Action<T> action, T arg, long priority = long.MinValue)
        {
            Assert.IsNotNull(action);
            itemsTmpP.Add(new TaskPoolItem<T>(action, arg, priority));
        }

        public void Commit()
        {
            if (itemsTmp.Count <= 0 && itemsTmpP.Count <= 0)
            {
                return;
            }

            lock (objectLock)
            {
                items.AddRange(itemsTmp);
                itemsP.AddRange(itemsTmpP);

                hasPriorityItems = itemsP.Count > 0;
            }

            itemsTmp.Clear();
            itemsTmpP.Clear();

            @event.Set();
        }

        private void ThreadFunc()
        {
            List<ITaskPoolItem> actions = new List<ITaskPoolItem>();
            List<ITaskPoolItem> actionsP = new List<ITaskPoolItem>();

            ITaskPoolItem poolItem;

            while (!stop)
            {
                // Swap action list pointers
                lock (objectLock)
                {
                    List<ITaskPoolItem> tmp = actions;
                    actions = items;
                    items = tmp;

                    tmp = actionsP;
                    actionsP = itemsP;
                    itemsP = tmp;

                    hasPriorityItems = false;
                }

                // Sort tasks by priority
                actions.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                max = actions.Count;
                curr = 0;

            priorityLabel:
                actionsP.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                maxP = actionsP.Count;
                currP = 0;

                // Process priority tasks first
                for (; currP < maxP; currP++)
                {
                    poolItem = actionsP[currP];

#if DEBUG
                    try
                    {
#endif
                        poolItem.Run();
#if DEBUG
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
#endif
                }

                // Process ordinary tasks now
                for (; curr < max; curr++)
                {
                    poolItem = actions[curr];

#if DEBUG
                    try
                    {
#endif
                        poolItem.Run();
#if DEBUG
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
#endif

                    // Let's see if there wasn't a priority action queued in the meantime.
                    // No need to lock these bool variables here. If they're not set yet,
                    // we'll simply read their state in the next iteration
                    if (!stop && hasPriorityItems)
                    {
                        lock (objectLock)
                        {
                            actionsP.AddRange(itemsP);
                            itemsP.Clear();

                            hasPriorityItems = false;
                            goto priorityLabel;
                        }
                    }
                }

                // Everything processed
                actions.Clear();
                actionsP.Clear();
                curr = max = currP = maxP = 0;

                // Wait for new tasks
                @event.WaitOne();
            }
        }

        public override string ToString()
        {
            sb.Length = 0;
            return sb.ConcatFormat("{0}/{1}, prio:{2}/{3}", curr, max, currP, maxP).ToString();
        }
    }
}
