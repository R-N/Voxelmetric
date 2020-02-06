using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Load_Resources.Blocks
{
    public class BlockProvider
    {
        //! Air type block will always be present
        public const ushort AIR_TYPE = 0;
        public static readonly BlockData airBlock = new BlockData(AIR_TYPE, false);

        //! Special reserved block types
        public static readonly ushort firstReservedSimpleType = 1;
        public static readonly ushort lastReservedSimpleType = (ushort)(firstReservedSimpleType + 254);
        public static readonly ushort lastReservedType = lastReservedSimpleType;
        public static readonly ushort firstCustomType = (ushort)(lastReservedType + 1);

        //! An array of loaded block configs
        private BlockConfig[] configs;

        //! Mapping from config's name to type
        private readonly Dictionary<string, ushort> names;
        //! Mapping from typeInConfig to type
        private ushort[] types;

        public Block[] BlockTypes { get; private set; }

        public static BlockProvider Create()
        {
            return new BlockProvider();
        }

        private BlockProvider()
        {
            names = new Dictionary<string, ushort>();
        }

        public void Init(BlockCollection blocks, World world)
        {
            // Add all the block definitions defined in the config files
            ProcessConfigs(world, blocks.Blocks);

            // Build block type lookup table
            BlockTypes = new Block[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                BlockConfig config = configs[i];

                Block block = (Block)Activator.CreateInstance(config.BlockClass);
                block.Init((ushort)i, config);
                BlockTypes[i] = block;
            }

            // Once all blocks are set up, call OnInit on them. It is necessary to do it in a separate loop
            // in order to ensure there will be no dependency issues.
            for (int i = 0; i < BlockTypes.Length; i++)
            {
                Block block = BlockTypes[i];
                block.OnInit(this);
            }

            // Add block types from config
            foreach (BlockConfig configFile in configs)
            {
                configFile.OnPostSetUp(world);
            }
        }

        // World is only needed for setting up the textures
        private void ProcessConfigs(World world, BlockConfigObject[] blocks)
        {
            List<BlockConfig> configs = new List<BlockConfig>(blocks.Length);
            Dictionary<ushort, ushort> types = new Dictionary<ushort, ushort>();

            // Add reserved block types
            AddBlockType(configs, types, BlockConfig.CreateAirBlockConfig());
            //for (ushort i = 1; i <= LastReservedSimpleType; i++)
            //{
            //    AddBlockType(configs, types, BlockConfig.CreateColorBlockConfig(world, i));
            //}

            // Add block types from config
            for (int i = 0; i < blocks.Length; i++)
            {
                BlockConfig config = blocks[i].GetConfig();
                config.BlockClass = (Type)blocks[i].GetBlockClass();

                if (!config.OnSetUp(blocks[i], world))
                {
                    continue;
                }

                if (!VerifyBlockConfig(types, config))
                {
                    continue;
                }

                AddBlockType(configs, types, config);
            }

            this.configs = configs.ToArray();

            // Now iterate over configs and find the one with the highest TypeInConfig
            ushort maxTypeInConfig = lastReservedType;
            for (int i = 0; i < this.configs.Length; i++)
            {
                if (this.configs[i].TypeInConfig > maxTypeInConfig)
                {
                    maxTypeInConfig = this.configs[i].TypeInConfig;
                }
            }

            // Allocate maxTypeInConfigs big array now and map config types to runtime types
            this.types = new ushort[maxTypeInConfig + firstCustomType];
            for (ushort i = 0; i < this.configs.Length; i++)
            {
                this.types[this.configs[i].TypeInConfig] = i;
            }
        }

        private bool VerifyBlockConfig(Dictionary<ushort, ushort> types, BlockConfig config)
        {
            // Unique identifier of block type
            if (names.ContainsKey(config.Name))
            {
                Debug.LogErrorFormat("Two blocks with the name {0} are defined", config.Name);
                return false;
            }

            // Unique identifier of block type
            if (types.ContainsKey(config.TypeInConfig))
            {
                Debug.LogErrorFormat("Two blocks with type {0} are defined", config.TypeInConfig);
                return false;
            }

            // Class name must be valid
            if (config.BlockClass == null)
            {
                Debug.LogErrorFormat("Invalid class name {0} for block {1}", config.ClassName, config.Name);
                return false;
            }

            // Use the type defined in the config if there is one, otherwise add one to the largest index so far
            if (config.type == ushort.MaxValue)
            {
                Debug.LogError("Maximum number of block types reached for " + config.Name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
        /// </summary>
        /// <param name="configs">A list of configs</param>
        /// <param name="types"></param>
        /// <param name="config">The controller object for this block</param>
        /// <returns>The index of the block</returns>
        private void AddBlockType(List<BlockConfig> configs, Dictionary<ushort, ushort> types, BlockConfig config)
        {
            config.type = (ushort)configs.Count;
            configs.Add(config);
            names.Add(config.Name, config.type);
            types.Add(config.TypeInConfig, config.type);
        }

        public ushort GetType(string name)
        {
            if (names.TryGetValue(name, out ushort type))
            {
                return type;
            }

            Debug.LogError("Block not found: " + name);
            return AIR_TYPE;
        }

        public ushort GetTypeFromTypeInConfig(ushort typeInConfig)
        {
            if (typeInConfig < types.Length)
            {
                return types[typeInConfig];
            }

            Debug.LogError("TypeInConfig not found: " + typeInConfig);
            return AIR_TYPE;
        }

        public Block GetBlock(string name)
        {
            if (names.TryGetValue(name, out ushort type))
            {
                return BlockTypes[type];
            }

            Debug.LogError("Block not found: " + name);
            return BlockTypes[AIR_TYPE];
        }

        public BlockConfig GetConfig(ushort type)
        {
            if (type < configs.Length)
            {
                return configs[type];
            }

            Debug.LogError("Config not found: " + type);
            return configs[AIR_TYPE];
        }
    }
}
