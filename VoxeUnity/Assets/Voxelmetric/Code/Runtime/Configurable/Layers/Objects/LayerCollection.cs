using UnityEngine;

[CreateAssetMenu(fileName = "New Layer Collection", menuName = "Voxelmetric/Layers/Layer Collection", order = -100)]
public class LayerCollection : ScriptableObject
{
    [SerializeField]
    private LayerConfigObject[] layers = null;

    public LayerConfigObject[] Layers { get { return layers; } }
}
