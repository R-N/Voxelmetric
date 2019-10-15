using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CubeBlockConfig : BlockConfig
{
    public TextureCollection[] textures;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
        {
            return false;
        }

        textures = new TextureCollection[6];
        JArray textureNames = (JArray)JsonConvert.DeserializeObject(config["textures"].ToString());

        for (int i = 0; i < 6; i++)
        {
            textures[i] = world.textureProvider.GetTextureCollection(textureNames[i].ToObject<string>());
        }

        return true;
    }

    public void SetTextures(Texture2D top, Texture2D bottom, Texture2D front, Texture2D back, Texture2D right, Texture2D left)
    {
        textures = new TextureCollection[6];

        textures[0] = World.Instance.textureProvider.GetTextureCollection(top != null ? top.name : "");
        textures[1] = World.Instance.textureProvider.GetTextureCollection(bottom != null ? bottom.name : "");
        textures[2] = World.Instance.textureProvider.GetTextureCollection(back != null ? back.name : "");
        textures[3] = World.Instance.textureProvider.GetTextureCollection(front != null ? front.name : "");
        textures[4] = World.Instance.textureProvider.GetTextureCollection(right != null ? right.name : "");
        textures[5] = World.Instance.textureProvider.GetTextureCollection(left != null ? left.name : "");
    }
}
