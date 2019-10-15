using UnityEngine;

[CreateAssetMenu(fileName = "New Cube Config", menuName = "Voxelmetric/Blocks/Cube")]
public class CubeConfigObject : BlockConfigObject
{
    public override object GetBlockClass()
    {
        return typeof(CubeBlock);
    }

    public override BlockConfig GetConfig()
    {
        return new BlockConfig()
        {
            typeInConfig = ID,
            solid = Solid,
            transparent = Transparent,
            name = BlockName
        };
    }
}
