using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Additive Layer", menuName = "Voxelmetric/Layers/Additive Layer")]
    public class AdditiveLayerConfigObject : LayerConfigObject
    {
        [SerializeField]
        private float frequency = 0f;
        [SerializeField]
        private float exponent = 0f;
        [SerializeField]
        private int minHeight = 0;
        [SerializeField]
        private int maxHeight = 0;

        public override TerrainLayer GetLayer()
        {
            return new AdditiveLayer()
            {
                Exponent = exponent,
                Frequency = frequency,
                MinHeight = minHeight,
                MaxHeight = maxHeight
            };
        }
    }
}