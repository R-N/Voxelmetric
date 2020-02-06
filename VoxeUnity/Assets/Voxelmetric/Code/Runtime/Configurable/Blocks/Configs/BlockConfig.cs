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
    public ushort TypeInConfig { get; set; }
    //! Unique identifier of block config
    public string Name { get; set; }

    private string className;
    public string ClassName
    {
        get { return className; }
        set
        {
            className = value;
            BlockClass = Type.GetType(value + ", " + typeof(Block).Assembly, false);
        }
    }

    public Type BlockClass { get; set; }

    public bool Solid { get; set; }
    public bool Transparent { get; set; }
    public bool RaycastHit { get; set; }
    public bool RaycastHitOnRemoval { get; set; }
    public int RenderMaterialID { get; set; }
    public int PhysicMaterialID { get; set; }

    #endregion

    public static BlockConfig CreateAirBlockConfig()
    {
        return new BlockConfig
        {
            Name = "air",
            TypeInConfig = BlockProvider.AIR_TYPE,
            ClassName = "Block",
            Solid = false,
            Transparent = true,
            PhysicMaterialID = -1
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
        Name = config.BlockName;
        if (string.IsNullOrWhiteSpace(Name))
        {
            Debug.LogError(config.name + " can't have a empty block name!");
            return false;
        }

        TypeInConfig = (ushort)(config.ID + BlockProvider.lastReservedType);
        Solid = config.Solid;
        Transparent = config.Transparent;

        // Try to associate requested render materials with one of world's materials
        RenderMaterialID = (int)config.TextureType;
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
        PhysicMaterialID = Solid ? 0 : -1; // solid objects will collide by default
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
        return Name;
    }
}
