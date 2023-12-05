using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voxelmetric
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
            Texture2D neutralTexture = new Texture2D(blocks.TextureSize, blocks.TextureSize)
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

                Assert.IsTrue(individualTextures[i].width == blocks.TextureSize, individualTextures[i].name + " width must be the same as texture size!");
                Assert.IsTrue(individualTextures[i].height == blocks.TextureSize, individualTextures[i].name + " height must be the same as texture size!");
                Assert.IsTrue(individualTextures[i].isReadable == true, individualTextures[i].name + " must be marked as readbale!");
            }

            // Generate atlas
            Texture2D packedTextures = new Texture2D(1, 1, TextureFormat.ARGB32, 0, false);
            Rect[] rects = packedTextures.PackTextures(individualTextures.ToArray(), 0, 8192, false);

            // Transfer over the pixels to another texture2d because PackTextures resets the texture format and useMipMaps settings
            atlas = new Texture2D(packedTextures.width, packedTextures.height, config.AtlasFormat, config.UseMipMaps);
            atlas.SetPixels(packedTextures.GetPixels(0, 0, packedTextures.width, packedTextures.height));
            atlas.filterMode = config.AtlasFiltering;
            atlas.Apply();

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

                //collection.AddTexture(uvs, new TextureConfig.Texture() { weight = 1, index = 0 });
                collection.AddTexture(new Vector2Int((int)(atlas.width * uvs.x / 128), (int)(atlas.height * uvs.y / 128)));

                index++;
            }
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

            textures.TryGetValue(textureName, out TextureCollection collection);
            return collection;
        }

    }
}
