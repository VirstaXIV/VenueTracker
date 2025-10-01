using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services.Mediator;
using System.Security.Cryptography;
using System.Text;

namespace VenueTracker.Services;

public class UtilService : IHostedService, IMediatorSubscriber
{
    public struct PlayerCharacter
    {
        public uint ObjectId;
        public string Name;
        public uint HomeWorldId;
        public nint Address;
    };

    private struct PlayerInfo
    {
        public PlayerCharacter Character;
        public string Hash;
    };
    
#pragma warning disable SYSLIB0021
    private static readonly SHA256CryptoServiceProvider _sha256CryptoProvider = new();
#pragma warning restore SYSLIB0021
    private readonly ILogger<UtilService> _logger;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IChatGui _chatGui;
    private readonly ICondition _condition;
    private uint? _classJobId = 0;
    private DateTime _delayedFrameworkUpdateCheck = DateTime.UtcNow;
    private ushort _lastZone = 0;
    private bool _sentBetweenAreas = false;
    private static readonly Dictionary<uint, PlayerInfo> _playerInfoCache = new();
    
    public UtilService(ILogger<UtilService> logger, IClientState clientState, IFramework framework, IChatGui chatGui, ICondition condition)
    {
        _logger = logger;
        _clientState = clientState;
        _framework = framework;
        _chatGui = chatGui;
        _condition = condition;
    }
    
    public bool IsAnythingDrawing { get; private set; } = false;
    public bool IsInCutscene { get; private set; } = false;
    public bool IsInGpose { get; private set; } = false;
    public bool IsLoggedIn { get; private set; }
    public bool IsOnFrameworkThread => _framework.IsInFrameworkUpdateThread;
    public bool IsZoning => _condition[ConditionFlag.BetweenAreas] || _condition[ConditionFlag.BetweenAreas51];
    public bool IsInCombatOrPerforming { get; private set; } = false;
    
    
    public VSyncMediator Mediator { get; }
    
