using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public class TextureProvider
    {
        WorldConfig config;
        //TextureConfig[] configs;
        BlockCollection blocks;

        //! Texture atlas
        public readonly Dictionary<string, TextureCollection> textures;
        //! Texture atlas
        public Texture2D atlas;

        private const string NO_TEXTURE_NAME = "Voxelmetric_No_Texture_Pick_Neutral";

        public static TextureProvider Create()
        {
            return new TextureProvider();
        }

        private TextureProvider()
        {
            textures = new Dictionary<string, TextureCollection>();
        }

        public void Init(BlockCollection blocks, WorldConfig config)
        {
            this.config = config;
            this.blocks = blocks;
            //configs = LoadAllTextures();
            LoadTextureIndex();
        }

        private void LoadTextureIndex()
        {
            List<Texture2D> individualTextures = blocks.GetAllUniqueTextures();
            Texture2D neutralTexture = new Texture2D(32, 32)
            {
                name = NO_TEXTURE_NAME
            };
            for (int x = 0; x < neutralTexture.width; x++)
            {
                for (int y = 0; y < neutralTexture.height; y++)
                {
                    neutralTexture.SetPixel(x, y, Color.white);
                }
            }
            neutralTexture.Apply(false);

            for (int i = 0; i < individualTextures.Count; i++)
            {
                if (individualTextures[i] == null)
                {
                    individualTextures[i] = neutralTexture;
                }

                if (!individualTextures[i].isReadable)
                {
                    Debug.LogError(individualTextures[i].name + " needs to be marked as readable!");
                    return;
                }
            }

            // Generate atlas
            Texture2D packedTextures = new Texture2D(8192, 8192);
            Rect[] rects = packedTextures.PackTextures(individualTextures.ToArray(), config.AtlasPadding, 8192, false);

            // Transfer over the pixels to another texture2d because PackTextures resets the texture format and useMipMaps settings
            atlas = new Texture2D(packedTextures.width, packedTextures.height, config.AtlasFormat, config.UseMipMaps);
            atlas.SetPixels(packedTextures.GetPixels(0, 0, packedTextures.width, packedTextures.height));
            atlas.filterMode = config.AtlasFiltering;

            List<Rect> nonrepeatingTextures = new List<Rect>();

            int index = 0;
            textures.Clear();

            for (int i = 0; i < individualTextures.Count; i++)
            {
                Rect uvs = rects[index];

                if (!textures.TryGetValue(individualTextures[i].name, out TextureCollection collection))
                {
                    collection = new TextureCollection(individualTextures[i].name, TextureConfigType.Simple);
                    textures.Add(individualTextures[i].name, collection);
                }

                collection.AddTexture(uvs, new TextureConfig.Texture() { weight = 1, index = 0 });

                nonrepeatingTextures.Add(rects[index]);

                index++;
            }

            //uPaddingBleed.BleedEdges(atlas, config.textureAtlasPadding, repeatingTextures.ToArray(), true);
            uPaddingBleed.BleedEdges(atlas, config.AtlasPadding, nonrepeatingTextures.ToArray(), false);
        }

        public TextureCollection GetTextureCollection(Texture2D texture)
        {
            string textureName = texture == null ? NO_TEXTURE_NAME : texture.name;
            return GetTextureCollection(textureName);
        }

        public TextureCollection GetTextureCollection(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
            {
                textureName = NO_TEXTURE_NAME;
            }

            if (textures.Keys.Count == 0)
            {
                LoadTextureIndex();
            }

            TextureCollection collection;
            textures.TryGetValue(textureName, out collection);
            return collection;
        }

    }
}
