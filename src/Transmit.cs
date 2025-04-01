using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using System.Runtime.InteropServices;

public static class Transmit
{
    private static Plugin Instance = Plugin.Instance;

    private static readonly MemoryFunctionVoid<CCSPlayerPawn, CSPlayerState> StateTransition = new(GameData.GetSignature("StateTransition"));
    private static readonly INetworkServerService networkServerService = new();
    private static readonly CSPlayerState[] _oldPlayerState = new CSPlayerState[65];

    public static void Load()
    {
        Instance.RegisterListener<Listeners.CheckTransmit>(CheckTransmit);

        StateTransition.Hook(Hook_StateTransition, HookMode.Post);

        Instance.HookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
    }

    public static void Unload()
    {
        Instance.RemoveListener<Listeners.CheckTransmit>(CheckTransmit);

        StateTransition.Unhook(Hook_StateTransition, HookMode.Post);

        Instance.UnhookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
    }

    private static void CheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player == null || player.IsBot || !player.PawnIsAlive)
                continue;

            foreach (var hidden in Instance.HiddenPlayers)
            {
                if (hidden == null || hidden == player) continue;

                if (!Instance.HiddenPlayers.Contains(player) &&
                    player.Pawn.Value?.As<CCSPlayerPawnBase>().PlayerState != CSPlayerState.STATE_OBSERVER_MODE)
                {
                    var remove = hidden.Pawn.Value;
                    if (remove != null)
                        info.TransmitEntities.Remove(remove);
                }
            }
        }
    }

    private static HookResult CMsgSosStartSoundEvent(UserMessage um)
    {
        int entIndex = um.ReadInt("source_entity_index");
        var entHandle = NativeAPI.GetEntityFromIndex(entIndex);

        var pawn = new CBasePlayerPawn(entHandle);
        if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player") return HookResult.Continue;

        var player = pawn.Controller?.Value?.As<CCSPlayerController>();
        if (player == null || !player.IsValid) return HookResult.Continue;

        if (Instance.HiddenPlayers.Contains(player))
        {
            foreach (var target in Utilities.GetPlayers())
            {
                if (!target.IsValid) continue;
                if (Instance.HiddenPlayers.Contains(target)) continue;

                um.Recipients.Remove(target);
            }
        }

        return HookResult.Continue;
    }

    private static HookResult Hook_StateTransition(DynamicHook hook)
    {
        var player = hook.GetParam<CCSPlayerPawn>(0).OriginalController.Value;
        var state = hook.GetParam<CSPlayerState>(1);

        if (player == null)
            return HookResult.Continue;

        if (state != _oldPlayerState[player.Index])
        {
            if (state == CSPlayerState.STATE_OBSERVER_MODE || _oldPlayerState[player.Index] == CSPlayerState.STATE_OBSERVER_MODE)
                ForceFullUpdate(player);
        }

        _oldPlayerState[player.Index] = state;

        return HookResult.Continue;
    }
    private static void ForceFullUpdate(CCSPlayerController? player)
    {
        if (player is null || !player.IsValid)
            return;

        var networkGameServer = networkServerService.GetIGameServer();
        networkGameServer.GetClientBySlot(player.Slot)?.ForceFullUpdate();

        player.PlayerPawn.Value?.Teleport(null, player.PlayerPawn.Value.EyeAngles, null);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CUtlMemory
    {
        public unsafe nint* m_pMemory;
        public int m_nAllocationCount;
        public int m_nGrowSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CUtlVector
    {
        public unsafe nint this[int index]
        {
            get => this.m_Memory.m_pMemory[index];
            set => this.m_Memory.m_pMemory[index] = value;
        }

        public int m_iSize;
        public CUtlMemory m_Memory;

        public nint Element(int index) => this[index];
    }

    class INetworkServerService : NativeObject
    {
        private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerFunc;

        public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
        {
            this.GetIGameServerFunc = new VirtualFunctionWithReturn<nint, nint>(this.Handle, GameData.GetOffset("INetworkServerService_GetIGameServer"));
        }

        public INetworkGameServer GetIGameServer()
        {
            return new INetworkGameServer(this.GetIGameServerFunc.Invoke(this.Handle));
        }
    }

    public class INetworkGameServer : NativeObject
    {
        private static int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");

        private CUtlVector Slots;

        public INetworkGameServer(nint ptr) : base(ptr)
        {
            this.Slots = Marshal.PtrToStructure<CUtlVector>(base.Handle + SlotsOffset);
        }

        public CServerSideClient? GetClientBySlot(int playerSlot)
        {
            if (playerSlot >= 0 && playerSlot < this.Slots.m_iSize)
                return this.Slots[playerSlot] == IntPtr.Zero ? null : new CServerSideClient(this.Slots[playerSlot]);

            return null;
        }
    }

    public class CServerSideClient : NativeObject
    {
        private static int m_nForceWaitForTick = GameData.GetOffset("CServerSideClient_m_nForceWaitForTick");

        public unsafe int ForceWaitForTick
        {
            get { return *(int*)(base.Handle + m_nForceWaitForTick); }
            set { *(int*)(base.Handle + m_nForceWaitForTick) = value; }
        }

        public CServerSideClient(nint ptr) : base(ptr)
        { }

        public void ForceFullUpdate()
        {
            this.ForceWaitForTick = -1;
        }
    }
}