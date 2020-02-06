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

    public Texture2D TopTexture { get { return topTexture; } set { topTexture = value; } }
    public Texture2D BottomTexture { get { return bottomTexture; } set { bottomTexture = value; } }
    public Texture2D FrontTexture { get { return frontTexture; } set { frontTexture = value; } }
    public Texture2D BackTexture { get { return backTexture; } set { backTexture = value; } }
    public Texture2D LeftTexture { get { return leftTexture; } set { leftTexture = value; } }
    public Texture2D RightTexture { get { return rightTexture; } set { rightTexture = value; } }

    public Color32 TopColor { get { return topColor; } set { topColor = value; } }
    public Color32 BottomColor { get { return bottomColor; } set { bottomColor = value; } }
    public Color32 FrontColor { get { return frontColor; } set { frontColor = value; } }
    public Color32 BackColor { get { return backColor; } set { backColor = value; } }
    public Color32 LeftColor { get { return leftColor; } set { leftColor = value; } }
    public Color32 RightColor { get { return rightColor; } set { rightColor = value; } }

    public override object GetBlockClass()
    {
        return typeof(CubeBlock);
    }

    public override BlockConfig GetConfig()
    {
        CubeBlockConfig config = new CubeBlockConfig()
        {
            raycastHit = true,
            raycastHitOnRemoval = true
        };

        return config;
    }

    public override Texture2D[] GetTextures()
    {
        return new Texture2D[6] { topTexture, bottomTexture, frontTexture, backTexture, rightTexture, leftTexture };
    }
}
