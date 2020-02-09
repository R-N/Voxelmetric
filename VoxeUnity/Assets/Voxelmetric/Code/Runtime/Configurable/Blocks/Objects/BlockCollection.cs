using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Block Collection", menuName = "Voxelmetric/Blocks/Block Collection", order = -100)]
    public class BlockCollection : ScriptableObject
    {
        [SerializeField]
        private int textureSize = 16;

#if UNITY_EDITOR
        [Space]
#endif

        [SerializeField]
        private List<BlockConfigObject> blocks = new List<BlockConfigObject>();

        public int TextureSize { get { return textureSize; } set { textureSize = value; } }

        public List<BlockConfigObject> Blocks { get { return blocks; } set { blocks = value; } }

        public List<Texture2D> GetAllUniqueTextures()
        {
            List<Texture2D> textures = new List<Texture2D>();
            for (int i = 0; i < blocks.Count; i++)
            {
                Texture2D[] blockTextures = blocks[i].GetTextures();

                for (int j = 0; j < blockTextures.Length; j++)
                {
                    if (!textures.Contains(blockTextures[j]))
                    {
                        textures.Add(blockTextures[j]);
                    }
                }
            }

            return textures;
        }
    }
}