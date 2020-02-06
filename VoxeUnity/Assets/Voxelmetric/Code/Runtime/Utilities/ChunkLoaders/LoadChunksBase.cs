using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Chunk = Voxelmetric.Code.Core.Chunk;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    public abstract class LoadChunksBase : MonoBehaviour
    {
        protected const int HORIZONTAL_MIN_RANGE = 0;
        protected const int HORIZONTAL_MAX_RANGE = 32;
        protected const int HORIZONTAL_DEF_RANGE = 6;
        protected const int VERTICAL_MIN_RANGE = 0;
        protected const int VERTICAL_MAX_RANGE = 32;
        protected const int VERTICAL_DEF_RANGE = 3;

        //! The world we are attached to
        protected World world;
        //! The camera against which we perform frustrum checks
        protected Camera myCamera;
        //! Position of the camera when the game started
        protected Vector3 cameraStartPos;

        //! Distance in chunks for loading chunks
        [Range(HORIZONTAL_MIN_RANGE, HORIZONTAL_MAX_RANGE)]
        public int horizontalChunkLoadRadius = HORIZONTAL_DEF_RANGE;
        //! Distance in chunks for loading chunks
        [Range(VERTICAL_MIN_RANGE, VERTICAL_MAX_RANGE)]
        public int verticalChunkLoadRadius = VERTICAL_DEF_RANGE;
        //! Makes the world regenerate around the attached camera. If false, X sticks at 0.
        public bool followCameraX = true;
        //! Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool followCameraY = false;
        //! Makes the world regenerate around the attached camera. If false, Z sticks at 0.
        public bool followCameraZ = true;
        //! Toogles frustum culling
        public bool useFrustumCulling = true;

        public bool diag_DrawWorldBounds = false;
        public bool diag_DrawLoadRange = false;

        protected int chunkHorizontalLoadRadiusPrev;
        protected int chunkVerticalLoadRadiusPrev;

        protected Vector3Int[] chunkPositions;
        protected readonly Plane[] cameraPlanes = new Plane[6];
        protected Vector3Int viewerPos;
        protected Vector3Int viewerPosPrev;

        //! A list of chunks to update
        protected readonly List<Chunk> updateRequests = new List<Chunk>();

        protected virtual void OnPreProcessChunks() { }
        protected virtual void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ) { }
        protected abstract void OnProcessChunk(Chunk chunk);

        void Awake()
        {
            myCamera = GetComponent<Camera>();
        }

        void Start()
        {
            world = World.Instance;

            chunkHorizontalLoadRadiusPrev = horizontalChunkLoadRadius;
            chunkVerticalLoadRadiusPrev = verticalChunkLoadRadius;

            cameraStartPos = myCamera.transform.position;

            UpdateViewerPosition();

            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
            viewerPosPrev += Vector3Int.one;
        }

        void Update()
        {
            Globals.GeometryBudget.Reset();
            Globals.SetBlockBudget.Reset();

            PreProcessChunks();
            PostProcessChunks();
            ProcessChunks();
        }

        public void PreProcessChunks()
        {
            Profiler.BeginSample("PreProcessChunks");

            // Recalculate camera frustum planes
            Planes.CalculateFrustumPlanes(myCamera, cameraPlanes);

            // Update clipmap based on range values
            UpdateRanges();

            // Update viewer position
            UpdateViewerPosition();

            OnPreProcessChunks();

            Profiler.EndSample();
        }

        public void PostProcessChunks()
        {
            int minX = viewerPos.x - (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxX = viewerPos.x + (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int minY = viewerPos.y - (verticalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxY = viewerPos.y + (verticalChunkLoadRadius * Env.CHUNK_SIZE);
            int minZ = viewerPos.z - (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxZ = viewerPos.z + (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            world.CapCoordXInsideWorld(ref minX, ref maxX);
            world.CapCoordYInsideWorld(ref minY, ref maxY);
            world.CapCoordZInsideWorld(ref minZ, ref maxZ);

            world.Bounds = new AABBInt(minX, minY, minZ, maxX, maxY, maxZ);

            int expectedChunks = chunkPositions.Length * ((maxY - minY + Env.CHUNK_SIZE) / Env.CHUNK_SIZE);

            if (// No update necessary if there was no movement
                viewerPos == viewerPosPrev &&
                // However, we need to make sure that we have enough chunks loaded
                world.Count >= expectedChunks)
            {
                return;
            }

            // Unregister any non-necessary pending structures
            Profiler.BeginSample("UnregisterStructures");
            {
                world.UnregisterPendingStructures();
            }
            Profiler.EndSample();

            // Cycle through the array of positions
            Profiler.BeginSample("PostProcessChunks");
            {
                // Cycle through the array of positions
                for (int y = maxY; y >= minY; y -= Env.CHUNK_SIZE)
                {
                    for (int i = 0; i < chunkPositions.Length; i++)
                    {
                        // Skip loading chunks which are off limits
                        int cx = (chunkPositions[i].x * Env.CHUNK_SIZE) + viewerPos.x;
                        if (cx > maxX || cx < minX)
                        {
                            continue;
                        }

                        int cy = (chunkPositions[i].y * Env.CHUNK_SIZE) + y;
                        if (cy > maxY || cy < minY)
                        {
                            continue;
                        }

                        int cz = (chunkPositions[i].z * Env.CHUNK_SIZE) + viewerPos.z;
                        if (cz > maxZ || cz < minZ)
                        {
                            continue;
                        }

                        // Create a new chunk if possible
                        Vector3Int newChunkPos = new Vector3Int(cx, cy, cz);
                        if (!world.CreateChunk(ref newChunkPos, out Chunk chunk))
                        {
                            continue;
                        }

                        updateRequests.Add(chunk);
                    }
                }
            }
            Profiler.EndSample();
        }

        private void HandleVisibility()
        {
            if (!useFrustumCulling)
            {
                return;
            }

            Profiler.BeginSample("CullPrepare1");

            // Make everything invisible by default
            foreach (Chunk ch in updateRequests)
            {
                ch.PossiblyVisible = false;
                ch.NeedsRenderGeometry = false;
            }

            Profiler.EndSample();

            int minX = viewerPos.x - (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxX = viewerPos.x + (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int minY = viewerPos.y - (verticalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxY = viewerPos.y + (verticalChunkLoadRadius * Env.CHUNK_SIZE);
            int minZ = viewerPos.z - (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            int maxZ = viewerPos.z + (horizontalChunkLoadRadius * Env.CHUNK_SIZE);
            world.CapCoordXInsideWorld(ref minX, ref maxX);
            world.CapCoordYInsideWorld(ref minY, ref maxY);
            world.CapCoordZInsideWorld(ref minZ, ref maxZ);

            minX /= Env.CHUNK_SIZE;
            maxX /= Env.CHUNK_SIZE;
            minY /= Env.CHUNK_SIZE;
            maxY /= Env.CHUNK_SIZE;
            minZ /= Env.CHUNK_SIZE;
            maxZ /= Env.CHUNK_SIZE;

            // TODO: Merge this with clipmap
            // Let's update chunk visibility info. Operate in chunk load radius so we know we're never outside cached range
            UpdateVisibility(minX, minY, minZ, maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
        }

        public void ProcessChunks()
        {
            Profiler.BeginSample("ProcessChunks");

            HandleVisibility();

            // Process removal requests
            for (int i = 0; i < updateRequests.Count;)
            {
                Chunk chunk = updateRequests[i];

                OnProcessChunk(chunk);

                // Update the chunk if possible
                if (chunk.Update())
                {
                    // Build geometry if there is enough time
                    if (Globals.GeometryBudget.HasTimeBudget)
                    {
                        Globals.GeometryBudget.StartMeasurement();

                        bool wasBuilt = chunk.UpdateCollisionGeometry();
                        wasBuilt |= chunk.UpdateRenderGeometry();
                        if (wasBuilt)
                        {
                            Globals.GeometryBudget.StopMeasurement();
                        }
                    }
                }

                // Automatically collect chunks which are ready to be removed from the world
                if (chunk.IsStateCompleted(ChunkState.Remove))
                {
                    // Remove the chunk from our provider and unregister it from chunk storage
                    world.RemoveChunk(chunk);

                    // Unregister from updates
                    updateRequests.RemoveAt(i);
                    continue;
                }

                ++i;
            }

            world.PerformBlockActions();

            Profiler.EndSample();
        }

        // Updates our clipmap region. Has to be set from the outside!
        private void UpdateRanges()
        {
            // Make sure horizontal ranges are always correct
            horizontalChunkLoadRadius = Mathf.Max(HORIZONTAL_MIN_RANGE, horizontalChunkLoadRadius);
            horizontalChunkLoadRadius = Mathf.Min(HORIZONTAL_MAX_RANGE, horizontalChunkLoadRadius);

            // Make sure vertical ranges are always correct
            verticalChunkLoadRadius = Mathf.Max(VERTICAL_MIN_RANGE, verticalChunkLoadRadius);
            verticalChunkLoadRadius = Mathf.Min(VERTICAL_MAX_RANGE, verticalChunkLoadRadius);

            bool isDifferenceXZ = horizontalChunkLoadRadius != chunkHorizontalLoadRadiusPrev || chunkPositions == null;
            bool isDifferenceY = verticalChunkLoadRadius != chunkVerticalLoadRadiusPrev;
            chunkHorizontalLoadRadiusPrev = horizontalChunkLoadRadius;
            chunkVerticalLoadRadiusPrev = verticalChunkLoadRadius;

            // Rebuild precomputed chunk positions
            if (isDifferenceXZ)
            {
                chunkPositions = ChunkLoadOrder.ChunkPositions(horizontalChunkLoadRadius);
            }
            // Invalidate prev pos so that updated ranges can take effect right away
            if (isDifferenceXZ || isDifferenceY ||
                horizontalChunkLoadRadius != chunkHorizontalLoadRadiusPrev ||
                verticalChunkLoadRadius != chunkVerticalLoadRadiusPrev)
            {
                OnUpdateRanges();

                viewerPosPrev = viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        protected virtual void OnUpdateRanges()
        {
        }

        private void UpdateViewerPosition()
        {
            Vector3Int chunkPos = transform.position;
            Vector3Int pos = Helpers.ContainingChunkPos(ref chunkPos);

            // Update the viewer position
            viewerPosPrev = viewerPos;

            // Do not let y overflow
            int x = viewerPos.x;
            if (followCameraX)
            {
                x = pos.x;
                world.CapCoordXInsideWorld(ref x, ref x);
            }

            // Do not let y overflow
            int y = viewerPos.y;
            if (followCameraY)
            {
                y = pos.y;
                world.CapCoordYInsideWorld(ref y, ref y);
            }

            // Do not let y overflow
            int z = viewerPos.z;
            if (followCameraZ)
            {
                z = pos.z;
                world.CapCoordZInsideWorld(ref z, ref z);
            }

            viewerPos = new Vector3Int(x, y, z);
        }
    }
}
