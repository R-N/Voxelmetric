using System;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Blocks;

/// <summary>
/// BlockConfigs define constants for block types. Things like if the block is solid,
/// the block's texture etc. We could have used static variables in the block class
/// but the same block class can be used by different blocks - for example block cube
/// can be used by any cube block with a different texture for each block by defining the
/// texture for each of them in the block's json config. Then a BlockConfig will be
/// created for each block type and stored in BlockIndex referenced by the block type.
/// </summary>
public class BlockConfig
{
    //! Block type. Set externally by BlockIndex class when config is loaded
    public ushort type = 1;

    #region Parameters read from config

    //! Unique identifier of block config
    public ushort typeInConfig { get; set; }
    //! Unique identifier of block config
    public string name { get; set; }

    private string m_className;
    public string className
    {
        get { return m_className; }
        set
        {
            m_className = value;
            blockClass = Type.GetType(value + ", " + typeof(Block).Assembly, false);
        }
    }

    public Type blockClass { get; set; }

    public bool solid { get; set; }
    public bool transparent { get; set; }
    public bool raycastHit { get; set; }
    public bool raycastHitOnRemoval { get; set; }
    public int renderMaterialID { get; set; }
    public int physicMaterialID { get; set; }

    #endregion

    public static BlockConfig CreateAirBlockConfig(World world)
    {
        return new BlockConfig
        {
            name = "air",
            typeInConfig = BlockProvider.AirType,
            className = "Block",
            solid = false,
            transparent = true,
            physicMaterialID = -1
        };
    }

    public static BlockConfig CreateColorBlockConfig(World world, ushort type)
    {
        return new BlockConfig
        {
            name = string.Format("simple_{0}", type),
            typeInConfig = type,
            className = "SimpleBlock",
            solid = true,
            transparent = false,
            physicMaterialID = 0
        };
    }

    /// <summary>
    /// Assigns the variables in the config from a hashtable. When overriding this
    /// remember to call the base function first.
    /// </summary>
    /// <param name="config">Hashtable of the json config for the block</param>
    /// <param name="world">The world this block type belongs to</param>
    public virtual bool OnSetUp(BlockConfigObject config, World world)
    {
        // Obligatory parameters
        name = config.BlockName;
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError(config.name + " can't have a empty block name!");
            return false;
        }

        typeInConfig = (ushort)(config.ID + BlockProvider.LastReservedType);
        solid = config.Solid;
        transparent = config.Transparent;

        // Try to associate requested render materials with one of world's materials
        renderMaterialID = (int)config.TextureType;
        //string materialName = _GetPropertyFromConfig(config, "material", string.Empty);
        //for (int i = 0; i < world.renderMaterials.Length; i++)
        //{
        //    if (world.renderMaterials[i].name.Equals(materialName))
        //    {
        //        renderMaterialID = i;
        //        break;
        //    }
        //}

        // Try to associate requested physic materials with one of world's materials
        physicMaterialID = solid ? 0 : -1; // solid objects will collide by default
                                           //string materialName = _GetPropertyFromConfig(config, "materialPx", string.Empty);
                                           //for (int i = 0; i < world.physicsMaterials.Length; i++)
                                           //{
                                           //    if (world.physicsMaterials[i].name.Equals(materialName))
                                           //    {
                                           //        physicMaterialID = i;
                                           //        break;
                                           //    }
                                           //}

        return true;
    }

    public virtual bool OnPostSetUp(World world)
    {
        return true;
    }

    public override string ToString()
    {
        return name;
    }
}
