using System.Collections;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig : BlockConfig
{
    public TextureCollection texture;

    private Texture2D texture2D;
    public Texture2D Texture
    {
        get { return texture2D; }
        set
        {
            texture2D = value;
            texture = World.Instance.textureProvider.GetTextureCollection(value != null ? value.name : "");
        }
    }

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
        {
            return false;
        }

        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        return true;
    }
}
