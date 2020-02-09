using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric
{
    public partial class World : MonoBehaviour
    {
        public BlockCollection blocks;
        public LayerCollection layers;
        public WorldConfig config;

        //This world name is used for the save file name and as a seed for random noise
        public string worldName = "New World";

        public VmNetworking networking = new VmNetworking();

        public BlockProvider blockProvider;
        public TextureProvider textureProvider;
        public TerrainGen terrainGen;

        public Material[] renderMaterials;
        public PhysicMaterial[] physicsMaterials;

        public AABBInt Bounds { get; set; }

        private readonly List<ModifyBlockContext> modifyRangeQueue = new List<ModifyBlockContext>();

        private readonly object pendingStructureMutex = new object();
        private readonly Dictionary<Vector3Int, List<StructureContext>> pendingStructures = new Dictionary<Vector3Int, List<StructureContext>>();
        private readonly List<StructureInfo> pendingStructureInfo = new List<StructureInfo>();

        public static World Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (config == null)
            {
                Debug.LogError(gameObject.name + " needs to have a World Config assigned!");
                return;
            }

            StartWorld();
        }

        private void OnDestroy()
        {
            Serialization.ClearTemporary();
        }

        void OnApplicationQuit()
        {
            StopWorld();
        }

        private void SetFeatures()
        {
            Features.useThreadPool = config.UseThreadPool;
            Features.useThreadedIO = config.UseThreadedIO;
            Features.useSerialization = config.UseSerialization;
            Features.useGreedyMeshing = config.UseGreedyMeshing;
        }

        public void Configure()
        {
            VerifyConfig();

            //textureProvider = Voxelmetric.resources.GetTextureProvider(this);
            textureProvider = TextureProvider.Create();
            blockProvider = BlockProvider.Create();

            textureProvider.Init(blocks, config);
            blockProvider.Init(blocks, this);

            //TODO: make this configurable.
            Serialization.Initialize(Application.persistentDataPath + "/Voxelmetric/" + worldName, Application.persistentDataPath + "/Voxelmetric/.temp/");
            Serialization.CopyFromSaveLocationToTemp();

            int xSize = textureProvider.atlas.width / 128;
            int ySize = textureProvider.atlas.height / 128;

            foreach (Material renderMaterial in renderMaterials)
            {
                renderMaterial.mainTexture = textureProvider.atlas;
                renderMaterial.SetInt("_AtlasX", xSize);
                renderMaterial.SetInt("_AtlasY", ySize);
                renderMaterial.SetVector("_AtlasRec", new Vector4(1.0f / xSize, 1.0f / ySize));
            }
        }

        private void VerifyConfig()
        {
            // minX can't be greater then maxX
            if (config.MinX > config.MaxX)
            {
                int tmp = config.MinX;
                config.MaxX = config.MinX;
                config.MinX = tmp;
            }

            if (config.MinX != config.MaxX)
            {
                // Make sure there is at least one chunk worth of space in the world on the X axis
                if (config.MaxX - config.MinX < Env.CHUNK_SIZE)
                {
                    config.MaxX = config.MinX + Env.CHUNK_SIZE;
                }
            }

            // minY can't be greater then maxY
            if (config.MinY > config.MaxY)
            {
                int tmp = config.MinY;
                config.MaxY = config.MinY;
                config.MinY = tmp;
            }

            if (config.MinY != config.MaxY)
            {
                // Make sure there is at least one chunk worth of space in the world on the Y axis
                if (config.MaxY - config.MinY < Env.CHUNK_SIZE)
                {
                    config.MaxY = config.MinY + Env.CHUNK_SIZE;
                }
            }

            // minZ can't be greater then maxZ
            if (config.MinZ > config.MaxZ)
            {
                int tmp = config.MinZ;
                config.MaxZ = config.MinZ;
                config.MinZ = tmp;
            }

            if (config.MinZ != config.MaxZ)
            {
                // Make sure there is at least one chunk worth of space in the world on the Z axis
                if (config.MaxZ - config.MinZ < Env.CHUNK_SIZE)
                {
                    config.MaxZ = config.MinZ + Env.CHUNK_SIZE;
                }
            }
        }

        private void StartWorld()
        {
            SetFeatures();
            Configure();

            networking.StartConnections(this);
            terrainGen = TerrainGen.Create(this, layers);
        }

        private void StopWorld()
        {
            networking.EndConnections();
        }

        public void CapCoordXInsideWorld(ref int minX, ref int maxX)
        {
            if (config.MinX != config.MaxX)
            {
                minX = Mathf.Max(minX, config.MinX);
                maxX = Mathf.Min(maxX, config.MaxX);
            }
        }

        public void CapCoordYInsideWorld(ref int minY, ref int maxY)
        {
            if (config.MinY != config.MaxY)
            {
                minY = Mathf.Max(minY, config.MinY);
                maxY = Mathf.Min(maxY, config.MaxY);
            }
        }

        public void CapCoordZInsideWorld(ref int minZ, ref int maxZ)
        {
            if (config.MinZ != config.MaxZ)
            {
                minZ = Mathf.Max(minZ, config.MinZ);
                maxZ = Mathf.Min(maxZ, config.MaxZ);
            }
        }

        public bool IsCoordInsideWorld(ref Vector3Int pos)
        {
            return
                config.MinX == config.MaxX || (pos.x >= config.MinX && pos.x <= config.MaxX) ||
                config.MinY == config.MaxY || (pos.y >= config.MinY && pos.y <= config.MaxY) ||
                config.MinZ == config.MaxZ || (pos.z >= config.MinZ && pos.z <= config.MaxZ);
        }

        public void RegisterModifyRange(ModifyBlockContext onModified)
        {
            modifyRangeQueue.Add(onModified);
        }

        public void PerformBlockActions()
        {
            for (int i = 0; i < modifyRangeQueue.Count; i++)
            {
                modifyRangeQueue[i].PerformAction();
            }

            modifyRangeQueue.Clear();
        }

        public void RegisterPendingStructure(StructureInfo info, StructureContext context)
        {
            if (info == null || context == null)
            {
                return;
            }

            lock (pendingStructureMutex)
            {
                {
                    bool alreadyThere = false;

                    // Do not register the same thing twice
                    for (int i = 0; i < pendingStructureInfo.Count; i++)
                    {
                        if (pendingStructureInfo[i].Equals(info))
                        {
                            alreadyThere = true;
                            break;
                        }
                    }

                    if (!alreadyThere)
                    {
                        pendingStructureInfo.Add(info);
                    }
                }

                if (pendingStructures.TryGetValue(context.chunkPos, out List<StructureContext> list))
                {
                    list.Add(context);
                }
                else
                {
                    pendingStructures.Add(context.chunkPos, new List<StructureContext> { context });
                }
            }

            {
                Chunk chunk;
                lock (chunks)
                {
                    // Let the chunk know it needs an update if it exists
                    chunk = GetChunk(ref context.chunkPos);
                }
                if (chunk != null)
                {
                    chunk.needApplyStructure = true;
                }
            }
        }

        public void UnregisterPendingStructures()
        {
            // TODO: This is not exactly optimal. A lot of iterations for one mutex. On the other hand, I expect only
            // a small amount of structures stored here. Definitelly not hundreds or more. But there's a room for
            // improvement...
            lock (pendingStructureMutex)
            {
                // Let's see whether we can unload any positions
                for (int i = 0; i < pendingStructureInfo.Count;)
                {
                    StructureInfo info = pendingStructureInfo[i];
                    Vector3Int pos = info.chunkPos;

                    // See whether we can remove the structure
                    if (!Bounds.IsInside(ref pos))
                    {
                        pendingStructureInfo.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                        continue;
                    }

                    // Structure removed. We need to remove any associated world positions now
                    for (int y = info.bounds.minY; y < info.bounds.maxY; y += Env.CHUNK_SIZE)
                    {
                        for (int z = info.bounds.minZ; z < info.bounds.maxZ; z += Env.CHUNK_SIZE)
                        {
                            for (int x = info.bounds.minX; x < info.bounds.maxX; x += Env.CHUNK_SIZE)
                            {
                                if (!pendingStructures.TryGetValue(new Vector3Int(x, y, z), out List<StructureContext> list) || list.Count <= 0)
                                {
                                    continue;
                                }

                                // Remove any occurence of this structure from pending positions
                                for (int j = 0; j < list.Count;)
                                {
                                    if (list[j].id == info.id)
                                    {
                                        list.RemoveAt(j);
                                    }
                                    else
                                    {
                                        ++j;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ApplyPendingStructures(Chunk chunk)
        {
            // Check this unlocked first
            if (!chunk.needApplyStructure)
            {
                return;
            }

            List<StructureContext> list;
            int cnt;

            lock (pendingStructureMutex)
            {
                if (!chunk.needApplyStructure)
                {
                    return;
                }

                // Consume the event
                chunk.needApplyStructure = false;

                if (!pendingStructures.TryGetValue(chunk.Pos, out list))
                {
                    return;
                }

                cnt = list.Count;
            }

            // Apply changes to the chunk
            for (int i = chunk.maxPendingStructureListIndex; i < cnt; i++)
            {
                list[i].Apply(chunk);
            }

            chunk.maxPendingStructureListIndex = cnt - 1;
        }

        public bool CheckInsideWorld(Vector3Int pos)
        {
            int offsetX = (Bounds.maxX + Bounds.minX) >> 1;
            int offsetZ = (Bounds.maxZ + Bounds.minZ) >> 1;

            int xx = (pos.x - offsetX) / Env.CHUNK_SIZE;
            int zz = (pos.z - offsetZ) / Env.CHUNK_SIZE;
            int yy = pos.y / Env.CHUNK_SIZE;
            int horizontalRadius = (Bounds.maxX - Bounds.minX) / (2 * Env.CHUNK_SIZE);

            return ChunkLoadOrder.CheckXZ(xx, zz, horizontalRadius) &&
                   yy >= (Bounds.minY / Env.CHUNK_SIZE) && yy <= (Bounds.maxY / Env.CHUNK_SIZE);
        }
    }
}
