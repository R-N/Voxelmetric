using UnityEngine;

public abstract class LayerConfigObject : ScriptableObject
{
    [SerializeField]
    private string layerName = "New Layer";
    [SerializeField]
    private int index = 0;
    [SerializeField]
    private string blockName = "block";

    public string LayerName { get { return layerName; } }
    public string BlockName { get { return blockName; } }

    public int Index { get { return index; } }

    public abstract TerrainLayer GetLayer();

    public virtual bool IsStructure()
    {
        return false;
    }
}
