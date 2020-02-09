using UnityEngine;

namespace Voxelmetric
{
    public class TimeBudgetHandler
    {
        //! Time in ms allowed to be spent working on something
        public long TimeBudgetMs { get; set; }

        public bool HasTimeBudget { get; private set; }

        private long startTime;
        private long totalTime;

        public TimeBudgetHandler(long budget = 0)
        {
            Reset();
            TimeBudgetMs = budget;
        }

        public void Reset()
        {
            startTime = 0;
            totalTime = 0;
            HasTimeBudget = true;
        }

        public void StartMeasurement()
        {
            startTime = Globals.Watch.ElapsedMilliseconds;
        }

        public void StopMeasurement()
        {
            long stopTime = Globals.Watch.ElapsedMilliseconds;
            Debug.Assert(stopTime >= startTime); // Let's make sure the class is used correctly

            totalTime += (stopTime - startTime);
            HasTimeBudget = totalTime < TimeBudgetMs;
        }
    }
}
