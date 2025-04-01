using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class SpecialProp
{
    public CDynamicProp entity;

    public Vector lastPlayerPos = new Vector(0, 0, 0);
    public Vector lastPlayerPos2 = new Vector(0, 0, 0);

    public int modelID = 0;
    public int Swaps = 2;
    public int DecoysLeft = 3;
    public bool Frozen = false;

    // Button Once
    public bool AtkOnce = false;
    public bool Atk2Once = false;
    public bool RelOnce = false;

    public SpecialProp(CDynamicProp prop, int modelId)
    {
        entity = prop;
        modelID = modelId;
        Swaps = Plugin.Instance.Config.Swaps;
        Frozen = false;
        DecoysLeft = Plugin.Instance.Config.Decoys;
    }
}