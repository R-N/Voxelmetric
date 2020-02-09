using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Absolute Layer", menuName = "Voxelmetric/Layers/Absolute Layer")]
    public class AbsoluteLayerConfigObject : LayerConfigObject
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
            return new AbsoluteLayer()
            {
                Exponent = exponent,
                Frequency = frequency,
                MinHeight = minHeight,
                MaxHeight = maxHeight
            };
        }
    }
}