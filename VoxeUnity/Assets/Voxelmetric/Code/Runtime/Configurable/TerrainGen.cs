using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Utilities.Noise;

public class TerrainGen
{
    public TerrainLayer[] TerrainLayers { get; private set; }
    public TerrainLayer[] StructureLayers { get; private set; }

    public static TerrainGen Create(World world, LayerCollection layers)
    {
        TerrainGen provider = new TerrainGen();
        provider.Init(world, layers);
        return provider;
    }

    protected TerrainGen()
    {
    }

    protected void Init(World world, LayerCollection layers)
    {
        // Verify all correct layers
        ProcessConfigs(world, layers);
    }

    private void ProcessConfigs(World world, LayerCollection layers)
    {
        List<LayerConfigObject> layersConfigs = new List<LayerConfigObject>(layers.Layers);

        // Terrain layers
        List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
        List<int> terrainLayersIndexes = new List<int>(); // could be implemented as a HashSet, however, it would be insane storing hundreads of layers here

        // Structure layers
        List<TerrainLayer> structLayers = new List<TerrainLayer>();
        List<int> structLayersIndexes = new List<int>(); // could be implemented as a HashSet, however, it would be insane storing hundreads of layers here

        for (int i = 0; i < layers.Layers.Length;)
        {
            LayerConfigObject config = layers.Layers[i];

            // Set layers up
            TerrainLayer layer = config.GetLayer();
            layer.BaseSetUp(config, world, this);

            if (layer.IsStructure)
            {
                // Do not allow any two layers share the same index
                if (structLayersIndexes.Contains(layer.Index))
                {
                    Debug.LogError("Could not create structure layer " + config.LayerName + ". Index " + layer.Index.ToString() + " already defined");
                    layersConfigs.RemoveAt(i);
                    continue;
                }

                // Add layer to layers list
                structLayers.Add(layer);
                structLayersIndexes.Add(layer.Index);
            }
            else
            {
                // Do not allow any two layers share the same index
                if (terrainLayersIndexes.Contains(layer.Index))
                {
                    Debug.LogError("Could not create terrain layer " + config.LayerName + ". Index " + layer.Index.ToString() + " already defined");
                    layersConfigs.RemoveAt(i);
                    continue;
                }

                // Add layer to layers list
                terrainLayers.Add(layer);
                terrainLayersIndexes.Add(layer.Index);
            }

            ++i;
        }

        // Call OnInit for each layer now that they all have been set up. Thanks to this, layers can
        // e.g. address other layers knowing that they will be able to access all data they need.
        int ti = 0, si = 0;
        for (int i = 0; i < layersConfigs.Count; i++)
        {
            LayerConfigObject config = layersConfigs[i];
            if (config.IsStructure())
            {
                structLayers[si++].Init(config);
            }
            else
            {
                terrainLayers[ti++].Init(config);
            }
        }

        // Sort the layers by index
        TerrainLayers = terrainLayers.ToArray();
        Array.Sort(TerrainLayers);
        StructureLayers = structLayers.ToArray();
        Array.Sort(StructureLayers);

        // Register support for noise functionality with each workpool thread
        for (int i = 0; i < Globals.WorkPool.Size; i++)
        {
            Voxelmetric.Code.Common.MemoryPooling.LocalPools pool = Globals.WorkPool.GetPool(i);
            pool.noiseItems = new NoiseItem[layersConfigs.Count];
            for (int j = 0; j < layersConfigs.Count; j++)
            {
                pool.noiseItems[j] = new NoiseItem
                {
                    noiseGen = new NoiseInterpolator()
                };
            }
        }
    }

    public void GenerateTerrain(Chunk chunk)
    {
        // Do some layer preprocessing on a chunk
        for (int i = 0; i < TerrainLayers.Length; i++)
        {
            TerrainLayers[i].PreProcess(chunk, i);
        }

        /* // DEBUG CODE
        for(int y=0; y<Env.ChunkSize; y++)
            for (int z = 0; z < Env.ChunkSize; z++)
                for (int x = 0; x<Env.ChunkSize; x++)
                {
                    int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                    chunk.blocks.SetRaw(index, new BlockData(4, true));
                }
        */
        // Generate terrain and structures
        GenerateTerrainForChunk(chunk);
        GenerateStructuresForChunk(chunk);

        // Do some layer postprocessing on a chunk
        for (int i = 0; i < TerrainLayers.Length; i++)
        {
            TerrainLayers[i].PostProcess(chunk, i);
        }
    }

    /// <summary>
    /// Retrieves the terrain height in a given chunk on given coordinates
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    /// <param name="x">Position on the x axis in local coordinates</param>
    /// <param name="z">Position on the z axis in local coordinates</param>
    public float GetTerrainHeightForChunk(Chunk chunk, int x, int z)
    {
        float height = 0f;
        for (int i = 0; i < TerrainLayers.Length; i++)
        {
            height = TerrainLayers[i].GetHeight(chunk, i, x, z, height, 1f);
        }

        return height;
    }

    /// <summary>
    /// Generates terrain for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    public void GenerateTerrainForChunk(Chunk chunk)
    {
        int maxY = chunk.Pos.y + Env.CHUNK_SIZE;
        for (int z = 0; z < Env.CHUNK_SIZE; z++)
        {
            for (int x = 0; x < Env.CHUNK_SIZE; x++)
            {
                float height = 0f;
                for (int i = 0; i < TerrainLayers.Length; i++)
                {
                    height = TerrainLayers[i].GenerateLayer(chunk, i, x, z, height, 1f);
                }
            }
        }
    }

    /// <summary>
    /// Generates structures for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which structures are generated</param>
    public void GenerateStructuresForChunk(Chunk chunk)
    {
        for (int i = 0; i < StructureLayers.Length; i++)
        {
            StructureLayers[i].GenerateStructures(chunk, i);
        }
    }
}
