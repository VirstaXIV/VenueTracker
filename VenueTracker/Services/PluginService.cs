using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.Utils;

namespace VenueTracker.Services;

public class PluginService : MediatorSubscriberBase, IHostedService
{
    private readonly ILogger<PluginService> _logger;
    private readonly UtilService _utilService;
    private readonly ConfigService _configService;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly PluginState _pluginState;
    private readonly IObjectTable _objectTable;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly GuestList _guestList;
    private readonly DoorbellService _doorbellService;
    private IServiceScope? _runtimeServiceScope;
    private Task? _launchTask = null;
    private bool running = false;
    private bool justEnteredHouse = false;
    
    public PluginService(ILogger<PluginService> logger, UtilService utilService, PluginState pluginState,
        IFramework framework, IClientState clientState, GuestList guestList, DoorbellService doorbellService, IObjectTable objectTable,
        ConfigService configService, IServiceScopeFactory serviceScopeFactory, VSyncMediator mediator) : base(logger, mediator)
    {
        _logger = logger;
        _utilService = utilService;
        _configService = configService;
        _pluginState = pluginState;
        _clientState = clientState;
        _objectTable = objectTable;
        _framework = framework;
        _serviceScopeFactory = serviceScopeFactory;
        _guestList = guestList;
        _doorbellService = doorbellService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        Logger.LogInformation("Launching {name} {major}.{minor}.{build}.{rev}", "VenueSync", version.Major, version.Minor, version.Build, version.Revision);
        Mediator.Publish(new EventMessage(new Services.Events.Event(nameof(PluginService), Services.Events.EventSeverity.Informational,
                                                                    $"Starting VenueSync {version.Major}.{version.Minor}.{version.Build}.{version.Revision}")));

        Mediator.Subscribe<LaunchTaskMessage>(this, (msg) => { if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunch); });
        Mediator.Subscribe<LoginMessage>(this, (_) => OnLogin());
        Mediator.Subscribe<LogoutMessage>(this, (_) => OnLogout());

        Mediator.StartQueueProcessing();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        UnsubscribeAll();

        OnLogout();

