using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.GeometryHandler;

namespace Voxelmetric.Code.Core
{
    public partial class Chunk : ChunkEventSource
    {
        //! Static shared pointers to callbacks
        private static readonly Action<Chunk> actionOnLoadData = OnLoadData;
        private static readonly Action<Chunk> actionOnPrepareGenerate = OnPrepareGenerate;
        private static readonly Action<Chunk> actionOnGenerateData = OnGenerateData;
        private static readonly Action<Chunk> actionOnPrepareSaveData = OnPrepareSaveData;
        private static readonly Action<Chunk> actionOnSaveData = OnSaveData;
        private static readonly Action<Chunk> actionOnSyncEdges = OnSynchronizeEdges;
        private static readonly Action<Chunk> actionOnBuildVertices = OnBuildVertices;
        private static readonly Action<Chunk> actionOnBuildCollider = OnBuildCollider;

        //! ID used by memory pools to map the chunk to a given thread. Must be accessed from the main thread
        private static int id = 0;

        public World World { get; private set; }
        public ChunkBlocks Blocks { get; }

        public ChunkRenderGeometryHandler RenderGeometryHandler { get; private set; }
        public ChunkColliderGeometryHandler ColliderGeometryHandler { get; private set; }

        //! Queue of setBlock operations to execute
        private List<ModifyOp> setBlockQueue;

        //! Save handler for chunk
        private readonly Save save;
        //! Custom update logic
        private ChunkLogic logic;

        //! Chunk position in world coordinates
        public Vector3Int Pos { get; private set; }

        //! Bounding box in world coordinates. It always considers a full-size chunk
        public AABB worldBounds;

        //! List of neighbors
        public Chunk[] Neighbors { get; } = Helpers.CreateArray1D<Chunk>(6);
        //! Current number of neigbors
        public int NeighborCount { get; private set; }
        //! Maximum possible number of neighbors given the circumstances
        public int NeighborCountMax { get; private set; }

        private int pow = 0;
        //! Size of chunk's side
        private int sideSize = 0;
        public int SideSize
        {
            get { return sideSize; }
            set
            {
                sideSize = value;
                pow = 1 + (int)Math.Log(value, 2);
            }
        }

        private long lastUpdateTimeGeometry;
        private long lastUpdateTimeCollider;
        private int rebuildMaskGeometry;
        private int rebuildMaskCollider;

        //! Bounding coordinates in local space. Corresponds to real geometry
        public int minBounds, maxBounds;
        //! Bounding coordinates in local space. Corresponds to collision geometry
        public int minBoundsC, maxBoundsC;

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public int maxPendingStructureListIndex;
        public bool needApplyStructure;

        //! State to notify event listeners about
        private ChunkStateExternal stateExternal;
        //! States waiting to be processed
        private ChunkState pendingStates;
        //! Tasks already executed
        private ChunkState completedStates;
        //! Specifies whether there's a task running on this Chunk
        private bool taskRunning;
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool removalRequested;
        //! If true, edge synchronization is in progress
        private bool isSyncingEdges;

        //! Flags telling us whether pool items should be returned back to the pool
        private ChunkPoolItemState poolState;
        private ITaskPoolItem threadPoolItem;

        //! Says whether the chunk needs collision geometry
        public bool NeedsColliderGeometry
        {
            get { return ColliderGeometryHandler.Batcher.Enabled; }
            set { ColliderGeometryHandler.Batcher.Enabled = value; }
        }

        //! Says whether the chunk needs render geometry
        public bool NeedsRenderGeometry
        {
            get { return RenderGeometryHandler.Batcher.Enabled; }
            set { RenderGeometryHandler.Batcher.Enabled = value; }
        }

        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        public bool IsSavePossible
        {
            get
            {
                // Serialization must be enabled
                if (!Features.useSerialization)
                {
                    return false;
                }

                // Chunk has to be generated first before we can save it
                if ((completedStates & ChunkState.Generate) == 0)
                {
                    return false;
                }

                // When doing a pure differential serialization chunk needs to be modified before we can save it
                return !Features.useDifferentialSerialization || Features.useDifferentialSerialization_ForceSaveHeaders || Blocks.modifiedBlocks.Count > 0;
            }
        }

        /// <summary>
        /// Takes a chunk from the memory pool and intiates it
        /// </summary>
        /// <param name="world">World to which this chunk belongs</param>
        /// <param name="pos">Chunk position in world coordinates</param>
        /// <returns>A new chunk</returns>
        public static Chunk Create(World world, Vector3Int pos)
        {
            Chunk chunk = Globals.MemPools.chunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
        }

        /// <summary>
        /// Returns a chunk back to the memory pool
        /// </summary>
        /// <param name="chunk">Chunk to be returned back to the memory pool</param>
        public static void Remove(Chunk chunk)
        {
            Assert.IsTrue((chunk.completedStates & ChunkState.Remove) != 0);

            // Reset the chunk back to defaults
            chunk.Reset();
            chunk.World = null;

            // Return the chunk pack to object pool
            Globals.MemPools.chunkPool.Push(chunk);
        }

