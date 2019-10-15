using UnityEngine;

[CreateAssetMenu(fileName = "New Structure Layer", menuName = "Voxelmetric/Layers/Structure Layer")]
public class StructureLayerConfigObject : LayerConfigObject
{
    [SerializeField]
    private float chance = 0;
    [SerializeField]
    private string structure = string.Empty;

    public string Structure { get { return structure; } }

    public override TerrainLayer GetLayer()
    {
        return new StructureLayer()
        {
            Chance = chance
        };
    }

    public override bool IsStructure()
    {
        return true;
    }
}
