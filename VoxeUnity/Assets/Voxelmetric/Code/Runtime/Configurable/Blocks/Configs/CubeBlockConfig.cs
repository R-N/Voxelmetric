using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CubeBlockConfig : BlockConfig
{
    public TextureCollection[] textures;
    public Color32[] colors;

    public override bool OnSetUp(BlockConfigObject config, World world)
    {
        if (!base.OnSetUp(config, world))
        {
            return false;
        }

        if (config is CubeConfigObject cubeConfig)
        {
            textures = new TextureCollection[6];
            textures[0] = world.textureProvider.GetTextureCollection(cubeConfig.TopTexture);
            textures[1] = world.textureProvider.GetTextureCollection(cubeConfig.BottomTexture);
            textures[2] = world.textureProvider.GetTextureCollection(cubeConfig.BackTexture);
            textures[3] = world.textureProvider.GetTextureCollection(cubeConfig.FrontTexture);
            textures[4] = world.textureProvider.GetTextureCollection(cubeConfig.RightTexture);
            textures[5] = world.textureProvider.GetTextureCollection(cubeConfig.LeftTexture);

            colors = new Color32[6];
            colors[0] = cubeConfig.TopColor;
            colors[1] = cubeConfig.BottomColor;
            colors[2] = cubeConfig.BackColor;
            colors[3] = cubeConfig.FrontColor;
            colors[4] = cubeConfig.RightColor;
            colors[5] = cubeConfig.LeftColor;
        }
        else
        {
            Debug.LogError(config.GetType() + " config passed to cube block.");
            return false;
        }

        return true;
    }
}
