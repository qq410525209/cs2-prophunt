using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

public partial class Plugin : BasePlugin
{
    public void RegisterCommands()
    {
        foreach (var cmd in Config.Commands.TogglePropHunt.Split(','))
            AddCommand($"css_{cmd}", "Toggle Prophunt", Command_ToggleProphunt);

        foreach (var cmd in Config.Commands.Whistle.Split(','))
            AddCommand($"css_{cmd}", "Whistle Command", Command_Whistle);

        foreach (var cmd in Config.Commands.Decoy.Split(','))
            AddCommand($"css_{cmd}", "Spawn a fake prop at your legs", Command_Decoy);

        foreach (var cmd in Config.Commands.PropSwap.Split(','))
            AddCommand($"css_{cmd}", "Swap prop for another prop (infinite times when hiding time, after that only 2 times)", Command_SwapProp);

        foreach (var cmd in Config.Commands.PropFreeze.Split(','))
            AddCommand($"css_{cmd}", "Freeze prop", Command_PropFreezer);
    }

    public void RemoveCommands()
    {
        foreach (var cmd in Config.Commands.TogglePropHunt.Split(','))
            RemoveCommand($"css_{cmd}", Command_ToggleProphunt);

        foreach (var cmd in Config.Commands.Whistle.Split(','))
            RemoveCommand($"css_{cmd}", Command_Whistle);

        foreach (var cmd in Config.Commands.Decoy.Split(','))
            RemoveCommand($"css_{cmd}", Command_Decoy);

        foreach (var cmd in Config.Commands.PropSwap.Split(','))
            RemoveCommand($"css_{cmd}", Command_SwapProp);

        foreach (var cmd in Config.Commands.PropFreeze.Split(','))
            RemoveCommand($"css_{cmd}", Command_PropFreezer);
    }

    public void Command_ToggleProphunt(CCSPlayerController? player, CommandInfo cmdInfo)
    {
        PropHuntEnabled = !PropHuntEnabled;

        string status = PropHuntEnabled ? "ON" : "OFF";
        char color = PropHuntEnabled ? ChatColors.Green : ChatColors.Red;

        Utils.PrintToChatAll($"Status: {color}{status}");
    }

    public void Command_Whistle(CCSPlayerController? player, CommandInfo? command)
    {
        if (!PropHuntEnabled)
            return;

        if (player == null)
            return;

        CSoundEventEntity prop = Utilities.CreateEntityByName<CSoundEventEntity>("point_soundevent")!;

        prop.DispatchSpawn();
        prop.SoundName = "sounds/ambient/animal/bird15.vsnd";
        prop.Teleport(player.Pawn.Value!.AbsOrigin, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        prop.StartOnSpawn = true;
    }

    public void Command_Decoy(CCSPlayerController? player, CommandInfo? command)
    {
        if (!PropHuntEnabled) return;
        SpecialProp? foundProp = null;
        foreach (var item in props)
        {
            if (item.playerId == player!.SteamID)
                foundProp = item;

            break;
        }

        if (foundProp == null)
            return;

        if (foundProp.DecoysLeft > 0)
        {
            foundProp.DecoysLeft--;

            CDynamicProp prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic")!;

            prop.DispatchSpawn();
            prop.Teleport(foundProp.prop.AbsOrigin, foundProp.prop.AbsRotation, foundProp.prop.AbsVelocity);
            prop.Globalname = "test_prop";
            prop.SetModel(models[foundProp.modelID]);
            prop.Collision.CollisionGroup = 2;
            Utils.PrintToChat(player!, "Decoys Left: " + foundProp.DecoysLeft);

        }
        else Utils.PrintToChat(player!, "No decoys!");
    }

    public void Command_SwapProp(CCSPlayerController? player, CommandInfo command)
    {
        if (!PropHuntEnabled)
            return;

        PropSpawner(player);
    }
    public void PropSpawner(CCSPlayerController? player, bool allowCreate = false)
    {
        if (player == null)
            return;

        var modelId = Random.Shared.Next(0, models.Count - 1);

        foreach (var item in props)
        {
            if (item.playerId == player.SteamID)
            {
                if (!player.Pawn.IsValid)
                    continue;

                var canSwap = hideTime.CompareTo(DateTime.Now) > 0;

                if (!canSwap)
                {
                    if (item.Swaps > 0)
                    {
                        canSwap = true;
                        item.Swaps--;
                    }

                }
                if (canSwap)
                {
                    if (models.Count > 1)
                        while (item.modelID == modelId) modelId = Random.Shared.Next(0, models.Count - 1);

                    item.prop.SetModel(models[modelId]);
                    item.modelID = modelId;
                }

                return;
            }
        }
        if (allowCreate || player.Team == CsTeam.CounterTerrorist)
        {
            // Spawn
            CDynamicProp prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic")!;

            prop.DispatchSpawn();
            prop.Teleport(player.Pawn.Value!.AbsOrigin, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            prop.Globalname = "test_prop";
            prop.SetModel(models[modelId]);

            /*if (Server.MapName == "de_mirage")
            {
                prop.SetModel("models/props_junk/plasticcrate01a.vmdl");
            }
            if (Server.MapName == "de_inferno")
            {
                prop.SetModel("models/generic/planter_kit_01/pk01_planter_09_cressplant_breakable_b.vmdl");
            }*/

            prop.Collision.CollisionGroup = 2; // best is 2

            props.Add(new SpecialProp(this, prop, player.SteamID, modelId));

            player.RemoveItemByDesignerName("weapon_c4");
            player.RemoveWeapons();
            player.GiveNamedItem(CounterStrikeSharp.API.Modules.Entities.Constants.CsItem.Knife);
            player.Pawn.Value.Health = 1;

            SetBodygroup(player.PlayerPawn.Value!.Handle, "default_gloves", 0);
        }
    }

    public void Command_PropFreezer(CCSPlayerController? player, CommandInfo? command)
    {
        if (!PropHuntEnabled)
            return;

        if (player == null)
            return;

        foreach (var item in props)
        {
            if (item.playerId == player.SteamID)
            {
                item.Frozen = !item.Frozen;
                return;
            }
        }
    }
}