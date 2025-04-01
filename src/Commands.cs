using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

public partial class Plugin : BasePlugin
{
    public void RegisterCommands()
    {
        foreach (var cmd in Config.Commands.TogglePropHunt)
            AddCommand($"css_{cmd}", "Toggle Prophunt", Command_ToggleProphunt);

        foreach (var cmd in Config.Commands.Taunt)
            AddCommand($"css_{cmd}", "Taunt Command", Command_Taunt);

        foreach (var cmd in Config.Commands.Decoy)
            AddCommand($"css_{cmd}", "Spawn a fake prop at your legs", Command_Decoy);

        foreach (var cmd in Config.Commands.PropSwap)
            AddCommand($"css_{cmd}", "Swap prop for another prop (infinite times when hiding time, after that only 2 times)", Command_SwapProp);

        foreach (var cmd in Config.Commands.PropFreeze)
            AddCommand($"css_{cmd}", "Freeze prop", Command_PropFreezer);
    }

    public void RemoveCommands()
    {
        foreach (var cmd in Config.Commands.TogglePropHunt)
            RemoveCommand($"css_{cmd}", Command_ToggleProphunt);

        foreach (var cmd in Config.Commands.Taunt)
            RemoveCommand($"css_{cmd}", Command_Taunt);

        foreach (var cmd in Config.Commands.Decoy)
            RemoveCommand($"css_{cmd}", Command_Decoy);

        foreach (var cmd in Config.Commands.PropSwap)
            RemoveCommand($"css_{cmd}", Command_SwapProp);

        foreach (var cmd in Config.Commands.PropFreeze)
            RemoveCommand($"css_{cmd}", Command_PropFreezer);
    }

    public void Command_ToggleProphunt(CCSPlayerController? player, CommandInfo cmdInfo)
    {
        PropHuntEnabled = !PropHuntEnabled;

        string status = PropHuntEnabled ? "ON" : "OFF";
        char color = PropHuntEnabled ? ChatColors.Green : ChatColors.Red;

        Utils.PrintToChatAll($"Status: {color}{status}");
    }

    public void Command_Taunt(CCSPlayerController? player, CommandInfo? command)
    {
        if (!PropHuntEnabled)
            return;

        if (player == null)
            return;

        player.EmitSound(Config.TauntSoundEvent);
    }

    public void Command_Decoy(CCSPlayerController? player, CommandInfo? command)
    {
        if (player == null || !PropHuntEnabled) return;

        if (props.TryGetValue(player, out var prop))
        {
            if (prop.DecoysLeft > 0)
            {
                prop.DecoysLeft--;

                var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                if (entity == null)
                {
                    Console.WriteLine("Entity creation failed!");
                    return;
                }

                entity.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

                entity.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
                entity.SetModel(prop.entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                entity.Teleport(prop.entity.AbsOrigin, prop.entity.AbsRotation, prop.entity.AbsVelocity);
                entity.DispatchSpawn();

                Utils.PrintToChat(player!, "Decoys Left: " + prop.DecoysLeft);

            }
            else Utils.PrintToChat(player!, "No decoys!");
        }
    }

    public void Command_SwapProp(CCSPlayerController? player, CommandInfo command)
    {
        if (!PropHuntEnabled)
            return;

        PropSpawner(player, true);
    }
    public void PropSpawner(CCSPlayerController? player, bool swap = false)
    {
        if (player == null)
            return;

        if (models.Count == 0)
        {
            Logger.LogWarning("Models list is empty");
            return;
        }

        var modelId = Random.Shared.Next(0, models.Count - 1);

        if (swap && props.TryGetValue(player, out var prop))
        {
            var canSwap = hideTime.CompareTo(DateTime.Now) > 0;

            if (!canSwap)
            {
                if (prop.Swaps > 0)
                {
                    canSwap = true;
                    prop.Swaps--;
                }

            }
            if (canSwap)
            {
                if (models.Count > 1)
                    while (prop.modelID == modelId) modelId = Random.Shared.Next(0, models.Count - 1);

                prop.entity.SetModel(models[modelId]);
                prop.modelID = modelId;
            }
            return;
        }
        else
        {
            var entity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (entity == null)
            {
                Console.WriteLine("Entity creation failed!");
                return;
            }

            entity.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;

            entity.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            entity.SetModel(models[modelId]);
            entity.Teleport(player.Pawn.Value!.AbsOrigin);
            entity.DispatchSpawn();
            entity.AcceptInput("FollowEntity", player, entity, "!activator");
            props.Add(player, new SpecialProp(entity, modelId));
        }
    }

    public void Command_PropFreezer(CCSPlayerController? player, CommandInfo? command)
    {
        if (!PropHuntEnabled)
            return;

        if (player == null)
            return;

        if (props.TryGetValue(player, out var prop))
            prop.Frozen = !prop.Frozen;
    }
}