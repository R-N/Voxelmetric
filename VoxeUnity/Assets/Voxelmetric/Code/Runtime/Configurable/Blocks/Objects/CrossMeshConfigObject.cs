using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Cross Mesh", menuName = "Voxelmetric/Blocks/Cross Mesh")]
    public class CrossMeshConfigObject : BlockConfigObject
    {
        [SerializeField]
        private ColoredTexture texture = new ColoredTexture(new Color32(255, 255, 255, 255));

        public ColoredTexture Texture { get { return texture; } set { texture = value; } }

        public override object GetBlockClass()
        {
            return typeof(CrossMeshBlock);
        }

        public override BlockConfig GetConfig()
        {
            return new CrossMeshBlockConfig()
            {
                RaycastHit = true,
                RaycastHitOnRemoval = true
            };
        }

        public override Texture2D[] GetTextures()
        {
            return new Texture2D[1] { texture.texture };
        }
    }
}