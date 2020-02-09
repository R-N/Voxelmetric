using System;

namespace Voxelmetric
{
    public abstract class TerrainLayer : IComparable, IEquatable<TerrainLayer>
    {
        protected World world;
        protected TerrainGen terrainGen;
        protected NoiseWrapper noise;
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
        protected NoiseWrapperSIMD noiseSIMD;
#endif

        public string layerName = string.Empty;
        public int Index { get; private set; }
        public bool IsStructure { get; private set; }

        public NoiseWrapper Noise { get { return noise; } }
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
        public NoiseWrapperSIMD NoiseSIMD { get {return noiseSIMD;} }
#endif

        public void BaseSetUp(LayerConfigObject config, World world, TerrainGen terrainGen)
        {
            this.terrainGen = terrainGen;
            layerName = config.LayerName;
            IsStructure = config.IsStructure();
            this.world = world;
            Index = config.Index;

            noise = new NoiseWrapper(world.worldName);
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
            noiseSIMD = new NoiseWrapperSIMD(world.name);
#endif

            SetUp(config);
        }

        protected virtual void SetUp(LayerConfigObject config) { }

        public virtual void Init(LayerConfigObject config) { }

        public virtual void PreProcess(Chunk chunk, int layerIndex) { }
        public virtual void PostProcess(Chunk chunk, int layerIndex) { }

        /// <summary>
        /// Retrieves the height on given coordinates
        /// </summary>
        /// <param name="chunk">Chunk for which we search for height</param>
        /// <param name="layerIndex">Index of layer generating this structure</param>
        /// <param name="x">Position on the x-axis in local coordinates</param>
        /// <param name="z">Position on the z-axis in local coordinates</param>
        /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
        /// <param name="strength">How much features are pronounced</param>
        /// <returns>List of chunks waiting to be saved.</returns>
        public abstract float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength);

        /// <summary>
        /// Retrieves the height on given coordinates and if possible, updates the block within chunk based on the layer's configuration
        /// </summary>
        /// <param name="chunk">Chunk for which we search for height</param>
        /// <param name="layerIndex">Index of layer generating this structure</param>
        /// <param name="x">Position on the x-axis in local coordinates</param>
        /// <param name="z">Position on the z-axis in local coordinates</param>
        /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
        /// <param name="strength">How much features are pronounced</param>
        /// <returns>List of chunks waiting to be saved.</returns>
        public abstract float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength);

        /// <summary>
        /// Called once for each chunk. Should generate any
        /// parts of the structure within the chunk using GeneratedStructure.
        /// </summary>
        /// <param name="chunk">Chunk for which structures are to be generated</param>
        /// <param name="layerIndex">Index of layer generating this structure</param>
        public virtual void GenerateStructures(Chunk chunk, int layerIndex)
        {
        }

        /// <summary>
        /// Fills chunk with layer data starting at startPlaceHeight and ending at endPlaceHeight
        /// </summary>
        /// <param name="chunk">Chunk filled with data</param>
        /// <param name="x">Position on x axis in local coordinates</param>
        /// <param name="z">Position on z axis in local coordinates</param>
        /// <param name="startPlaceHeight">Starting position on y axis in world coordinates</param>
        /// <param name="endPlaceHeight">Ending position on y axis in world coordinates</param>
        /// <param name="blockData">Block data to set</param>
        protected static void SetBlocks(Chunk chunk, int x, int z, int startPlaceHeight, int endPlaceHeight, BlockData blockData)
        {
            int chunkY = chunk.Pos.y;
            int chunkYMax = chunkY + Env.CHUNK_SIZE;

            int y = startPlaceHeight > chunkY ? startPlaceHeight : chunkY;
            int yMax = endPlaceHeight < chunkYMax ? endPlaceHeight : chunkYMax;

            ChunkBlocks blocks = chunk.Blocks;
            int index = Helpers.GetChunkIndex1DFrom3D(x, y - chunkY, z);
            while (y++ < yMax)
            {
                blocks.SetRaw(index, blockData);
                index += Env.CHUNK_SIZE_WITH_PADDING_POW_2;
            }
        }

        #region Object-level comparison

        public int CompareTo(object obj)
        {
            return Index.CompareTo(((TerrainLayer)obj).Index);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is TerrainLayer))
            {
                return false;
            }

            TerrainLayer other = (TerrainLayer)obj;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public bool Equals(TerrainLayer other)
        {
            return other.Index == Index;
        }

        public static bool operator ==(TerrainLayer left, TerrainLayer right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(TerrainLayer left, TerrainLayer right)
        {
            return !(left == right);
        }

        public static bool operator <(TerrainLayer left, TerrainLayer right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(TerrainLayer left, TerrainLayer right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(TerrainLayer left, TerrainLayer right)
        {
            return left is object && left.CompareTo(right) > 0;
        }

        public static bool operator >=(TerrainLayer left, TerrainLayer right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        #endregion
    }
}