    public void EnsureIsOnFramework()
    {
        if (!_framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Can only be run on Framework");
    }
    
    public bool GetIsPlayerPresent()
    {
        EnsureIsOnFramework();
        return _clientState.LocalPlayer != null && _clientState.LocalPlayer.IsValid();
    }
    
    public async Task<bool> GetIsPlayerPresentAsync()
    {
        return await RunOnFrameworkThread(GetIsPlayerPresent).ConfigureAwait(false);
    }
    
    public async Task<IPlayerCharacter> GetPlayerCharacterAsync()
    {
        return await RunOnFrameworkThread(GetPlayerCharacter).ConfigureAwait(false);
    }
    
    public IPlayerCharacter GetPlayerCharacter()
    {
        EnsureIsOnFramework();
        return _clientState.LocalPlayer!;
    }
    
    private unsafe PlayerInfo GetPlayerInfo(IGameObject chara)
    {
        uint id = chara.EntityId;

        if (!_playerInfoCache.TryGetValue(id, out var info))
        {
            info.Character.ObjectId = id;
            info.Character.Name = chara.Name.TextValue; // ?
            info.Character.HomeWorldId = ((BattleChara*)chara.Address)->Character.HomeWorld;
            info.Character.Address = chara.Address;
            info.Hash = BitConverter.ToString(
                _sha256CryptoProvider.ComputeHash(Encoding.UTF8.GetBytes(info.Character.Name + info.Character.HomeWorldId.ToString()))
                ).Replace("-", "", StringComparison.Ordinal);
            _playerInfoCache[id] = info;
        }

        info.Character.Address = chara.Address;

        return info;
    }
    
    public void ChatPlayerLink(Player player, string? message = null)
    {

        var messageBuilder = new SeStringBuilder();
        messageBuilder.Add(new PlayerPayload(player.Name, player.HomeWorld));
        if (message != null)
        {
            messageBuilder.AddText(message);
        }

        var entry = new XivChatEntry() { Message = messageBuilder.Build() };
        _chatGui.Print(entry);
    }
    
    public async Task RunOnFrameworkThread(System.Action act, [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        var fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (!_framework.IsInFrameworkUpdateThread)
        {
            await _framework.RunOnFrameworkThread(act).ContinueWith((_) => Task.CompletedTask).ConfigureAwait(false);
            while (_framework.IsInFrameworkUpdateThread) // yield the thread again, should technically never be triggered
            {
                _logger.LogTrace("Still on framework");
                await Task.Delay(1).ConfigureAwait(false);
            }
        }
        else
        {
            act();
        }
    }
    
    public async Task<T> RunOnFrameworkThread<T>(Func<T> func, [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        var fileName = Path.GetFileNameWithoutExtension(callerFilePath);
        if (!_framework.IsInFrameworkUpdateThread)
        {
            var result = await _framework.RunOnFrameworkThread(func).ContinueWith((task) => task.Result).ConfigureAwait(false);
            while (_framework.IsInFrameworkUpdateThread) // yield the thread again, should technically never be triggered
            {
                _logger.LogTrace("Still on framework");
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        return func.Invoke();
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting UtilService");
        Plugin.Self.RealOnFrameworkUpdate = this.FrameworkOnUpdate;
        _framework.Update += Plugin.Self.OnFrameworkUpdate;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Stopping {type}", GetType());
        
        Mediator.UnsubscribeAll(this);
        _framework.Update -= Plugin.Self.OnFrameworkUpdate;
        return Task.CompletedTask;
    }
    
    private void FrameworkOnUpdate(IFramework framework)
    {
        FrameworkOnUpdateInternal();
    }

    private unsafe void FrameworkOnUpdateInternal()
    {
        if (_clientState.LocalPlayer?.IsDead ?? false)
        {
            return;
        }

        bool isNormalFrameworkUpdate = DateTime.UtcNow < _delayedFrameworkUpdateCheck.AddMilliseconds(200);

        IsAnythingDrawing = false;
        if (_sentBetweenAreas)
            return;

        if (_clientState.IsGPosing && !IsInGpose)
        {
            _logger.LogDebug("Gpose start");
            IsInGpose = true;
            // Leaving this check if its needed.
        }
        else if (!_clientState.IsGPosing && IsInGpose)
        {
            _logger.LogDebug("Gpose end");
            IsInGpose = false;
            // Leaving this check if its needed.
        }

        if ((_condition[ConditionFlag.Performing] || _condition[ConditionFlag.InCombat]) && !IsInCombatOrPerforming)
        {
            _logger.LogDebug("Combat/Performance start");
            IsInCombatOrPerforming = true;
            // Leaving this check if its needed.
        }
        else if ((!_condition[ConditionFlag.Performing] && !_condition[ConditionFlag.InCombat]) && IsInCombatOrPerforming)
        {
            _logger.LogDebug("Combat/Performance end");
            IsInCombatOrPerforming = false;
            // Leaving this check if its needed.
        }

        if (_condition[ConditionFlag.WatchingCutscene] && !IsInCutscene)
        {
            _logger.LogDebug("Cutscene start");
            IsInCutscene = true;
            // Leaving this check if its needed.
        }
        else if (!_condition[ConditionFlag.WatchingCutscene] && IsInCutscene)
        {
            _logger.LogDebug("Cutscene end");
            IsInCutscene = false;
            // Leaving this check if its needed.
        }

        if (IsInCutscene)
        {
            // Leaving this check if its needed.
            return;
        }

        if (_condition[ConditionFlag.BetweenAreas] || _condition[ConditionFlag.BetweenAreas51])
        {
            var zone = _clientState.TerritoryType;
            if (_lastZone != zone)
            {
                _lastZone = zone;
                if (!_sentBetweenAreas)
                {
                    _logger.LogDebug("Zone switch/Gpose start");
                    _sentBetweenAreas = true;
                    _playerInfoCache.Clear();
                    // Leaving this check if its needed.
                }
            }

            return;
        }

        if (_sentBetweenAreas)
        {
            _logger.LogDebug("Zone switch/Gpose end");
            _sentBetweenAreas = false;
            // Leaving this check if its needed.
        }

        var localPlayer = _clientState.LocalPlayer;
        if (localPlayer != null)
        {
            _classJobId = localPlayer.ClassJob.RowId;
        }

        Mediator.Publish(new PriorityFrameworkUpdateMessage());

        if (!IsInCombatOrPerforming)
            Mediator.Publish(new FrameworkUpdateMessage());

        if (isNormalFrameworkUpdate)
            return;

        if (localPlayer != null && !IsLoggedIn)
        {
            _logger.LogDebug("Logged in");
            IsLoggedIn = true;
            _lastZone = _clientState.TerritoryType;
            Mediator.Publish(new LoginMessage());
        }
        else if (localPlayer == null && IsLoggedIn)
        {
            _logger.LogDebug("Logged out");
            IsLoggedIn = false;
            Mediator.Publish(new LogoutMessage());
        }

        if (IsInCombatOrPerforming)
            Mediator.Publish(new FrameworkUpdateMessage());

        Mediator.Publish(new DelayedFrameworkUpdateMessage());

        _delayedFrameworkUpdateCheck = DateTime.UtcNow;
    }
}