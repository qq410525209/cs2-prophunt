using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public class SpecialProp
{
    Plugin plugin;

    public CDynamicProp prop;

    public ulong playerId;
    public bool weirdStuff = false;

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

    public SpecialProp(Plugin plugin, CDynamicProp prop, ulong userId, int modelId)
    {
        this.prop = prop;
        playerId = userId;
        this.plugin = plugin;
        modelID = modelId;
        Swaps = Plugin.Instance.Config.Swaps;
        Frozen = false;
        DecoysLeft = Plugin.Instance.Config.Decoys;
    }

    public void Teleport(Vector position, QAngle angle, Vector velocity)
    {
        if (!prop.IsValid)
        {
            plugin.props.Remove(this);
            return;
        }

        lastPlayerPos = position;
        prop.Teleport(position, angle, velocity);
    }

    public void Teleport(Vector position)
    {
        Teleport(position, prop.AbsRotation!, new Vector(0, 0, 0));
    }

    public void Teleport(Vector position, QAngle angle)
    {
        Teleport(position, angle, new Vector(0, 0, 0));
    }
}