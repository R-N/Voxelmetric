using UnityEngine;

[CreateAssetMenu(fileName = "New Custom Mesh", menuName = "Voxelmetric/Blocks/Custom Mesh")]
public class CustomMeshConfigObject : BlockConfigObject
{
    [SerializeField]
    private GameObject meshObject = null;
    [SerializeField]
    private Vector3 meshOffset = Vector3.zero;

    public override object GetBlockClass()
    {
        return typeof(CustomMeshBlock);
    }

    public override BlockConfig GetConfig()
    {
        if (meshObject == null)
        {
            Debug.LogError("Mesh Object can't be null on " + name);
            return null;
        }

        return new CustomMeshBlockConfig()
        {
            solid = Solid,
            transparent = Transparent,
            name = BlockName,
            typeInConfig = ID,
            raycastHit = true,
            raycastHitOnRemoval = true,
            meshGO = meshObject,
            MeshOffset = meshOffset
        };
    }
}
