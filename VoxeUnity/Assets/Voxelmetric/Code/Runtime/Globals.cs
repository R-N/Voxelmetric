using System.Diagnostics;

namespace Voxelmetric
{
    public static class Globals
    {
        // Thread pool
        public static ThreadPool WorkPool { get; private set; }

        public static void InitWorkPool()
        {
            if (WorkPool == null)
            {
                WorkPool = new ThreadPool();
                WorkPool.Start();
            }
        }

        // Task pool for IO-related tasks
        public static TaskPool IOPool { get; private set; }

        public static void InitIOPool()
        {
            if (IOPool == null)
            {
                IOPool = new TaskPool();
                IOPool.Start();
            }
        }

        // Geometry mesh builder used for the terrain
        public static AMeshBuilder ModelMeshBuilder { get; } = new CubeMeshBuilder(Env.BLOCK_SIZE, Env.CHUNK_SIZE) { SideMask = 0 };

        private static AMeshBuilder terrainMeshBuilder;

        // Geometry mesh builder used for the terrain
        public static AMeshBuilder TerrainMeshBuilder
        {
            get
            {
                if (terrainMeshBuilder == null)
                {
                    if (Features.useGreedyMeshing)
                    {
                        terrainMeshBuilder = new CubeMeshBuilder(Env.BLOCK_SIZE, Env.CHUNK_SIZE) { SideMask = Features.DONT_RENDER_WORLD_EDGES_MASK };
                    }
                    else
                    {
                        terrainMeshBuilder = new CubeMeshBuilderNaive(Env.BLOCK_SIZE, Env.CHUNK_SIZE) { SideMask = Features.DONT_RENDER_WORLD_EDGES_MASK };
                    }
                }

                return terrainMeshBuilder;
            }
        }

        // Collider mesh builder used for the terrain
        public static AMeshBuilder TerrainMeshColliderBuilder { get; } = new CubeMeshColliderBuilder(Env.BLOCK_SIZE, Env.CHUNK_SIZE) { SideMask = Features.DONT_RENDER_WORLD_EDGES_MASK };

        // Global object pools
        public static GlobalPools MemPools { get; private set; }

        public static void InitMemPools()
        {
            if (MemPools == null)
            {
                MemPools = new GlobalPools();
            }
        }

        // Global stop watch
        public static Stopwatch Watch { get; private set; }
        public static void InitWatch()
        {
            if (Watch == null)
            {
                Watch = new Stopwatch();
                Watch.Start();
            }
        }

        // Global time budget handlers
        public static TimeBudgetHandler GeometryBudget { get; } = new TimeBudgetHandler(4);

        public static TimeBudgetHandler SetBlockBudget { get; } = new TimeBudgetHandler(4);
    }
}
