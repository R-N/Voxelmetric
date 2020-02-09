using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Cube Config", menuName = "Voxelmetric/Blocks/Cube")]
    public class CubeConfigObject : BlockConfigObject
    {
        [SerializeField]
        private ColoredTexture topTexture = new ColoredTexture(new Color32(255, 255, 255, 255));
        [SerializeField]
        private ColoredTexture bottomTexture = new ColoredTexture(new Color32(255, 255, 255, 255));
        [SerializeField]
        private ColoredTexture frontTexture = new ColoredTexture(new Color32(255, 255, 255, 255));
        [SerializeField]
        private ColoredTexture backTexture = new ColoredTexture(new Color32(255, 255, 255, 255));
        [SerializeField]
        private ColoredTexture leftTexture = new ColoredTexture(new Color32(255, 255, 255, 255));
        [SerializeField]
        private ColoredTexture rightTexture = new ColoredTexture(new Color32(255, 255, 255, 255));

        public Texture2D TopTexture { get { return topTexture.texture; } set { topTexture.texture = value; } }
        public Texture2D BottomTexture { get { return bottomTexture.texture; } set { bottomTexture.texture = value; } }
        public Texture2D FrontTexture { get { return frontTexture.texture; } set { frontTexture.texture = value; } }
        public Texture2D BackTexture { get { return backTexture.texture; } set { backTexture.texture = value; } }
        public Texture2D LeftTexture { get { return leftTexture.texture; } set { leftTexture.texture = value; } }
        public Texture2D RightTexture { get { return rightTexture.texture; } set { rightTexture.texture = value; } }

        public Color32 TopColor { get { return topTexture.color; } set { topTexture.color = value; } }
        public Color32 BottomColor { get { return bottomTexture.color; } set { bottomTexture.color = value; } }
        public Color32 FrontColor { get { return frontTexture.color; } set { frontTexture.color = value; } }
        public Color32 BackColor { get { return backTexture.color; } set { backTexture.color = value; } }
        public Color32 LeftColor { get { return leftTexture.color; } set { leftTexture.color = value; } }
        public Color32 RightColor { get { return rightTexture.color; } set { rightTexture.color = value; } }

        public override object GetBlockClass()
        {
            return typeof(CubeBlock);
        }

        public override BlockConfig GetConfig()
        {
            CubeBlockConfig config = new CubeBlockConfig()
            {
                RaycastHit = true,
                RaycastHitOnRemoval = true
            };

            return config;
        }

        public override Texture2D[] GetTextures()
        {
            return new Texture2D[6] { topTexture.texture, bottomTexture.texture, frontTexture.texture, backTexture.texture, rightTexture.texture, leftTexture.texture };
        }
    }
}