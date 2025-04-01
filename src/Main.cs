using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using static CounterStrikeSharp.API.Core.Listeners;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Prop Hunt";
    public override string ModuleVersion => "0.2.1";
    public override string ModuleAuthor => "Siomek101, continued by exkludera";

    public static Plugin Instance { get; set; } = new();

    public List<string> models = new List<string>();
    public Dictionary<CCSPlayerController, SpecialProp> props = new();
    public HashSet<CCSPlayerController> HiddenPlayers = new();

    int spawnTerroristOffset = 0;
    DateTime hideTime = DateTime.Now;

    bool teleportedPlayers = false;
    public static bool PropHuntEnabled = false;

    public override void Load(bool hotReload)
    {
        Instance = this;

        Server.ExecuteCommand("mp_restartgame 3");

        RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        RegisterListener<OnMapStart>(OnMapStart);
        RegisterListener<OnTick>(OnTick);
        RegisterListener<OnEntitySpawned>(OnEntitySpawned);

        RegisterEventHandler<EventRoundStart>(EventRoundStart, HookMode.Post);
        RegisterEventHandler<EventPlayerHurt>(EventPlayerHurt);

        RegisterCommands();

        Transmit.Load();
    }
    public override void Unload(bool hotReload)
    {
        RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        RemoveListener<OnMapStart>(OnMapStart);
        RemoveListener<OnTick>(OnTick);
        RemoveListener<OnEntitySpawned>(OnEntitySpawned);

        DeregisterEventHandler<EventRoundStart>(EventRoundStart, HookMode.Post);
        DeregisterEventHandler<EventPlayerHurt>(EventPlayerHurt);

        RemoveCommands();

        Transmit.Unload();
    }

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config)
    {
        Config = config;
        Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);

        PropHuntEnabled = Config.Enabled;
    }

    public void OnServerPrecacheResources(ResourceManifest manifest)
    {
        manifest.AddResource(Config.SoundEvents);
    }

    public void OnMapStart(string map)
    {
        if (PropHuntEnabled)
        {
            spawnTerroristOffset = 0;
            Server.ExecuteCommand("mp_give_player_c4 0");
            models.Clear();
            HiddenPlayers.Clear();
        }
    }

    public void OnTick()
    {
        if (!PropHuntEnabled)
            return;

        var players = Utilities.GetPlayers().Where(x => x.PawnIsAlive).ToList();
        bool hiding = hideTime.CompareTo(DateTime.Now) > 0;

        if (hiding)
        {
            string timeLeft = hideTime.Subtract(DateTime.Now).ToString("mm\\:ss");
            foreach (var player in players)
                player.PrintToCenter($"Hiding time: {timeLeft}");
        }

        if (!teleportedPlayers && !hiding)
        {
            int offs = 0;
            foreach (var player in players.Where(p => p.Team == CsTeam.Terrorist))
            {
                var pawn = player.PlayerPawn?.Value;
                if (pawn == null) continue;

                Vector pos = Server.MapName switch
                {
                    "de_mirage" => new Vector(1316, -421 + offs, -103),
                    "de_inferno" => new Vector(670 - offs, 494, 136),
                    "cs_office" => new Vector(814 - offs, -495, -110),
                    _ => pawn.AbsOrigin!
                };
                pawn.Teleport(pos, new QAngle(0, -180, 0));
                offs += 30;

                player.RemoveWeapons();
                player.GiveNamedItem(CsItem.P90);
                player.GiveNamedItem(CsItem.Knife);
                player.GiveNamedItem(CsItem.USP);
            }
            teleportedPlayers = true;
        }

        foreach (var player in players)
        {
            if (props.TryGetValue(player, out var prop))
            {
                if (player.Team == CsTeam.Spectator || !player.PawnIsAlive)
                {
                    if (prop.entity != null && prop.entity.IsValid)
                        prop.entity.Remove();

                    props.Remove(player);
                    continue;
                }

                var buttons = player.Buttons;
                if ((buttons & PlayerButtons.Use) != 0 && !prop.AtkOnce)
                {
                    Command_PropFreezer(player, null);
                    prop.AtkOnce = true;
                }
                else if ((buttons & PlayerButtons.Use) == 0)
                    prop.AtkOnce = false;

                if ((buttons & PlayerButtons.Attack2) != 0 && !prop.Atk2Once)
                {
                    PropSpawner(player, true);
                    prop.Atk2Once = true;
                }
                else if ((buttons & PlayerButtons.Attack2) == 0)
                    prop.Atk2Once = false;

                if ((buttons & PlayerButtons.Reload) != 0 && !prop.RelOnce)
                {
                    Command_Decoy(player, null);
                    prop.RelOnce = true;
                }
                else if ((buttons & PlayerButtons.Reload) == 0)
                    prop.RelOnce = false;

                var pawn = player.PlayerPawn?.Value;
                if (pawn == null) continue;

                var entity = prop.entity;
                if (entity == null || !entity.IsValid) continue;

                if (!prop.Frozen)
                {
                    entity.Teleport(pawn.AbsOrigin, new QAngle(0, pawn.AbsRotation!.Y, 0));
                    player.GravityScale = 1;
                }
                else
                {
                    if (Utils.CalculateDistance(entity.AbsOrigin!,pawn.AbsOrigin!) > 5 || true)
                        pawn.Teleport(entity.AbsOrigin, new QAngle(IntPtr.Zero), new Vector(0, 0, 0));

                    player.GravityScale = 0;
                }


                if (!hiding)
                    player.PrintToCenter($"Swaps left: {prop.Swaps}, You are {(prop.Frozen ? "" : "not ")}frozen.");
            }
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
            hideTime = DateTime.Now.AddSeconds(Config.HideTime);
            teleportedPlayers = false;
            props.Clear();
            HiddenPlayers.Clear();

            Server.NextFrame(() =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    player.RemoveWeapons();

                    if (player.Team == CsTeam.CounterTerrorist)
                    {
                        PropSpawner(player);
                        HiddenPlayers.Add(player);
                    }
                }
            });
        }

        return HookResult.Continue;
    }

    HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (PropHuntEnabled)
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (player == null || attacker == null) return HookResult.Continue;

            if (attacker.DesignerName != "cs_player_controller" || attacker == player)
                return HookResult.Continue;

            if (props.TryGetValue(player, out var prop))
            {
                prop.entity.Remove();
                HiddenPlayers.Remove(player);
                props.Remove(player);
            }
        }

        return HookResult.Continue;
    }
}