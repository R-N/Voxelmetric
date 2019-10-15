using UnityEngine;

public abstract class BlockConfigObject : ScriptableObject
{
    [SerializeField]
    private string blockName = "New Block";
    [SerializeField]
    private ushort id = 1;
    [SerializeField]
    private bool solid = true;
    [SerializeField]
    private bool transparent = false;

    public string BlockName { get { return blockName; } }
    public ushort ID { get { return id; } }
    public bool Solid { get { return solid; } }
    public bool Transparent { get { return transparent; } }

    public abstract BlockConfig GetConfig();

    public abstract object GetBlockClass();
}
