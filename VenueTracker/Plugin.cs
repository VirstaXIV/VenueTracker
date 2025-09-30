using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Bindings.ImPlot;
using Dalamud.Bindings.ImGui;
using VenueTracker.Api;
using VenueTracker.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data.Config;
using VenueTracker.Services;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Components;
using VenueTracker.UI.Windows;

namespace VenueTracker;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;
    public static Plugin Self;
    
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!; 
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IToastGui ToastGui { get; private set; } = null!;
    [PluginService] public static INamePlateGui NamePlateGui { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IPartyList PartyList { get; private set; } = null!; 
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;

    private const string CommandName = "/vtrack";
    private bool running = false;
    private bool justEnteredHouse = false;

    public Plugin()
    {
        Plugin.Self = this;
        _host = new HostBuilder()
                .UseContentRoot(PluginInterface.ConfigDirectory.FullName)
                .ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddDalamudLogging(Log);
                    lb.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(new WindowSystem("VenueSync"));
                    
                    // dalamud services
                    collection.AddSingleton(_ => PluginInterface);
                    collection.AddSingleton(_ => PluginInterface.UiBuilder);
                    collection.AddSingleton(_ => CommandManager);
                    collection.AddSingleton(_ => DataManager);
                    collection.AddSingleton(_ => Framework);
                    collection.AddSingleton(_ => Objects);
                    collection.AddSingleton(_ => ClientState);
                    collection.AddSingleton(_ => Condition);
                    collection.AddSingleton(_ => GameGui);
                    collection.AddSingleton(_ => ChatGui);
                    collection.AddSingleton(_ => DtrBar);
                    collection.AddSingleton(_ => ToastGui);
                    collection.AddSingleton(_ => Log);
                    collection.AddSingleton(_ => TargetManager);
                    collection.AddSingleton(_ => NotificationManager);
                    collection.AddSingleton(_ => TextureProvider);
                    collection.AddSingleton(_ => ContextMenu);
                    collection.AddSingleton(_ => GameInteropProvider);
                    collection.AddSingleton(_ => NamePlateGui);
                    collection.AddSingleton(_ => GameConfig);
                    collection.AddSingleton(_ => PartyList);
                    
                    // VenueSync Services
                    collection.AddSingleton<VSyncMediator>();
                    collection.AddSingleton<Request>();
                    collection.AddSingleton<DoorbellService>();
                    collection.AddSingleton<HookService>();
                    collection.AddSingleton<ApiService>();
                    collection.AddSingleton<PluginState>();
                    collection.AddSingleton<GuestsListWidget>();
                    collection.AddSingleton<GuestList>();
                    collection.AddSingleton<UtilService>();
                    
                    collection.AddSingleton((s) => new ConfigService(PluginInterface.ConfigDirectory.FullName));
                    collection.AddSingleton<IConfigService<IVSyncConfiguration>>(s => s.GetRequiredService<ConfigService>());
                    collection.AddSingleton<ConfigurationSaveService>();
                    
                    // scoped services
                    collection.AddScoped<WindowMediatorSubscriberBase, ConfigWindow>();
                    
                    collection.AddHostedService(p => p.GetRequiredService<ConfigurationSaveService>());
                    collection.AddHostedService(p => p.GetRequiredService<VSyncMediator>());
                })
                .Build();
            
            
            
        ImPlot.SetImGuiContext(ImGui.GetCurrentContext());
        ImPlot.SetCurrentContext(ImPlot.CreateContext());

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the guest ui"
        });
        
        // Bind territory changed listener to client 
        ClientState.TerritoryChanged += OnTerritoryChanged;
        Framework.Update += OnFrameworkUpdate;
        ClientState.Logout += OnLogout;
        
        OnTerritoryChanged(ClientState.TerritoryType);

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleGuestsUI;
    }

    public void Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        
        Framework.Update -= OnFrameworkUpdate;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        GuestsWindow.Dispose();
        
        HookService.Dispose();
        Request.Dispose();

        CommandManager.RemoveHandler(CommandName);
        
        ImPlot.DestroyContext();
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        ToggleGuestsUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleGuestsUI() => GuestsWindow.Toggle();
    
    private void OnLogout(int type, int code)
    {
        PluginState.Territory = 0;

        LeftHouse();
    }
    
    private void EnteredHouse()
    {
        PluginState.PlayerInHouse = true;
        justEnteredHouse = true;
    }
    
    private void LeftHouse()
    {
        PluginState.PlayerInHouse = false;
        PluginState.CurrentHouse = new House();
    }
    
    private unsafe void OnTerritoryChanged(ushort territory)
    {
        PluginState.Territory = territory;
        
        bool inHouse = false;
        try
        {
            var housingManager = HousingManager.Instance();
            inHouse = housingManager->IsInside();
        }
        catch (Exception ex) {
            Log.Warning("Could not get housing state on territory change. " + ex.Message);
        }
        
        if (inHouse)
        {
            EnteredHouse();
        } else if (PluginState.PlayerInHouse)
        {
            LeftHouse();
        }
        
        Configuration.Save();
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (running) {
            Log.Warning("Skipping processing while already running.");
            return;
        }
        running = true;

        try
        {
            if (PluginState.PlayerInHouse)
            {
                try
                {
                    var housingManager = HousingManager.Instance();
                    var worldId = ClientState.LocalPlayer?.CurrentWorld.Value.RowId;
                    PluginState.CurrentWorld = ClientState.LocalPlayer?.CurrentWorld.Value.Name.ToString();

                    if (PluginState.CurrentHouse.HouseId != (long)housingManager->GetCurrentIndoorHouseId().Id &&
                        worldId != null)
                    {
                        PluginState.CurrentHouse.HouseId = (long)housingManager->GetCurrentIndoorHouseId().Id;
                        PluginState.CurrentHouse.Plot = housingManager->GetCurrentPlot() + 1; // Game stores plot as -1 
                        PluginState.CurrentHouse.Ward = housingManager->GetCurrentWard() + 1; // Game stores ward as -1 
                        PluginState.CurrentHouse.Room = housingManager->GetCurrentRoom();
                        PluginState.CurrentHouse.Type = (ushort)HousingManager.GetOriginalHouseTerritoryTypeId();
                        PluginState.CurrentHouse.District = TerritoryUtils.GetDistrict((long)housingManager->GetCurrentIndoorHouseId().Id);
                        
                        GuestList.Load();
                    }
                }
                catch
                {
                    running = false;
                }
                
                bool guestListUpdated = false;
                bool playerArrived = false;
                int playerCount = 0;

                var currentPlayer = GetCurrentPlayer();
                
                Dictionary<string, bool> seenPlayers = new();
                foreach (var o in Objects)
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
                    if (!GuestList.Guests.ContainsKey(player.Name))
                    {
                        guestListUpdated = true;
                        GuestList.Guests.Add(player.Name, player);
                        ChatPlayerLink(player, " has come inside.");
                        if (!isSelf) playerArrived = true;
                    }
                    // Mark the player as re-entering the venue 
                    else if (!GuestList.Guests[player.Name].InHouse)
                    {
                        guestListUpdated = true;
                        GuestList.Guests[player.Name].InHouse = true;
                        GuestList.Guests[player.Name].LatestEntry = DateTime.Now;
                        GuestList.Guests[player.Name].TimeCursor = DateTime.Now;
                        GuestList.Guests[player.Name].EntryCount++;
                        ChatPlayerLink(player, " has come inside.");
                        if (!isSelf) playerArrived = true;
                    }
                    // Current user just entered house
                    else if (justEnteredHouse)
                    {
                        GuestList.Guests[player.Name].TimeCursor = DateTime.Now;
                    }
                    
                    // Re-mark as friend incase status changed 
                    GuestList.Guests[player.Name].IsFriend = pc.StatusFlags.HasFlag(StatusFlags.Friend);
                    
                    // Mark last seen 
                    GuestList.Guests[player.Name].LastSeen = DateTime.Now;
                    
                    // Mark last time current player enter house 
                    if (justEnteredHouse && isSelf)
                    {
                        GuestList.Guests[player.Name].LatestEntry = DateTime.Now;
                    }
                }
                
                // Check for guests that have left the house 
                foreach (var guest in GuestList.Guests)
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
                
                if (Configuration.SoundAlerts && playerArrived)
                {
                    doorbell.Play();
                }
                
                // Save number of players seen this update 
                PluginState.PlayersInHouse = playerCount;
                
                // Save config if we saw new players
                if (guestListUpdated) GuestList.Save();
                
                justEnteredHouse = false;
            }
        }
        catch (Exception e)
        {
            Log.Error("Venue Tracker Failed during framework update");
            Log.Error(e.ToString());
        }
        running = false;
    }
}
