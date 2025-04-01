using CounterStrikeSharp.API.Core;

public class Config : BasePluginConfig
{
    public string Prefix { get; set; } = "{blue}[PropHunt]{default}";
    public bool Enabled { get; set; } = true;
    public int HideTime { get; set; } = 60;
    public int Decoys { get; set; } = 3;
    public int Swaps { get; set; } = 2;
    public string SoundEvents { get; set; } = "soundevents/ambience/game_sounds_inferno.vsndevts";
    public string TauntSoundEvent { get; set; } = "inferno.bell_g";
    public Commands Commands { get; set; } = new Commands();
}

public class Commands
{
    public List<string> TogglePropHunt { get; set; } = ["prophunt"];
    public List<string> Taunt { get; set; } = ["taunt", "whistle"];
    public List<string> Decoy { get; set; } = ["decoy"];
    public List<string> PropSwap { get; set; } = ["swap", "swapprop"];
    public List<string> PropFreeze { get; set; } = ["freeze"];
}