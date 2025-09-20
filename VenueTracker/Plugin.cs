using Dalamud.Game.Command;
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
using VenueTracker.Windows;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Bindings.ImPlot;
using Dalamud.Bindings.ImGui;
using VenueTracker.Utils;

namespace VenueTracker;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    private const string CommandName = "/vtrack";

    public Configuration Configuration { get; init; }
    public GuestList GuestList;
    public PluginState PluginState { get; init; }
    public readonly WindowSystem WindowSystem = new("VenueTracker");
    public readonly Hooks Hooks;
    private ConfigWindow ConfigWindow { get; init; }
    private GuestsWindow GuestsWindow { get; init; }
    private readonly Doorbell doorbell;
    private bool running = false;
    private bool justEnteredHouse = false;

    public Plugin()
    {
        ImPlot.SetImGuiContext(ImGui.GetCurrentContext());
        ImPlot.SetCurrentContext(ImPlot.CreateContext());
        
        PluginState = new PluginState();
        GuestList = new GuestList();
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        GuestsWindow = new GuestsWindow(this);
        Hooks = new Hooks(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(GuestsWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the guest ui"
        });
        
        // Bind territory changed listener to client 
        ClientState.TerritoryChanged += OnTerritoryChanged;
        Framework.Update += OnFrameworkUpdate;
        ClientState.Logout += OnLogout;
        
        doorbell = new Doorbell(this);
        doorbell.Load();
        
        OnTerritoryChanged(ClientState.TerritoryType);

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleGuestsUI;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        
        doorbell.DisposeFile();
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        GuestsWindow.Dispose();
        
        Hooks.Dispose();

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
                    var isSelf = ClientState.LocalPlayer?.Name.TextValue == player.Name;
                    
                    // Store Player name 
                    if (ClientState.LocalPlayer?.Name.TextValue.Length > 0) PluginState.PlayerName = ClientState.LocalPlayer?.Name.TextValue ?? "";
                    
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
    
    public void ProcessIncomingRoll(string name, ushort homeWorldId, int roll, int outOf)
    {
        if (GuestList.Guests.ContainsKey(name) && GuestList.Guests[name].InHouse)
        {
            GuestList.Guests[name].LastRoll = roll;
            GuestList.Guests[name].LastRollMax = outOf == 0 ? 1000 : outOf;
        }
    }
    
    public void PlayDoorbell()
    {
        doorbell.Play();
    }
    
    public void ReloadDoorbell()
    {
        doorbell.Load();
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
        Chat.Print(entry);
    }
}