        Logger.LogDebug("Halting PluginService");

        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _clientState.TerritoryChanged -= OnTerritoryChanged;
    }
    
    private void EnteredHouse()
    {
        _pluginState.PlayerInHouse = true;
        justEnteredHouse = true;
    }
    
    private void LeftHouse()
    {
        _pluginState.PlayerInHouse = false;
        _pluginState.CurrentHouse = new House();
    }
    
    private void OnLogin()
    {
        Logger?.LogDebug("Client login");
    }

    private void OnLogout()
    {
        Logger?.LogDebug("Client logout");
        
        _pluginState.Territory = 0;
        LeftHouse();
    }

    private async Task WaitForPlayerAndLaunch()
    {
        while (!await _utilService.GetIsPlayerPresentAsync().ConfigureAwait(false))
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        try
        {
            _logger.LogDebug("Launching Managers");

            _runtimeServiceScope?.Dispose();
            _runtimeServiceScope = _serviceScopeFactory.CreateScope();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<UiService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CommandManagerService>();
        
            // Bind territory changed listener to client 
            _clientState.TerritoryChanged += OnTerritoryChanged;
            _framework.Update += OnFrameworkUpdate;
        
            OnTerritoryChanged(_clientState.TerritoryType);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error during launch of managers");
        }
    }
    
    private unsafe void OnTerritoryChanged(ushort territory)
    {
        _pluginState.Territory = territory;
        
        bool inHouse = false;
        try
        {
            var housingManager = HousingManager.Instance();
            inHouse = housingManager->IsInside();
        }
        catch (Exception ex) {
            _logger.LogWarning("Could not get housing state on territory change. " + ex.Message);
        }
        
        if (inHouse)
        {
            EnteredHouse();
        } else if (_pluginState.PlayerInHouse)
        {
            LeftHouse();
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (running) {
            _logger.LogWarning("Skipping processing while already running.");
            return;
        }
        running = true;

        try
        {
            if (_pluginState.PlayerInHouse)
            {
                try
                {
                    var housingManager = HousingManager.Instance();
                    var worldId = _clientState.LocalPlayer?.CurrentWorld.Value.RowId;
                    _pluginState.CurrentWorld = _clientState.LocalPlayer?.CurrentWorld.Value.Name.ToString();

                    if (_pluginState.CurrentHouse.HouseId != (long)housingManager->GetCurrentIndoorHouseId().Id &&
                        worldId != null)
                    {
                        _pluginState.CurrentHouse.HouseId = (long)housingManager->GetCurrentIndoorHouseId().Id;
                        _pluginState.CurrentHouse.Plot = housingManager->GetCurrentPlot() + 1; // Game stores plot as -1 
                        _pluginState.CurrentHouse.Ward = housingManager->GetCurrentWard() + 1; // Game stores ward as -1 
                        _pluginState.CurrentHouse.Room = housingManager->GetCurrentRoom();
                        _pluginState.CurrentHouse.Type = (ushort)HousingManager.GetOriginalHouseTerritoryTypeId();
                        _pluginState.CurrentHouse.District = TerritoryUtils.GetDistrict((long)housingManager->GetCurrentIndoorHouseId().Id);
                        
                        _guestList.Load();
                    }
                }
                catch
                {
                    running = false;
                }
                
                bool guestListUpdated = false;
                bool playerArrived = false;
                int playerCount = 0;

                var currentPlayer = _utilService.GetPlayerCharacter();
                
                Dictionary<string, bool> seenPlayers = new();
                foreach (var o in _objectTable)
                {
                    // Reject non player objects 
                    if (o is not IPlayerCharacter pc) continue;
                    var player = Player.FromCharacter(pc);
                    
                    // Skip player characters that do not have a name. 
                    // Portrait and Adventure plates show up with this. 
                    if (pc.Name.TextValue.Length == 0) continue;
                    // Im not sure what this means, but it seems that 4 is for players
                    if (o.SubKind != 4) continue;
                    playerCount++;
                    
                    // Add player to seen map 
                    if (seenPlayers.ContainsKey(player.Name))
                        seenPlayers[player.Name] = true;
                    else
                        seenPlayers.Add(player.Name, true);
                    
                    // Is the new player the current user 
                    var isSelf = currentPlayer?.Name.TextValue == player.Name;
                    
                    // New Player has entered the house 
                    if (!_guestList.Guests.ContainsKey(player.Name))
                    {
                        guestListUpdated = true;
                        _guestList.Guests.Add(player.Name, player);
                        _utilService.ChatPlayerLink(player, " has come inside.");
                        if (!isSelf) playerArrived = true;
                    }
                    // Mark the player as re-entering the venue 
                    else if (!_guestList.Guests[player.Name].InHouse)
                    {
                        guestListUpdated = true;
                        _guestList.Guests[player.Name].InHouse = true;
                        _guestList.Guests[player.Name].LatestEntry = DateTime.Now;
                        _guestList.Guests[player.Name].TimeCursor = DateTime.Now;
                        _guestList.Guests[player.Name].EntryCount++;
                        _utilService.ChatPlayerLink(player, " has come inside.");
                        if (!isSelf) playerArrived = true;
                    }
                    // Current user just entered house
                    else if (justEnteredHouse)
                    {
                        _guestList.Guests[player.Name].TimeCursor = DateTime.Now;
                    }
                    
                    // Re-mark as friend incase status changed 
                    _guestList.Guests[player.Name].IsFriend = pc.StatusFlags.HasFlag(StatusFlags.Friend);
                    
                    // Mark last seen 
                    _guestList.Guests[player.Name].LastSeen = DateTime.Now;
                    
                    // Mark last time current player enter house 
                    if (justEnteredHouse && isSelf)
                    {
                        _guestList.Guests[player.Name].LatestEntry = DateTime.Now;
                    }
                }
                
                // Check for guests that have left the house 
                foreach (var guest in _guestList.Guests)
                {
                    // Guest is marked as in the house 
                    if (guest.Value.InHouse) 
                    {
                        // Guest was not seen this loop 
                        if (!seenPlayers.ContainsKey(guest.Value.Name))
                        {
                            guest.Value.OnLeaveHouse();
                            guestListUpdated = true;
                        }
                        // Guest was seen this loop 
                        else 
                        {
                            guest.Value.OnAccumulateTime();
                        }
                    }
                }
                
                if (_configService.Current.SoundAlerts && playerArrived)
                {
                    _doorbellService.PlayDoorbell();
                }
                
                // Save number of players seen this update 
                _pluginState.PlayersInHouse = playerCount;
                
                // Save config if we saw new players
                if (guestListUpdated) _guestList.Save();
                
                justEnteredHouse = false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Venue Tracker Failed during framework update");
            _logger.LogError(e.ToString());
        }
        running = false;
    }
}
