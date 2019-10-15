using UnityEngine;

[CreateAssetMenu(fileName = "New Block Collection", menuName = "Voxelmetric/Blocks/Block Collection", order = 0)]
public class BlockCollection : ScriptableObject
{
    [SerializeField]
    private BlockConfigObject[] blocks = new BlockConfigObject[0];

    public BlockConfigObject[] Blocks { get { return blocks; } }
}
