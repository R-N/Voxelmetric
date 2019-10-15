using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig : BlockConfig
{
    public TextureCollection texture;

    public override bool OnSetUp(BlockConfigObject config, World world)
    {
        if (!base.OnSetUp(config, world))
        {
            return false;
        }

        if (config is CrossMeshConfigObject crossMeshConfig)
        {
            texture = world.textureProvider.GetTextureCollection(crossMeshConfig.Texture);
        }
        else
        {
            Debug.LogError(config.GetType().Name + " config passed to cross mesh block.");
            return false;
        }

        return true;
    }
}