        public Chunk(int sideSize = Env.CHUNK_SIZE)
        {
            SideSize = sideSize;

            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(id++);

            Blocks = new ChunkBlocks(this, sideSize);
            if (Features.useSerialization)
            {
                save = new Save(this);
            }
        }

        public void Init(World world, Vector3Int pos)
        {
            World = world;
            Pos = pos;

            if (world != null)
            {
                logic = world.config.RandomUpdateFrequency > 0.0f ? new ChunkLogic(this) : null;

                if (RenderGeometryHandler == null)
                {
                    RenderGeometryHandler = new ChunkRenderGeometryHandler(this, world.renderMaterials);
                }

                if (ColliderGeometryHandler == null)
                {
                    ColliderGeometryHandler = new ChunkColliderGeometryHandler(this, world.physicsMaterials);
                }
            }
            else
            {
                if (RenderGeometryHandler == null)
                {
                    RenderGeometryHandler = new ChunkRenderGeometryHandler(this, null);
                }

                if (ColliderGeometryHandler == null)
                {
                    ColliderGeometryHandler = new ChunkColliderGeometryHandler(this, null);
                }
            }

            worldBounds = new AABB(
                pos.x, pos.y, pos.z,
                pos.x + SideSize, pos.y + SideSize, pos.z + SideSize
                );

            setBlockQueue = new List<ModifyOp>();

            Reset();

            Blocks.Init();

            // Request this chunk to be generated
            pendingStates |= ChunkState.LoadData;

            // Subscribe neighbors
            SubscribeNeighbors(true);
        }

        private void Reset()
        {
            // Unsubscribe neighbors
            SubscribeNeighbors(false);

            // Reset neighor data
            NeighborCount = 0;
            NeighborCountMax = 0;
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Neighbors[i] = null;
            }

            stateExternal = ChunkStateExternal.None;
            pendingStates = ChunkState.None;
            completedStates = ChunkState.None;

            poolState = poolState.Reset();
            taskRunning = false;
            threadPoolItem = null;

            NeedsRenderGeometry = false;
            NeedsColliderGeometry = false;
            PossiblyVisible = false;
            removalRequested = false;
            isSyncingEdges = false;

            needApplyStructure = true;
            maxPendingStructureListIndex = 0;

            minBounds = maxBounds = 0;
            minBoundsC = maxBoundsC = 0;

            lastUpdateTimeGeometry = 0;
            lastUpdateTimeCollider = 0;
            rebuildMaskGeometry = -1;
            rebuildMaskCollider = -1;

            Blocks.Reset();
            if (logic != null)
            {
                logic.Reset();
            }

            if (save != null)
            {
                save.Reset();
            }

            Clear();

            RenderGeometryHandler.Reset();
            ColliderGeometryHandler.Reset();

            //chunk.world = null; <-- must not be done inside here! Do it outside the method
        }

        public bool UpdateCollisionGeometry()
        {
            Profiler.BeginSample("UpdateCollisionGeometry");

            // Build collision geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
            {
                Profiler.EndSample();
                return false;
            }

            // Build collider only if necessary
            if ((completedStates & ChunkStates.CURR_STATE_BUILD_COLLIDER) == 0)
            {
                Profiler.EndSample();
                return false;
            }

            Globals.GeometryBudget.StartMeasurement();
            {
                ColliderGeometryHandler.Commit();
                completedStates &= ~ChunkStates.CURR_STATE_BUILD_COLLIDER;
            }
            Globals.GeometryBudget.StopMeasurement();

            Profiler.EndSample();
            return true;
        }

        public bool UpdateRenderGeometry()
        {
            Profiler.BeginSample("UpdateRenderGeometry");

            // Build render geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
            {
                Profiler.EndSample();
                return false;
            }

            // Build chunk mesh only if necessary
            if ((completedStates & ChunkStates.CURR_STATE_BUILD_VERTICES) == 0)
            {
                Profiler.EndSample();
                return false;
            }

            Globals.GeometryBudget.StartMeasurement();
            {
                RenderGeometryHandler.Commit();
                completedStates &= ~ChunkStates.CURR_STATE_BUILD_VERTICES;
            }
            Globals.GeometryBudget.StopMeasurement();

            Profiler.EndSample();
            return true;
        }

        #region Neighbors

