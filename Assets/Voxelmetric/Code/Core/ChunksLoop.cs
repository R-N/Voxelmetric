﻿using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    /// <summary>
    /// This class runs constantly running generation jobs for chunks. When chunks are added to one of the
    /// generation stages (chunk.generationStage) it should also be added to a list here and this 
    /// class will work through every list running the job for the relevant stage and pushing it to the
    /// next stage. Chunks can be added and forgotten because they will work their way to fully functioning
    /// chunks by the end.
    /// 
    /// Use ChunksInProgress to check the number of chunks in queue to be generated before adding a new one.
    /// There's no point in piling up the queue, better to wait, then add more.
    /// </summary>
    public class ChunksLoop
    {
        private World world;
        private readonly List<BlockPos> markedForDeletion = new List<BlockPos>();
        private Plane[] m_cameraPlanes = new Plane[6];

        public ChunksLoop(World world)
        {
            this.world = world;
        }

        public void Stop()
        {
        }

        public void Update()
        {
            Profiler.BeginSample("CameraPlanes");
            // Recalculate camera frustum planes
            Geometry.CalculateFrustumPlanes(Camera.main, ref m_cameraPlanes);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateTerrain");
            UpdateTerrain();
            Profiler.EndSample();
        }

        private void UpdateTerrain()
        {
            foreach (Chunk chunk in world.chunks.chunkCollection)
            {
                if (chunk.IsFinished())
                {
                    markedForDeletion.Add(chunk.pos);
                    continue;
                }

                if (world.UseFrustumCulling)
                {
                    bool isInsideFrustum = IsChunkInViewFrustum(chunk);
                    chunk.Visible = isInsideFrustum;
                    chunk.PossiblyVisible = isInsideFrustum;
                }
                else
                {
                    chunk.Visible = true;
                    chunk.PossiblyVisible = true;
                }

                chunk.UpdateChunk();
            }

            for (int i = 0; i<markedForDeletion.Count; i++)
            {
                BlockPos pos = markedForDeletion[i];
                Chunk chunk = world.chunks.Get(pos);
                world.chunks.Remove(pos);
                Chunk.RemoveChunk(chunk);
            }
            markedForDeletion.Clear();
        }

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return !world.UseFrustumCulling || GeometryUtility.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
        }
    }
}