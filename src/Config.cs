using CounterStrikeSharp.API.Core;

public class Config : BasePluginConfig
{
    public string Prefix { get; set; } = "{blue}[PropHunt]{default}";
    public bool Enabled { get; set; } = true;
    public int HideTimeMinutes { get; set; } = 1;
    public int Decoys { get; set; } = 3;
    public int Swaps { get; set; } = 2;
    public Commands Commands { get; set; } = new Commands();
}

public class Commands
{
    public string TogglePropHunt { get; set; } = "prophunt";
    public string Whistle { get; set; } = "whistle";
    public string Decoy { get; set; } = "decoy";
    public string PropSwap { get; set; } = "swap,swapprop";
    public string PropFreeze { get; set; } = "freeze";
}