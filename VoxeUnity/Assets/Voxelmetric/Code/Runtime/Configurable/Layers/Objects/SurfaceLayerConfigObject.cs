using UnityEngine;

[CreateAssetMenu(fileName = "New Surface Layer", menuName = "Voxelmetric/Layers/Surface Layer")]
public class SurfaceLayerConfigObject : LayerConfigObject
{
    public override TerrainLayer GetLayer()
    {
        return new SurfaceLayer();
    }
}
