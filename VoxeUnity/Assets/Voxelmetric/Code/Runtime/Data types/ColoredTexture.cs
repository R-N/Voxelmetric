using UnityEngine;

namespace Voxelmetric
{
    [System.Serializable]
    public struct ColoredTexture
    {
        public Texture2D texture;
        public Color32 color;

        public ColoredTexture(Texture2D texture) : this()
        {
            this.texture = texture;
            color = new Color32(255, 255, 255, 255);
        }

        public ColoredTexture(Color32 color) : this()
        {
            texture = null;
            this.color = color;
        }

        public ColoredTexture(Texture2D texture, Color32 color)
        {
            this.texture = texture;
            this.color = color;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ColoredTexture c && c.texture == texture && c.color.r == color.r && c.color.g == color.g && c.color.b == color.b && c.color.a == color.a;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * texture.GetHashCode();
                hash = hash * 23 * color.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(ColoredTexture left, ColoredTexture right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColoredTexture left, ColoredTexture right)
        {
            return !(left == right);
        }
    }
}