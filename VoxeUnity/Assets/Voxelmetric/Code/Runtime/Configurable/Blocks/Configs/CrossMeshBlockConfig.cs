using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig : BlockConfig
{
    public TextureCollection texture;
    public Color32 color;

    public override bool OnSetUp(BlockConfigObject config, World world)
    {
        if (!base.OnSetUp(config, world))
        {
            return false;
        }

        if (config is CrossMeshConfigObject crossMeshConfig)
        {
            texture = world.textureProvider.GetTextureCollection(crossMeshConfig.Texture);
            color = crossMeshConfig.Color;
        }
        else
        {
            Debug.LogError(config.GetType().Name + " config passed to cross mesh block.");
            return false;
        }

        return true;
    }
}
