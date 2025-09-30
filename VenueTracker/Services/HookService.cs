using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;
using FFXIVClientStructs.STD;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Components;

namespace VenueTracker.Services;

public unsafe class HookService : IHostedService, IMediatorSubscriber
{
    private readonly ILogger<HookService> _logger;
    private readonly GuestsListWidget _guestsListWidget;
    
    [Signature("E8 ?? ?? ?? ?? EB ?? 45 33 C9 4C 8B C6", DetourName = nameof(RandomPrintLogDetour))]
    private Hook<RandomPrintLogDelegate>? RandomPrintLogHook { get; set; }
    private delegate void RandomPrintLogDelegate(RaptureLogModule* module, int logMessageId, byte* playerName, byte sex, StdDeque<TextParameter>* parameter, byte flags, ushort homeWorldId);

    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 54 41 55 41 56 48 8D 6C 24 ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 0F B7 75", DetourName = nameof(DicePrintLogDetour))]
    private Hook<DicePrintLogDelegate>? DicePrintLogHook { get; set; }
    private delegate void DicePrintLogDelegate(RaptureLogModule* module, ushort chatType, byte* userName, void* unused, ushort worldId, ulong accountId, ulong contentId, ushort roll, ushort outOf, uint entityId, byte ident);


    public HookService(ILogger<HookService> logger, IGameInteropProvider gameInteropProvider, GuestsListWidget guestsListWidget)
    {
        _logger = logger;
        _guestsListWidget = guestsListWidget;
        gameInteropProvider.InitializeFromAttributes(this);

        RandomPrintLogHook?.Enable();
        DicePrintLogHook?.Enable();
    }
    
    public VSyncMediator Mediator { get; }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        RandomPrintLogHook?.Dispose();
        DicePrintLogHook?.Dispose();
    }
    
    private void RandomPrintLogDetour(RaptureLogModule* module, int logMessageId, byte* playerName, byte sex, StdDeque<TextParameter>* parameter, byte flags, ushort homeWorldId)
    {
        if (logMessageId != 856 && logMessageId != 3887)
        {
            RandomPrintLogHook!.Original(module, logMessageId, playerName, sex, parameter, flags, homeWorldId);
            return;
        }

        try
        {
            var name = MemoryHelper.ReadStringNullTerminated((nint)playerName);
            var roll = (*parameter)[1].IntValue;
            var outOf = logMessageId == 3887 ? (*parameter)[2].IntValue : 0;

            _guestsListWidget.ProcessIncomingRoll(name, homeWorldId, roll, outOf);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Unable to process random roll");
        }

        RandomPrintLogHook!.Original(module, logMessageId, playerName, sex, parameter, flags, homeWorldId);
    }

    private void DicePrintLogDetour(RaptureLogModule* module, ushort chatType, byte* playerName, void* unused, ushort worldId, ulong accountId, ulong contentId, ushort roll, ushort outOf, uint entityId, byte ident)
    {
        try
        {
            var name = MemoryHelper.ReadStringNullTerminated((nint)playerName);
            _guestsListWidget.ProcessIncomingRoll(name, worldId, roll, outOf);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Unable to process dice roll");
        }

        DicePrintLogHook!.Original(module, chatType, playerName, unused, worldId, accountId, contentId, roll, outOf, entityId, ident);
    }
}
