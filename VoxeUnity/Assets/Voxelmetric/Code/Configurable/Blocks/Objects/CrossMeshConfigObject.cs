using UnityEngine;

[CreateAssetMenu(fileName = "New Cross Mesh", menuName = "Voxelmetric/Blocks/Cross Mesh")]
public class CrossMeshConfigObject : BlockConfigObject
{
    [SerializeField]
    private Texture2D texture = null;

    public Texture2D Texture { get { return texture; } }

    public override object GetBlockClass()
    {
        return typeof(CrossMeshBlock);
    }

    public override BlockConfig GetConfig()
    {
        return new CrossMeshBlockConfig()
        {
            solid = Solid,
            transparent = Transparent,
            name = BlockName,
            typeInConfig = ID,
            Texture = texture,
            raycastHit = true,
            raycastHitOnRemoval = true
        };
    }

    public override Texture2D[] GetTextures()
    {
        return new Texture2D[1] { texture };
    }
}
