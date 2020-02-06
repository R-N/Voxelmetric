using System.Collections.Generic;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class WorkPoolManager
    {
        private static readonly List<AThreadPoolItem> workItems = new List<AThreadPoolItem>(2048);
        private static readonly List<AThreadPoolItem> workItemsP = new List<AThreadPoolItem>(2048);

        private static readonly HashSet<TaskPool> threads = Features.useThreadPool ? new HashSet<TaskPool>() : null;
        private static readonly List<TaskPool> threadsIter = Features.useThreadPool ? new List<TaskPool>() : null;
        private static readonly TimeBudgetHandler timeBudget = Features.useThreadPool ? null : new TimeBudgetHandler(10);

        public static void Add(AThreadPoolItem action, bool priority)
        {
            if (priority)
            {
                workItemsP.Add(action);
            }
            else
            {
                workItems.Add(action);
            }
        }

        private static void ProcessWorkItems(List<AThreadPoolItem> wi)
        {
            // Skip empty lists
            if (wi.Count <= 0)
            {
                return;
            }

            // Sort our work items by threadID
            wi.Sort((x, y) => x.ThreadID.CompareTo(y.ThreadID));

            ThreadPool pool = Globals.WorkPool;
            int from = 0;
            int to = 0;

            // Commit items to their respective task thread.
            // Instead of commiting tasks one by one we commit them all at once
            TaskPool tp;
            for (int i = 0; i < wi.Count - 1; i++)
            {
                AThreadPoolItem curr = wi[i];
                AThreadPoolItem next = wi[i + 1];
                if (curr.ThreadID == next.ThreadID)
                {
                    to = i + 1;
                    continue;
                }

                tp = pool.GetTaskPool(curr.ThreadID);
                for (int j = from; j <= to; j++)
                {
                    tp.AddPriorityItem(wi[j]);
                }
                if (threads.Add(tp))
                {
                    threadsIter.Add(tp);
                }

                from = i + 1;
                to = from;
            }

            tp = pool.GetTaskPool(wi[from].ThreadID);
            for (int j = from; j <= to; j++)
            {
                tp.AddPriorityItem(wi[j]);
            }
            if (threads.Add(tp))
            {
                threadsIter.Add(tp);
            }
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Features.useThreadPool)
            {
                // Priority tasks first
                ProcessWorkItems(workItemsP);
                // Oridinary tasks second
                ProcessWorkItems(workItems);

                // Commit all tasks we collected to their respective threads
                for (int i = 0; i < threadsIter.Count; i++)
                {
                    threadsIter[i].Commit();
                }

                threads.Clear();
                threadsIter.Clear();
            }
            else
            {
                workItemsP.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                for (int i = 0; i < workItemsP.Count; i++)
                {
                    timeBudget.StartMeasurement();
                    workItemsP[i].Run();
                    timeBudget.StopMeasurement();

                    // If the tasks take too much time to finish, spread them out over multiple
                    // frames to avoid performance spikes
                    if (!timeBudget.HasTimeBudget)
                    {
                        workItemsP.RemoveRange(0, i + 1);
                        return;
                    }
                }

                workItems.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                for (int i = 0; i < workItems.Count; i++)
                {
                    timeBudget.StartMeasurement();
                    workItems[i].Run();
                    timeBudget.StopMeasurement();

                    // If the tasks take too much time to finish, spread them out over multiple
                    // frames to avoid performance spikes
                    if (!timeBudget.HasTimeBudget)
                    {
                        workItems.RemoveRange(0, i + 1);
                        return;
                    }
                }
            }

            // Remove processed work items
            workItems.Clear();
            workItemsP.Clear();
        }

        public new static string ToString()
        {
            return Features.useThreadPool ? Globals.WorkPool.ToString() : workItems.Count.ToString();
        }
    }
}
