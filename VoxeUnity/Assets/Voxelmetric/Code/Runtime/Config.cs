using UnityEngine;

namespace Voxelmetric
{
    public static class Env
    {
        //! Size of chunk's side
        public const int CHUNK_POW = 5;

        //! Size of block when rendering
        public const float BLOCK_SIZE = 1f;

        #region DO NOT CHANGE THESE!

        public const float BLOCK_SIZE_HALF = BLOCK_SIZE / 2f;
        public const float BLOCK_SIZE_INV = 1f / BLOCK_SIZE;
        public static readonly Vector3 halfBlockOffset = new Vector3(BLOCK_SIZE_HALF, BLOCK_SIZE_HALF, BLOCK_SIZE_HALF);
        public static readonly Vector3 halfBlockOffsetInv = new Vector3(BLOCK_SIZE_INV, BLOCK_SIZE_INV, BLOCK_SIZE_INV);

        //! Padding added to the size of block faces to fix floating point issues
        //! where tiny gaps can appear between block faces
        public const float BLOCK_FACE_PADDING = 0.001f;

        public const int CHUNK_POW_2 = CHUNK_POW << 1;
        public const int CHUNK_MASK = (1 << CHUNK_POW) - 1;

        //! Internal chunk size including room for edge fields as well so that we do not have to check whether we are within chunk bounds.
        //! This means we will ultimately consume a bit more memory in exchange for more performance
        public const int CHUNK_PADDING = 1;
        public const int CHUNK_PADDING_2 = CHUNK_PADDING * 2;

        //! Visible chunk size
        public const int CHUNK_SIZE = (1 << CHUNK_POW) - 2 * CHUNK_PADDING;
        public const int CHUNK_SIZE_1 = CHUNK_SIZE - 1;
        public const int CHUNK_SIZE_POW_2 = CHUNK_SIZE * CHUNK_SIZE;
        public const int CHUNK_SIZE_POW_3 = CHUNK_SIZE * CHUNK_SIZE_POW_2;

        //! Internal chunk size (visible size + padding)
        public const int CHUNK_SIZE_PLUS_PADDING = CHUNK_SIZE + CHUNK_PADDING;
        public const int CHUNK_SIZE_WITH_PADDING = CHUNK_SIZE + CHUNK_PADDING * 2;
        public const int CHUNK_SIZE_WITH_PADDING_POW_2 = CHUNK_SIZE_WITH_PADDING * CHUNK_SIZE_WITH_PADDING;
        public const int CHUNK_SIZE_WITH_PADDING_POW_3 = CHUNK_SIZE_WITH_PADDING * CHUNK_SIZE_WITH_PADDING_POW_2;

        #endregion
    }

    public static class Features
    {
        //! A mask saying which world edges should not have their faces rendered
        public const Side DONT_RENDER_WORLD_EDGES_MASK = /*Side.up|*/Side.down | Side.north | Side.south | Side.west | Side.east;

        public static bool useThreadPool = true;
        public static bool useThreadedIO = true;

        public static bool useGreedyMeshing = true;

        //! If true, chunk serialization is enabled
        public static bool useSerialization = true;
        //! If true, chunk will be serialized when it's unloaded
        public static readonly bool serializeChunkWhenUnloading = useSerialization && true;
        //! If true, only difference form default-generated data will be stored
        //! If there is no change no serialization is performned unless UseDifferentialSerialization_ForceSaveHeaders is enabled
        public static readonly bool useDifferentialSerialization = useSerialization && true;
        //! If true, even if there is no difference in data, at least basic info about chunk structure is stored
        public static readonly bool useDifferentialSerialization_ForceSaveHeaders = useDifferentialSerialization && false;
    }
}
