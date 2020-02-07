using UnityEngine;

[CreateAssetMenu(fileName = "New Cross Mesh", menuName = "Voxelmetric/Blocks/Cross Mesh")]
public class CrossMeshConfigObject : BlockConfigObject
{
    [SerializeField]
    private Texture2D texture = null;
    [SerializeField]
    private Color32 color = new Color32(255, 255, 255, 255);

    public Texture2D Texture { get { return texture; } }
    public Color32 Color { get { return color; } }

    public override object GetBlockClass()
    {
        return typeof(CrossMeshBlock);
    }

    public override BlockConfig GetConfig()
    {
        return new CrossMeshBlockConfig()
        {
            RaycastHit = true,
            RaycastHitOnRemoval = true
        };
    }

    public override Texture2D[] GetTextures()
    {
        return new Texture2D[1] { texture };
    }
}
