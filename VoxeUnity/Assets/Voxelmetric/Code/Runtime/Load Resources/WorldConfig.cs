using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New World Config", menuName = "Voxelmetric/World Config")]
    public class WorldConfig : ScriptableObject
    {
#if UNITY_EDITOR
        [Header("World Size")]
#endif
        [SerializeField]
        private int minX = 0;
        [SerializeField]
        private int maxX = 0;
#if UNITY_EDITOR
        [Space]
#endif
        [SerializeField]
        private int minY = 0;
        [SerializeField]
        private int maxY = 640;
#if UNITY_EDITOR
        [Space]
#endif
        [SerializeField]
        private int minZ = 0;
        [SerializeField]
        private int maxZ = 0;

        public int MinX { get { return minX; } set { minX = value; } }
        public int MaxX { get { return maxX; } set { maxX = value; } }
        public int MinY { get { return minY; } set { minY = value; } }
        public int MaxY { get { return maxY; } set { maxY = value; } }
        public int MinZ { get { return minZ; } set { minZ = value; } }
        public int MaxZ { get { return maxZ; } set { maxZ = value; } }

#if UNITY_EDITOR
        [Header("Chunk Settings")]
#endif
        [SerializeField]
        private float randomUpdateFrequency = 1f;
        [SerializeField]
        private bool addAOToMesh = true;
        [SerializeField]
        private float aOStrength = 1f;

        public bool AddAOToMesh { get { return addAOToMesh; } set { addAOToMesh = value; } }
        public float AOStrength { get { return aOStrength; } set { aOStrength = value; } }
        public float RandomUpdateFrequency { get { return randomUpdateFrequency; } set { randomUpdateFrequency = value; } }

#if UNITY_EDITOR
        [Header("Texture Settings")]
#endif
        [SerializeField]
        private int atlasPadding = 32;
        [SerializeField]
        private FilterMode atlasFiltering = FilterMode.Point;
        [SerializeField]
        private TextureFormat atlasFormat = TextureFormat.ARGB32;
        [SerializeField]
        private bool useMipMaps = true;

        public int AtlasPadding { get { return atlasPadding; } set { atlasPadding = value; } }
        public FilterMode AtlasFiltering { get { return atlasFiltering; } set { atlasFiltering = value; } }
        public TextureFormat AtlasFormat { get { return atlasFormat; } set { atlasFormat = value; } }
        public bool UseMipMaps { get { return useMipMaps; } set { useMipMaps = value; } }

#if UNITY_EDITOR
        [Header("Voxelmetric Features")]
#endif
        [SerializeField]
        private bool useThreadPool = true;
        [SerializeField]
        private bool useThreadedIO = true;
        [SerializeField]
        private bool useSerialization = true;
        [SerializeField]
        private bool useGreedyMeshing = true;

        public bool UseThreadPool { get { return useThreadPool; } set { useThreadPool = value; } }
        public bool UseThreadedIO { get { return useThreadedIO; } set { useThreadedIO = value; } }
        public bool UseSerialization { get { return useSerialization; } set { useSerialization = value; } }
        public bool UseGreedyMeshing { get { return useGreedyMeshing; } set { useGreedyMeshing = value; } }

        public override string ToString()
        {
            return name;
        }
    }
}
