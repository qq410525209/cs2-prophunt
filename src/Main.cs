using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Runtime.InteropServices;
using static CounterStrikeSharp.API.Core.Listeners;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Prop Hunt";
    public override string ModuleVersion => "0.2.0";
    public override string ModuleAuthor => "Siomek101, continued by exkludera";

    public static Plugin Instance { get; set; } = new();

    public List<string> models = new List<string>();
    public List<SpecialProp> props = new List<SpecialProp>();

    private static readonly MemoryFunctionWithReturn<nint, string, int, int> SetBodygroupFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "55 48 89 E5 41 56 49 89 F6 41 55 41 89 D5 41 54 49 89 FC 48 83 EC 08" : "48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 41 8B F8 48 8B F2 48 8B D9 E8 ? ? ? ?");
    private static readonly Func<nint, string, int, int> SetBodygroup = SetBodygroupFunc.Invoke;

    int spawnTerroristOffset = 0;
    DateTime hideTime = DateTime.Now;

    bool teleportedPlayers = false;
    public static bool PropHuntEnabled = false;

    public override void Load(bool hotReload)
    {
        Instance = this;

        models.Add("models/props/de_inferno/claypot03.vmdl");

        Server.ExecuteCommand("mp_restartgame 3");

        RegisterListener<OnMapStart>(OnMapStart);
        RegisterListener<OnTick>(OnTick);
        RegisterListener<OnEntitySpawned>(OnEntitySpawned);

        RegisterEventHandler<EventRoundStart>(EventRoundStart, HookMode.Post);
 
        HookEntityOutput("prop_dynamic", "OnTakeDamage", OnTakeDamage);

        RegisterCommands();
    }
    public override void Unload(bool hotReload)
    {
        RemoveListener<OnMapStart>(OnMapStart);
        RemoveListener<OnTick>(OnTick);
        RemoveListener<OnEntitySpawned>(OnEntitySpawned);

        DeregisterEventHandler<EventRoundStart>(EventRoundStart, HookMode.Post);

        UnhookEntityOutput("prop_dynamic", "OnTakeDamage", OnTakeDamage);

        RemoveCommands();
    }

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config)
    {
        Config = config;
        Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);

        PropHuntEnabled = Config.Enabled;
    }

    public void OnMapStart(string map)
    {
        if (PropHuntEnabled)
        {
            spawnTerroristOffset = 0;
            Server.ExecuteCommand("mp_give_player_c4 0");
            models.Clear();
        }
    }

    public void OnTick()
    {
        if (PropHuntEnabled)
        {
            int offs = 0;
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (hideTime.CompareTo(DateTime.Now) > 0)
                {
                    player.PrintToCenter("Hiding time: " + hideTime.Subtract(DateTime.Now).ToString("mm\\:ss"));

                    if (player.TeamNum == 2)
                        player.RemoveWeapons();
                }

                else if (!teleportedPlayers)
                {
                    if (player.Pawn.IsValid)
                    {
                        if (player.TeamNum == 2)
                        {
                            if (Server.MapName == "de_mirage")
                                player.PlayerPawn.Value!.Teleport(new Vector(1316, -421 + offs, -103), new QAngle(0, -180, 0), new Vector(0, 0, 0));

                            if (Server.MapName == "de_inferno")
                                player.PlayerPawn.Value!.Teleport(new Vector(670 - offs, 494, 136), new QAngle(0, 0, 0), new Vector(0, 0, 0));

                            if (Server.MapName == "cs_office")
                                player.PlayerPawn.Value!.Teleport(new Vector(814 - offs, -495, -110), new QAngle(0, 0, 0), new Vector(0, 0, 0));

                            offs += 30;

                            player.GiveNamedItem(CounterStrikeSharp.API.Modules.Entities.Constants.CsItem.P90);
                            player.GiveNamedItem(CounterStrikeSharp.API.Modules.Entities.Constants.CsItem.Knife);
                            player.GiveNamedItem(CounterStrikeSharp.API.Modules.Entities.Constants.CsItem.USP);
                        }

                        if (player.TeamNum == 3)
                            player.RemoveWeapons();
                    }
                }

                if (!player.PlayerPawn.IsValid)
                    continue;

                bool found = false;

                foreach (var item in props)
                {
                    if (item.playerId == player.SteamID)
                    {
                        if (player.Team == CsTeam.Spectator || !player.PawnIsAlive)
                        {
                            item.prop.Remove();
                            props.Remove(item);
                            break;
                        }

                        if (!player.PlayerPawn.IsValid)
                            continue;

                        var buttons = player.Buttons;
                        if ((buttons & PlayerButtons.Use) != 0)
                        {
                            if (!item.AtkOnce)
                            {
                                Command_PropFreezer(player, null);
                                item.AtkOnce = true;
                            }
                        }
                        else item.AtkOnce = false;

                        if ((buttons & PlayerButtons.Attack2) != 0)
                        {
                            if (!item.Atk2Once)
                            {
                                PropSpawner(player);
                                item.Atk2Once = true;
                            }
                        }
                        else item.Atk2Once = false;

                        if ((buttons & PlayerButtons.Reload) != 0)
                        {
                            if (!item.RelOnce)
                            {
                                Command_Decoy(player, null);
                                item.RelOnce = true;
                            }
                        }
                        else item.RelOnce = false;

                        var off = Utils.Offset(player.Pawn.Value!.AbsOrigin!, new Vector(0, 0, 0));

                        if (!item.Frozen)
                        {
                            //|| item.lastPlayerPos.X != off.X || item.lastPlayerPos.Z != off.Z || item.lastPlayerPos.Y != off.Y || item.weirdStuff) { 
                            item.Teleport(off, /*new QAngle(item.prop.AbsRotation.X, player.Pawn.Value.AbsRotation.Y, item.prop.AbsRotation.Z)*/ new QAngle(0, player.PlayerPawn.Value!.AbsRotation!.Y, 0));
                            //item.weirdStuff = !(item.lastPlayerPos2.X == item.lastPlayerPos.X && item.lastPlayerPos2.Z == item.lastPlayerPos.Z && item.lastPlayerPos2.Y == item.lastPlayerPos.Y);
                            item.weirdStuff = false;
                            player.GravityScale = 1;
                        }
                        else
                        {
                            if (Utils.CalculateDistance(item.prop.AbsOrigin!, player.Pawn.Value!.AbsOrigin!) > 5 || true)
                                player.Pawn.Value.Teleport(item.prop.AbsOrigin, new QAngle(IntPtr.Zero), new Vector(0, 0, 0));

                            player.GravityScale = 0;
                            //item.Teleport(off);
                        }

                        if (hideTime.CompareTo(DateTime.Now) <= 0) player.PrintToCenter("Swaps left: " + item.Swaps + ", You are " + (item.Frozen ? "" : "not ") + "frozen.");
                        found = true;

                        break;
                    }
                }

                if (found)
                {
                    if (!player.Pawn.IsValid)
                        continue;

                    player.Pawn.Value!.ShadowStrength = 0;
                    player.Pawn.Value.Collision.BoundingRadius = 5f;
                    player.Pawn.Value.Render = Color.FromArgb(0, 0, 0, 0);
                    SetBodygroup(player.PlayerPawn.Value!.Handle, "default_gloves", 0);
                }
                else
                {
                    if (!player.Pawn.IsValid)
                        continue;

                    player.Pawn.Value!.ShadowStrength = 1;
                    player.Pawn.Value.Render = Color.FromArgb(255, 0, 0, 0);
                    SetBodygroup(player.PlayerPawn.Value!.Handle, "default_gloves", 1);
                }
            }

            if (hideTime.CompareTo(DateTime.Now) <= 0 && !teleportedPlayers)
                teleportedPlayers = true;
        }
    }

    public void OnEntitySpawned(CEntityInstance entity)
    {
        if (PropHuntEnabled)
        {
            if (entity.DesignerName == "prop_physics_multiplayer")
            {
                var prop = new CPhysicsPropMultiplayer(entity.Handle);

                string model = prop.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;

                if (!models.Contains(model))
                {
                    models.Add(model);
                    Console.WriteLine(model);
                }
            }

            if (entity.DesignerName == "func_buyzone")
                entity.Remove();

            if (entity.DesignerName == "info_player_terrorist")
            {
                var spawn = new CInfoPlayerTerrorist(entity.Handle);

                if (Server.MapName == "de_mirage")
                {
                    if (spawnTerroristOffset > Utils.Spawns.Mirage.Length - 1) spawnTerroristOffset = 0;
                    spawn.Teleport(Utils.Spawns.Mirage[spawnTerroristOffset], new QAngle(0, 0, 0), new Vector(0, 0, 0));
                }

                if (Server.MapName == "de_inferno")
                {
                    if (spawnTerroristOffset > Utils.Spawns.Inferno.Length - 1) spawnTerroristOffset = 0;
                    spawn.Teleport(Utils.Spawns.Inferno[spawnTerroristOffset], new QAngle(0, 0, 0), new Vector(0, 0, 0));
                }

                if (Server.MapName == "cs_office")
                {
                    if (spawnTerroristOffset > Utils.Spawns.Office.Length - 1) spawnTerroristOffset = 0;
                    spawn.Teleport(Utils.Spawns.Office[spawnTerroristOffset], new QAngle(0, 0, 0), new Vector(0, 0, 0));
                }

                spawnTerroristOffset += 1;
            }
        }
    }

    HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (PropHuntEnabled)
        {
            hideTime = DateTime.Now.AddMinutes(Config.HideTimeMinutes);
            teleportedPlayers = false;
            props.Clear();

            if (Server.MapName == "de_mirage")
            {
                var offset = -20;

                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -240));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -210));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -180));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -150));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -120));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1235 + offset, 543, -90));

                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -240));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -210));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -180));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -150));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -120));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1265 + offset, 543, -90));

                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -240));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -210));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -180));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -150));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -120));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1295 + offset, 543, -90));

                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -240));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -210));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -180));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -150));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -120));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1325 + offset, 543, -90));

                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -240));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -210));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -180));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -150));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -120));
                Utils.CreateProp("models/dev/dev_cube.vmdl", new Vector(1355 + offset, 543, -90));
            }
            foreach (var player in Utilities.GetPlayers())
            {
                try
                {
                    if (player.Team == CsTeam.CounterTerrorist)
                        PropSpawner(player, true);
                }
                catch (Exception e)
                {
                    Logger.LogError(e.Message);
                    // Shut the f up
                }
            }
        }

        return HookResult.Continue;
    }

    HookResult OnTakeDamage(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (PropHuntEnabled)
            caller.Remove();

        return HookResult.Continue;
    }
}