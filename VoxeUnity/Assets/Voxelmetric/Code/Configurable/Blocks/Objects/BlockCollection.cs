using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Block Collection", menuName = "Voxelmetric/Blocks/Block Collection", order = 0)]
public class BlockCollection : ScriptableObject
{
    [SerializeField]
    private BlockConfigObject[] blocks = new BlockConfigObject[0];

    public BlockConfigObject[] Blocks { get { return blocks; } }

    public List<Texture2D> GetAllUniqueTextures()
    {
        List<Texture2D> textures = new List<Texture2D>();
        for (int i = 0; i < blocks.Length; i++)
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
