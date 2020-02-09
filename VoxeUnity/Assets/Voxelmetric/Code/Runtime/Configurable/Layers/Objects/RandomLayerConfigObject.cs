using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Random Layer", menuName = "Voxelmetric/Layers/Random Layer")]
    public class RandomLayerConfigObject : LayerConfigObject
    {
        [SerializeField]
        private float chance = 0;

        public override TerrainLayer GetLayer()
        {
            return new RandomLayer()
            {
                Chance = chance
            };
        }
    }
}