        public bool RegisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null || neighbor == this)
            {
                return false;
            }

            // Determine neighbors's direction as compared to current chunk
            Vector3Int p = Pos - neighbor.Pos;
            Direction dir = Direction.up;
            if (p.x < 0)
            {
                dir = Direction.east;
            }
            else if (p.x > 0)
            {
                dir = Direction.west;
            }
            else if (p.z < 0)
            {
                dir = Direction.north;
            }
            else if (p.z > 0)
            {
                dir = Direction.south;
            }
            else if (p.y > 0)
            {
                dir = Direction.down;
            }

            Chunk l = Neighbors[(int)dir];

            // Do not register if already registred
            if (l == neighbor)
            {
                return false;
            }

            // Subscribe in the first free slot
            if (l == null)
            {
                ++NeighborCount;
                Assert.IsTrue(NeighborCount <= 6);
                Neighbors[(int)dir] = neighbor;
                return true;
            }

            // We want to register but there is no free space
            Assert.IsTrue(false);

            return false;
        }

        public bool UnregisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null || neighbor == this)
            {
                return false;
            }

            // Determine neighbors's direction as compared to current chunk
            Vector3Int p = Pos - neighbor.Pos;
            Direction dir = Direction.up;
            if (p.x < 0)
            {
                dir = Direction.east;
            }
            else if (p.x > 0)
            {
                dir = Direction.west;
            }
            else if (p.z < 0)
            {
                dir = Direction.north;
            }
            else if (p.z > 0)
            {
                dir = Direction.south;
            }
            else if (p.y > 0)
            {
                dir = Direction.down;
            }

            Chunk l = Neighbors[(int)dir];

            // Do not unregister if it's something else than we expected
            if (l != neighbor && l != null)
            {
                Assert.IsTrue(false);
                return false;
            }

            // Only unregister already registered sections
            if (l == neighbor)
            {
                --NeighborCount;
                Assert.IsTrue(NeighborCount >= 0);
                Neighbors[(int)dir] = null;
                return true;
            }

            return false;
        }

        private static void UpdateNeighborCount(Chunk chunk)
        {
            World world = chunk.World;
            if (world == null)
            {
                return;
            }

            // Calculate how many neighbors a chunk can have
            int maxNeighbors = 0;
            Vector3Int pos = chunk.Pos;
            if (world.CheckInsideWorld(pos.Add(Env.CHUNK_SIZE, 0, 0)) && pos.x <= world.Bounds.maxX)
            {
                ++maxNeighbors;
            }

            if (world.CheckInsideWorld(pos.Add(-Env.CHUNK_SIZE, 0, 0)) && pos.x >= world.Bounds.minX)
            {
                ++maxNeighbors;
            }

            if (world.CheckInsideWorld(pos.Add(0, Env.CHUNK_SIZE, 0)) && pos.y <= world.Bounds.maxY)
            {
                ++maxNeighbors;
            }

            if (world.CheckInsideWorld(pos.Add(0, -Env.CHUNK_SIZE, 0)) && pos.y >= world.Bounds.minY)
            {
                ++maxNeighbors;
            }

            if (world.CheckInsideWorld(pos.Add(0, 0, Env.CHUNK_SIZE)) && pos.z <= world.Bounds.maxZ)
            {
                ++maxNeighbors;
            }

            if (world.CheckInsideWorld(pos.Add(0, 0, -Env.CHUNK_SIZE)) && pos.z >= world.Bounds.minZ)
            {
                ++maxNeighbors;
            }

            // Update max neighbor count and request geometry update
            chunk.NeighborCountMax = maxNeighbors;

            // Geometry & collider needs to be rebuilt
            // This does not mean they will be built because the chunk might not
            // be visible or colliders might be turned off
            chunk.pendingStates |= ChunkState.SyncEdges;
            chunk.pendingStates |= ChunkState.BuildVertices;
            chunk.pendingStates |= ChunkState.BuildCollider;
        }

        private void SubscribeNeighbors(bool subscribe)
        {
            SubscribeTwoNeighbors(Pos.Add(Env.CHUNK_SIZE, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(-Env.CHUNK_SIZE, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, Env.CHUNK_SIZE, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, -Env.CHUNK_SIZE, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, Env.CHUNK_SIZE), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, -Env.CHUNK_SIZE), subscribe);

            // Update required neighbor count
            UpdateNeighborCount(this);
        }

        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
        {
            Chunk neighbor = World.GetChunk(ref neighborPos);
            if (neighbor == null)
            {
                return;
            }

            // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
            if (subscribe)
            {
                neighbor.RegisterNeighbor(this);
                RegisterNeighbor(neighbor);
            }
            else
            {
                neighbor.UnregisterNeighbor(this);
                UnregisterNeighbor(neighbor);
            }

            // Update required neighbor count of the neighbor
            UpdateNeighborCount(neighbor);
        }

        public bool NeedToHandleNeighbors(ref Vector3Int pos)
        {
            return rebuildMaskGeometry != 0x3f &&
                   // Only check neighbors when it is a change of a block on a chunk's edge
                   (pos.x <= 0 || pos.x >= sideSize - 1 ||
                    pos.y <= 0 || pos.y >= sideSize - 1 ||
                    pos.z <= 0 || pos.z >= sideSize - 1);
        }

        private ChunkBlocks HandleNeighborRight(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.east);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lz != cz)
            {
                return null;
            }

            if (pos.x != sideSize - 1 || lx - sideSize != cx)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborLeft(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.west);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lz != cz)
            {
                return null;
            }

            if (pos.x != 0 || lx + sideSize != cx)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborUp(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.up);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (lx != cx && lz != cz)
            {
                return null;
            }

            if (pos.y != sideSize - 1 || ly - sideSize != cy)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborDown(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.down);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (lx != cx && lz != cz)
            {
                return null;
            }

            if (pos.y != 0 || ly + sideSize != cy)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborFront(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.north);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lx != cx)
            {
                return null;
            }

            if (pos.z != sideSize - 1 || lz - sideSize != cz)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborBack(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.south);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
            {
                return null;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lx != cx)
            {
                return null;
            }

            if (pos.z != 0 || lz + sideSize != cz)
            {
                return null;
            }

            rebuildMaskGeometry |= (1 << i);
            return neighbor.Blocks;
        }

        public ChunkBlocks HandleNeighbor(ref Vector3Int pos, Direction dir)
        {
            switch (dir)
            {
                case Direction.up:
                    return HandleNeighborUp(ref pos);
                case Direction.down:
                    return HandleNeighborDown(ref pos);
                case Direction.north:
                    return HandleNeighborFront(ref pos);
                case Direction.south:
                    return HandleNeighborBack(ref pos);
                case Direction.east:
                    return HandleNeighborRight(ref pos);
                default: //Direction.west
                    return HandleNeighborLeft(ref pos);
            }
        }

        public void HandleNeighbors(BlockData block, Vector3Int pos)
        {
            if (!NeedToHandleNeighbors(ref pos))
            {
                return;
            }

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild their geometry
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighbor = Neighbors[i];
                if (neighbor == null)
                {
                    continue;
                }

                ChunkBlocks neighborChunkBlocks = neighbor.Blocks;

                int lx = neighbor.Pos.x;
                int ly = neighbor.Pos.y;
                int lz = neighbor.Pos.z;

                if (ly == cy || lz == cz)
                {
                    // Section to the left
                    if (pos.x == 0 && lx + sideSize == cx)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(sideSize, pos.y, pos.z, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the right
                    else if (pos.x == sideSize - 1 && lx - sideSize == cx)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(-1, pos.y, pos.z, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                if (lx == cx || lz == cz)
                {
                    // Section to the bottom
                    if (pos.y == 0 && ly + sideSize == cy)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, sideSize, pos.z, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the top
                    else if (pos.y == sideSize - 1 && ly - sideSize == cy)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, -1, pos.z, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                if (ly == cy || lx == cx)
                {
                    // Section to the back
                    if (pos.z == 0 && lz + sideSize == cz)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, sideSize, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the front
                    else if (pos.z == sideSize - 1 && lz - sideSize == cz)
                    {
                        rebuildMaskGeometry |= (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, -1, pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                // No further checks needed once we know all neighbors need to be notified
                if (rebuildMaskGeometry == 0x3f)
                {
                    break;
                }
            }
        }

        #endregion

        #region States management

        public void RequestRemoval()
        {
            if (removalRequested)
            {
                return;
            }

            removalRequested = true;
            pendingStates |= ChunkState.Remove;
            if (Features.serializeChunkWhenUnloading)
            {
                pendingStates |= ChunkState.PrepareSaveData;
            }
        }

        public void RequestSave()
        {
            if (removalRequested)
            {
                return;
            }

            pendingStates |= ChunkState.PrepareSaveData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsStateCompleted(ChunkState state)
        {
            return (completedStates & state) != 0;
        }

        #endregion

        #region State management

        public bool Update()
        {
            // Do not do any processing as long as there is any task still running
            // Note that this check is not thread-safe because this value can be changed from a different thread. However,
            // we do not care. The worst thing that can happen is that we read a value which is one frame old...
            // Thanks to this relaxed approach we do not need any synchronization primitives anywhere.
            if (taskRunning)
            {
                return false;
            }

            // Once this chunk is marked as removed we ignore any further requests and won't perform any updates
            if ((completedStates & ChunkState.Remove) != 0)
            {
                return false;
            }

            // Some operations can only be performed on a generated chunk
            if ((completedStates & ChunkState.Generate) != 0)
            {
                // Apply pending structures
                World.ApplyPendingStructures(this);

                // Update logic
                if (logic != null)
                {
                    logic.Update();
                }

                // Update blocks
                UpdateBlocks();
            }

            // Process chunk tasks
            UpdateState();

            return true;
        }

        public void UpdateBlocks()
        {
            // Chunk has to be generated first before we can update its blocks
            if ((completedStates & ChunkState.Generate) == 0)
            {
                return;
            }

            // Don't update during saving
            if (
                (pendingStates & ChunkState.PrepareSaveData) != 0 ||
                (pendingStates & ChunkState.SaveData) != 0
                )
            {
                return;
            }

            // Don't update when neighbors are syncing
            // TODO: We should be interested only in edge-blocks in this case
            if (AreNeighborsSynchronizing())
            {
                return;
            }

            //UnityEngine.Debug.Log(m_setBlockQueue.Count);

            if (setBlockQueue.Count > 0)
            {
                if (rebuildMaskGeometry < 0)
                {
                    rebuildMaskGeometry = 0;
                }

                if (rebuildMaskCollider < 0)
                {
                    rebuildMaskCollider = 0;
                }

                Utilities.TimeBudgetHandler timeBudget = Globals.SetBlockBudget;

                // Modify blocks
                int j;
                for (j = 0; j < setBlockQueue.Count; j++)
                {
                    timeBudget.StartMeasurement();
                    setBlockQueue[j].Apply(this);
                    timeBudget.StopMeasurement();

                    // Sync edges if there's enough time
                    /*if (!timeBudget.HasTimeBudget)
                    {
                        ++j;
                        break;
                    }*/
                }

                rebuildMaskCollider |= rebuildMaskGeometry;

                if (j == setBlockQueue.Count)
                {
                    setBlockQueue.Clear();
                }
                else
                {
                    setBlockQueue.RemoveRange(0, j);
                    return;
                }
            }

            long now = Globals.Watch.ElapsedMilliseconds;

            // Request a geometry update at most 10 times a second
            if (rebuildMaskGeometry >= 0 && now - lastUpdateTimeGeometry >= 100)
            {
                lastUpdateTimeGeometry = now;

                // Request rebuild on this chunk
                pendingStates |= ChunkState.BuildVerticesNow;

                // Notify neighbors that they need to rebuild their geometry
                if (rebuildMaskGeometry > 0)
                {
                    for (int j = 0; j < Neighbors.Length; j++)
                    {
                        Chunk neighbor = Neighbors[j];
                        if (neighbor != null && ((rebuildMaskGeometry >> j) & 1) != 0)
                        {
                            // Request rebuild on neighbor chunks
                            neighbor.pendingStates |= ChunkState.BuildVerticesNow;
                        }
                    }
                }

                rebuildMaskGeometry = -1;
            }

            // Request a collider update at most 4 times a second
            if (NeedsColliderGeometry && rebuildMaskCollider >= 0 && now - lastUpdateTimeCollider >= 250)
            {
                lastUpdateTimeCollider = now;

                // Request rebuild on this chunk
                pendingStates |= ChunkState.BuildColliderNow;

                // Notify neighbors that they need to rebuilt their geometry
                if (rebuildMaskCollider > 0)
                {
                    for (int j = 0; j < Neighbors.Length; j++)
                    {
                        Chunk neighbor = Neighbors[j];
                        if (neighbor != null && ((rebuildMaskCollider >> j) & 1) != 0)
                        {
                            // Request rebuild on neighbor chunks
                            if (neighbor.NeedsColliderGeometry)
                            {
                                neighbor.pendingStates |= ChunkState.BuildColliderNow;
                            }
                        }
                    }
                }

                rebuildMaskCollider = -1;
            }
        }

        private void UpdateState()
        {
            // Return processed work items back to the pool
            ReturnPoolItems();

            if (stateExternal != ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(stateExternal);

                stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to loading the chunk we can safely mark
            // it as removed right away
            if ((pendingStates & ChunkState.Remove) != 0 && (completedStates & ChunkState.LoadData) == 0)
            {
                completedStates |= ChunkState.Remove;
                return;
            }

            // Go from the least important bit to most important one. If a given bit is set
            // we execute a task tied with it
            if ((pendingStates & ChunkState.LoadData) != 0 && LoadData())
            {
                return;
            }

            if ((pendingStates & ChunkState.PrepareGenerate) != 0 && PrepareGenerate())
            {
                return;
            }

            if ((pendingStates & ChunkState.Generate) != 0 && GenerateData())
            {
                return;
            }

            if ((pendingStates & ChunkState.PrepareSaveData) != 0 && PrepareSaveData())
            {
                return;
            }

            if ((pendingStates & ChunkState.SaveData) != 0 && SaveData())
            {
                return;
            }

            if ((pendingStates & ChunkState.Remove) != 0 && RemoveChunk())
            {
                return;
            }

            if ((pendingStates & ChunkState.SyncEdges) != 0 && SynchronizeEdges())
            {
                return;
            }

            if ((pendingStates & ChunkStates.CURR_STATE_BUILD_COLLIDER) != 0 && BuildCollider())
            {
                return;
            }

            if ((pendingStates & ChunkStates.CURR_STATE_BUILD_VERTICES) != 0 && BuildVertices())
            {
                return;
            }
        }

        private void ReturnPoolItems()
        {
            Common.MemoryPooling.GlobalPools pools = Globals.MemPools;

            // Global.MemPools is not thread safe and were returning values to it from a different thread.
            // Therefore, each client remembers which pool it used and once the task is finished it returns
            // it back to the pool as soon as possible from the main thread

            if (poolState.Check(ChunkPoolItemState.ThreadPI))
            {
                pools.SMThreadPI.Push(threadPoolItem as ThreadPoolItem<Chunk>);
            }
            else if (poolState.Check(ChunkPoolItemState.TaskPI))
            {
                pools.SMTaskPI.Push(threadPoolItem as TaskPoolItem<Chunk>);
            }

            poolState = poolState.Reset();
            threadPoolItem = null;
        }

        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="op">Set operation to be performed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modify(ModifyOp op)
        {
            setBlockQueue.Add(op);
        }

        #region Load chunk data

        private const ChunkState CURR_STATE_LOAD_DATA = ChunkState.LoadData;
        private const ChunkState NEXT_STATE_LOAD_DATA = ChunkState.PrepareGenerate;

        private static void OnLoadData(Chunk chunk)
        {
            bool success = Serialization.Serialization.Read(chunk.save);
            OnLoadDataDone(chunk, success);
        }

        private static void OnLoadDataDone(Chunk chunk, bool success)
        {
            if (success)
            {
                chunk.completedStates |= CURR_STATE_LOAD_DATA;
                chunk.pendingStates |= NEXT_STATE_LOAD_DATA;
            }
            else
            {
                chunk.completedStates |= CURR_STATE_LOAD_DATA | ChunkState.PrepareGenerate;
                chunk.pendingStates |= ChunkState.Generate;
            }

            chunk.taskRunning = false;
        }

        private bool LoadData()
        {
            // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
            if (!PossiblyVisible)
            {
                return true;
            }

            pendingStates &= ~CURR_STATE_LOAD_DATA;
            completedStates &= ~CURR_STATE_LOAD_DATA;

            if (Features.useSerialization)
            {
                TaskPoolItem<Chunk> task = Globals.MemPools.SMTaskPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.TaskPI);
                threadPoolItem = task;
                task.Set(actionOnLoadData, this);

                taskRunning = true;
                IOPoolManager.Add(threadPoolItem);

                return true;
            }

            OnLoadDataDone(this, false);
            return false;
        }

        #endregion Load chunk data

        #region Prepare generate

        private const ChunkState CURR_STATE_PREPARE_GENERATE = ChunkState.PrepareGenerate;
        private const ChunkState NEXT_STATE_PREPARE_GENERATE = ChunkState.Generate;

        private static void OnPrepareGenerate(Chunk chunk)
        {
            bool success = chunk.save.DoDecompression();
            OnPrepareGenerateDone(chunk, success);
        }

        private static void OnPrepareGenerateDone(Chunk chunk, bool success)
        {
            // Consume info about invalidated chunk
            chunk.completedStates |= CURR_STATE_PREPARE_GENERATE;

            if (success)
            {
                if (chunk.save.IsDifferential)
                {
                    chunk.pendingStates |= NEXT_STATE_PREPARE_GENERATE;
                }
                else
                {
                    chunk.completedStates |= ChunkState.Generate;
                    chunk.pendingStates |= ChunkState.BuildVertices;
                }
            }
            else
            {
                chunk.pendingStates |= NEXT_STATE_PREPARE_GENERATE;
            }

            chunk.taskRunning = false;
        }

        private bool PrepareGenerate()
        {
            if ((completedStates & ChunkState.LoadData) == 0)
            {
                return true;
            }

            pendingStates &= ~CURR_STATE_PREPARE_GENERATE;
            completedStates &= ~CURR_STATE_PREPARE_GENERATE;

            if (Features.useSerialization && save.CanDecompress())
            {
                ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
                threadPoolItem = task;
                task.Set(ThreadID, actionOnPrepareGenerate, this);

                taskRunning = true;
                IOPoolManager.Add(threadPoolItem);

                return true;
            }

            OnPrepareGenerateDone(this, false);
            return false;
        }

        #endregion

        #region Generate Chunk data

        private const ChunkState CURR_STATE_GENERATE_DATA = ChunkState.Generate;
        private const ChunkState NEXT_STATE_GENERATE_DATA = ChunkState.BuildVertices;

        private static void OnGenerateData(Chunk chunk)
        {
            chunk.World.terrainGen.GenerateTerrain(chunk);

            // Commit serialization changes if any
            if (Features.useSerialization)
            {
                chunk.save.CommitChanges();
            }

            // Calculate the amount of non-empty blocks
            chunk.Blocks.CalculateEmptyBlocks();

            //chunk.blocks.Compress();
            //chunk.blocks.Decompress();

            OnGenerateDataDone(chunk);
        }

        private static void OnGenerateDataDone(Chunk chunk)
        {
            chunk.completedStates |= CURR_STATE_GENERATE_DATA;
            chunk.pendingStates |= NEXT_STATE_GENERATE_DATA;
            chunk.taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(Chunk chunk)
        {
            OnGenerateDataDone(chunk);
            OnLoadDataDone(chunk, false); //TODO: change to true once the network layers is implemented properly
        }

        private bool GenerateData()
        {
            if ((completedStates & ChunkState.LoadData) == 0)
            {
                return true;
            }

            pendingStates &= ~CURR_STATE_GENERATE_DATA;
            completedStates &= ~CURR_STATE_GENERATE_DATA;

            ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
            poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
            threadPoolItem = task;

            task.Set(ThreadID, actionOnGenerateData, this);

            taskRunning = true;
            WorkPoolManager.Add(task, false);

            return true;
        }

        #endregion Generate chunk data

        #region Prepare save

        private const ChunkState CURR_STATE_PREPARE_SAVE_DATA = ChunkState.PrepareSaveData;
        private const ChunkState NEXT_STATE_PREPARE_SAVE_DATA = ChunkState.SaveData;

        private static void OnPrepareSaveData(Chunk chunk)
        {
            bool success = chunk.save.DoCompression();
            OnPrepareSaveDataDone(chunk, success);
        }

        private static void OnPrepareSaveDataDone(Chunk chunk, bool success)
        {
            if (Features.useSerialization)
            {
                if (!success)
                {
                    // Free temporary memory in case of failure
                    chunk.save.MarkAsProcessed();

                    // Consider SaveData completed as well
                    chunk.completedStates |= NEXT_STATE_PREPARE_SAVE_DATA;
                }
                chunk.pendingStates |= NEXT_STATE_PREPARE_SAVE_DATA;
            }

            chunk.completedStates |= CURR_STATE_PREPARE_SAVE_DATA;
            chunk.taskRunning = false;
        }

        private bool PrepareSaveData()
        {
            // We need to wait until chunk is generated
            if ((completedStates & ChunkState.Generate) == 0)
            {
                return true;
            }

            pendingStates &= ~CURR_STATE_PREPARE_SAVE_DATA;
            completedStates &= ~CURR_STATE_PREPARE_SAVE_DATA;

            if (Features.useSerialization && save.ConsumeChanges())
            {
                ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
                threadPoolItem = task;
                task.Set(ThreadID, actionOnPrepareSaveData, this);

                taskRunning = true;
                IOPoolManager.Add(task);

                return true;
            }

            OnPrepareSaveDataDone(this, false);
            return false;
        }

        #endregion Save chunk data

        #region Save chunk data

        private const ChunkState CURR_STATE_SAVE_DATA = ChunkState.SaveData;

        private static void OnSaveData(Chunk chunk)
        {
            bool success = Serialization.Serialization.Write(chunk.save);
            OnSaveDataDone(chunk, success);
        }

        private static void OnSaveDataDone(Chunk chunk, bool success)
        {
            if (Features.useSerialization)
            {
                // Notify listeners in case of success
                if (success)
                {
                    chunk.stateExternal = ChunkStateExternal.Saved;
                }
                // Free temporary memory in case of failure
                chunk.save.MarkAsProcessed();
                chunk.completedStates |= ChunkState.SaveData;
            }

            chunk.completedStates |= CURR_STATE_SAVE_DATA;
            chunk.taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated
            if ((completedStates & ChunkState.PrepareSaveData) == 0)
            {
                return true;
            }

            pendingStates &= ~CURR_STATE_SAVE_DATA;
            completedStates &= ~CURR_STATE_SAVE_DATA;

            if (Features.useSerialization)
            {
                TaskPoolItem<Chunk> task = Globals.MemPools.SMTaskPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.TaskPI);
                threadPoolItem = task;
                task.Set(actionOnSaveData, this);

                taskRunning = true;
                IOPoolManager.Add(task);

                return true;
            }

            OnSaveDataDone(this, false);
            return false;
        }

        #endregion Save chunk data

        #region Synchronize edges

        private bool AreNeighborsSynchronizing()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
            {
                return false;
            }

            // All neighbors have to have their data generated
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighbor = Neighbors[i];
                if (neighbor != null && neighbor.isSyncingEdges)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AreNeighborsSynchronized()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
            {
                return false;
            }

            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighbor = Neighbors[i];
                if (neighbor != null && (neighbor.completedStates & ChunkState.SyncEdges) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreNeighborsGenerated()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
            {
                return false;
            }

            // All neighbors have to have their data generated
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighbor = Neighbors[i];
                if (neighbor != null && (neighbor.completedStates & ChunkState.Generate) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        // A dummy chunk. Used e.g. for copying air block to padded area of chunks missing a neighbor
        private static readonly Chunk dummyChunk = new Chunk();

        private static void OnSynchronizeEdges(Chunk chunk)
        {
            int chunkSize1 = chunk.SideSize - 1;
            int sizePlusPadding = chunk.SideSize + Env.CHUNK_PADDING;
            int sizeWithPadding = chunk.SideSize + Env.CHUNK_PADDING_2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int chunkIterXY = sizeWithPaddingPow2 - sizeWithPadding;

            for (int i = 0; i < chunk.Neighbors.Length; i++)
            {
                Chunk neighborChunk = dummyChunk;
                Vector3Int neighborPos;

                Chunk neighbor = chunk.Neighbors[i];
                if (neighbor != null)
                {
                    neighborChunk = neighbor;
                    neighborPos = neighbor.Pos;
                }
                else
                {
                    switch ((Direction)i)
                    {
                        case Direction.up:
                            neighborPos = chunk.Pos.Add(0, Env.CHUNK_SIZE, 0);
                            break;
                        case Direction.down:
                            neighborPos = chunk.Pos.Add(0, -Env.CHUNK_SIZE, 0);
                            break;
                        case Direction.north:
                            neighborPos = chunk.Pos.Add(0, 0, Env.CHUNK_SIZE);
                            break;
                        case Direction.south:
                            neighborPos = chunk.Pos.Add(0, 0, -Env.CHUNK_SIZE);
                            break;
                        case Direction.east:
                            neighborPos = chunk.Pos.Add(Env.CHUNK_SIZE, 0, 0);
                            break;
                        default:
                            neighborPos = chunk.Pos.Add(-Env.CHUNK_SIZE, 0, 0);
                            break;
                    }
                }

                // Sync vertical neighbors
                if (neighborPos.x == chunk.Pos.x && neighborPos.z == chunk.Pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborPos.y > chunk.Pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.CHUNK_SIZE, -1);
                        chunk.Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborPos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, chunkSize1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        chunk.Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborPos.x == chunk.Pos.x && neighborPos.y == chunk.Pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborPos.z > chunk.Pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, 0);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, Env.CHUNK_SIZE);
                        for (int y = -1;
                             y < sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x < sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborPos.z < chunk.pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, chunkSize1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1;
                             y < sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x < sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborPos.y == chunk.Pos.y && neighborPos.z == chunk.Pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborPos.x > chunk.Pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(0, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(Env.CHUNK_SIZE, -1, -1);
                        for (int y = -1; y < sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z < sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborPos.x < chunk.pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(chunkSize1, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1; y < sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z < sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }
            }

            OnSynchronizeEdgesDone(chunk);
        }

        private static void OnSynchronizeEdgesDone(Chunk chunk)
        {
            chunk.completedStates |= ChunkState.SyncEdges;
            chunk.isSyncingEdges = false;
            chunk.taskRunning = false;
        }

        private bool SynchronizeEdges()
        {
            // Block while we're waiting for data to be generated
            if ((completedStates & ChunkState.Generate) == 0)
            {
                return true;
            }

            // Make sure all neighbors are generated
            if (!AreNeighborsGenerated())
            {
                return true;
            }

            pendingStates &= ~ChunkState.SyncEdges;
            completedStates &= ~ChunkState.SyncEdges;

            ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
            poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
            threadPoolItem = task;

            task.Set(ThreadID, actionOnSyncEdges, this);

            taskRunning = true;
            isSyncingEdges = true;
            WorkPoolManager.Add(task, false);
            return true;
        }

        #endregion

        #region Build collider geometry

        private static void OnBuildCollider(Chunk client)
        {
            client.ColliderGeometryHandler.Build();
            OnBuildColliderDone(client);
        }

        private static void OnBuildColliderDone(Chunk chunk)
        {
            chunk.completedStates |= ChunkStates.CURR_STATE_BUILD_COLLIDER;
            chunk.taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool BuildCollider()
        {
            // To save performance we generate collider on-demand
            if (!NeedsColliderGeometry)
            {
                return false; // Try the next step - build render geometry
            }

            // Block while we're waiting for data to be synchronized
            if ((completedStates & ChunkState.SyncEdges) == 0)
            {
                return true;
            }

            // Make sure all neighbors are synchronized
            if (!AreNeighborsSynchronized())
            {
                return true;
            }

            if (Blocks.nonEmptyBlocks > 0)
            {
                bool priority = (pendingStates & ChunkState.BuildColliderNow) != 0;

                pendingStates &= ~ChunkStates.CURR_STATE_BUILD_COLLIDER;
                completedStates &= ~ChunkStates.CURR_STATE_BUILD_COLLIDER;

                ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
                threadPoolItem = task;

                task.Set(
                    ThreadID,
                    actionOnBuildCollider,
                    this,
                    priority ? Globals.Watch.ElapsedTicks : long.MinValue
                );

                taskRunning = true;
                WorkPoolManager.Add(task, false);

                return true;
            }

            OnBuildColliderDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Build render geometry

        private static void OnBuildVertices(Chunk client)
        {
            client.RenderGeometryHandler.Build();
            OnBuildVerticesDone(client);
        }

        private static void OnBuildVerticesDone(Chunk chunk)
        {
            chunk.completedStates |= ChunkStates.CURR_STATE_BUILD_VERTICES;
            chunk.taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool BuildVertices()
        {
            // To save performance we generate geometry on-demand - when the chunk can be seen
            if (!NeedsRenderGeometry)
            {
                return false; // Try the next step - there's no next step :)
            }

            // Block while we're waiting for data to be synchronized
            if ((completedStates & ChunkState.SyncEdges) == 0)
            {
                return true;
            }

            // Make sure all neighbors are synchronized
            if (!AreNeighborsSynchronized())
            {
                return true;
            }

            if (Blocks.nonEmptyBlocks > 0)
            {
                bool priority = (pendingStates & ChunkState.BuildVerticesNow) != 0;

                pendingStates &= ~ChunkStates.CURR_STATE_BUILD_VERTICES;
                completedStates &= ~ChunkStates.CURR_STATE_BUILD_VERTICES;

                ThreadPoolItem<Chunk> task = Globals.MemPools.SMThreadPI.Pop();
                poolState = poolState.Set(ChunkPoolItemState.ThreadPI);
                threadPoolItem = task;

                task.Set(
                    ThreadID,
                    actionOnBuildVertices,
                    this,
                    priority ? Globals.Watch.ElapsedTicks : long.MinValue
                );

                taskRunning = true;
                WorkPoolManager.Add(task, priority);

                return true;
            }

            OnBuildVerticesDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Remove chunk

        private bool RemoveChunk()
        {
            // If chunk was loaded we need to wait for other states with higher priority to finish first
            if ((completedStates & ChunkState.LoadData) != 0)
            {
                // Wait until chunk is generated
                if ((completedStates & ChunkState.Generate) == 0)
                {
                    return false;
                }

                // Wait for save to complete if it was requested
                if (
                    (pendingStates & ChunkState.PrepareSaveData) != 0 ||
                    (pendingStates & ChunkState.SaveData) != 0
                    )
                {
                    return false;
                }

                pendingStates &= ~ChunkState.Remove;
            }

            completedStates |= ChunkState.Remove;
            return true;
        }

        #endregion Remove chunk

        #endregion
    }
}
