using UnityEngine;

namespace Voxelmetric
{
    public enum VoxelTextureType { Opaque = 0, Cutout = 1, Transparent = 2 }

    public abstract class BlockConfigObject : ScriptableObject
    {
        [SerializeField]
        private string blockName = "New Block";
        [SerializeField]
        private ushort id = 1;
        [SerializeField]
        private bool solid = true;
        [SerializeField]
        private bool transparent = false;
        [SerializeField]
        private VoxelTextureType textureType = VoxelTextureType.Opaque;

        public string BlockName { get { return blockName; } }
        public ushort ID { get { return id; } }
        public bool Solid { get { return solid; } }
        public bool Transparent { get { return transparent; } }
        public VoxelTextureType TextureType { get { return textureType; } }

        public abstract BlockConfig GetConfig();

        public abstract object GetBlockClass();

        public virtual Texture2D[] GetTextures()
        {
            return new Texture2D[0];
        }
    }
}