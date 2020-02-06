using UnityEngine;
using UnityEngine.Scripting;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class Block
{
    protected BlockConfig config;
    public string name;
    public ushort type;
    public int renderMaterialID;
    public int physicMaterialID;
    public bool solid;
    public bool transparent;
    public bool custom;

    public bool CanCollide { get { return physicMaterialID >= 0; } }

    [Preserve]
    public Block()
    {
        type = 0;
        config = null;
    }

    public void Init(ushort type, BlockConfig config)
    {
        this.type = type;
        this.config = config;

        renderMaterialID = config.RenderMaterialID;
        physicMaterialID = config.PhysicMaterialID;

        name = config.Name;
        solid = config.Solid;
        transparent = config.Transparent;
        custom = false;
    }

    public virtual string DisplayName
    {
        get { return name; }
    }

    public virtual void OnInit(BlockProvider blockProvider) { }

    public virtual void BuildBlock(Chunk chunk, ref Vector3Int localpos, int materialID) { }

    public bool CanBuildFaceWith(Block adjacentBlock)
    {
        if (adjacentBlock.transparent)
        {
            return !transparent || adjacentBlock.type != type;
        }

        return adjacentBlock.solid ? !solid : (solid || type != adjacentBlock.type);
    }

    public virtual void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated) { }

    public virtual void OnCreate(Chunk chunk, ref Vector3Int localPos) { }

    public virtual void OnDestroy(Chunk chunk, ref Vector3Int localPos) { }

    public virtual void RandomUpdate(Chunk chunk, ref Vector3Int localPos) { }

    public virtual void ScheduledUpdate(Chunk chunk, ref Vector3Int localPos) { }

    public bool RaycastHit(ref Vector3 pos, ref Vector3 dir, ref Vector3Int bPos, bool removalRequested)
    {
        return removalRequested ? config.RaycastHitOnRemoval : config.RaycastHit;
    }

    public override string ToString()
    {
        return name;
    }
}
