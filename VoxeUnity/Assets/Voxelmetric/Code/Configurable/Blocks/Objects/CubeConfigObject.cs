using UnityEngine;

[CreateAssetMenu(fileName = "New Cube Config", menuName = "Voxelmetric/Blocks/Cube")]
public class CubeConfigObject : BlockConfigObject
{
    [SerializeField]
    private Texture2D topTexture = null;
    [SerializeField]
    private Color32 topColor = Color.white;
    [SerializeField]
    private Texture2D bottomTexture = null;
    [SerializeField]
    private Color32 bottomColor = Color.red;
    [SerializeField]
    private Texture2D frontTexture = null;
    [SerializeField]
    private Color32 frontColor = Color.blue;
    [SerializeField]
    private Texture2D backTexture = null;
    [SerializeField]
    private Color32 backColor = Color.yellow;
    [SerializeField]
    private Texture2D leftTexture = null;
    [SerializeField]
    private Color32 leftColor = Color.red;
    [SerializeField]
    private Texture2D rightTexture = null;
    [SerializeField]
    private Color32 rightColor = Color.green;

    public override object GetBlockClass()
    {
        return typeof(CubeBlock);
    }

    public override BlockConfig GetConfig()
    {
        CubeBlockConfig config = new CubeBlockConfig()
        {
            typeInConfig = ID,
            solid = Solid,
            transparent = Transparent,
            name = BlockName,
            raycastHit = true,
            raycastHitOnRemoval = true
        };

        config.SetTextures(topTexture, bottomTexture, frontTexture, backTexture, rightTexture, leftTexture);

        //config.colors[0] = topColor;
        //config.colors[1] = bottomColor;
        //config.colors[2] = backColor;
        //config.colors[3] = frontColor;
        //config.colors[4] = rightColor;
        //config.colors[5] = leftColor;

        return config;
    }

    public override Texture2D[] GetTextures()
    {
        return new Texture2D[6] { topTexture, bottomTexture, frontTexture, backTexture, rightTexture, leftTexture };
    }
}
