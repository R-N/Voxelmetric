using System.Collections;

namespace Voxelmetric
{
    public struct LayerConfig
    {
        public string name;

        // This is used to sort the layers, low numbers are applied first
        // does not need to be consecutive so use numbers like 100 so that
        // layer you can add layers in between if you have to
        public int index;
        public string layerType;
        public string structure;
        public Hashtable properties;

        public static bool IsStructure(string structure)
        {
            return !string.IsNullOrEmpty(structure);
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is LayerConfig config)
            {
                return config.name == name && config.index == index && config.layerType == layerType && config.structure == structure;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * name.GetHashCode();
                hash = hash * 23 * index.GetHashCode();
                hash = hash * 23 * layerType.GetHashCode();
                hash = hash * 23 * structure.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(LayerConfig left, LayerConfig right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LayerConfig left, LayerConfig right)
        {
            return !(left == right);
        }
    }
}
