using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services;
using VenueTracker.Services.Config;
using Task = System.Threading.Tasks.Task;

namespace VenueTracker.UI.Components;

public class GuestsListWidget : IDisposable, IHostedService
{
    private readonly ILogger<GuestsListWidget> _logger;
    private readonly ConfigService _configService;
    private readonly PluginState _pluginState;
    private readonly GuestList _guestList;
    private readonly UtilService _utilService;
    private static unsafe string GetUserPath() => Framework.Instance()->UserPathString;
    
    public GuestsListWidget(ILogger<GuestsListWidget> logger, ConfigService configService, PluginState pluginState, 
        GuestList guestList, UtilService utilService)
    {
        _logger = logger;
        _configService = configService;
        _pluginState = pluginState;
        _guestList = guestList;
        _utilService = utilService;
    }
    
    public void Dispose()
    {
        //
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public unsafe void Draw(long houseId)
    {
        DrawGuestTable(houseId);
    }

    public unsafe void ResetRolls(long houseId)
    {
        ResetGuestRolls(houseId);
    }
    
    public unsafe void ResetTable(long houseId)
    {
        ResetGuestTable(houseId);
    }

    public unsafe int GetActiveCount()
    {
        var activeGuestsList = _guestList.Guests.Where(player => player.Value.InHouse == true);
        var activeGuests = activeGuestsList.ToDictionary(player => player.Value.Name, player => player.Value);
        
        return activeGuests.Count;
    }
    
    public unsafe int GetTotalCount()
    {
        return _guestList.Guests.Count;
    }
    
    public void ProcessIncomingRoll(string name, ushort homeWorldId, int roll, int outOf)
    {
        if (_guestList.Guests.ContainsKey(name) && _guestList.Guests[name].InHouse)
        {
            _guestList.Guests[name].LastRoll = roll;
            _guestList.Guests[name].LastRollMax = outOf == 0 ? 1000 : outOf;
        }
    }

    private List<KeyValuePair<string, Player>> GetSortedGuests(ImGuiTableSortSpecsPtr sortSpecs, long houseId)
    {
        ImGuiTableColumnSortSpecsPtr currentSpecs = sortSpecs.Specs;

        var guestList = _guestList.Guests.ToList();
        
        guestList.Sort((pair1, pair2) => {
        // Filter friends to top 
        if (_configService.Current.SortFriendsToTop && pair1.Value.IsFriend != pair2.Value.IsFriend && 
            ((_configService.Current.SortCurrentVisitorsTop && pair1.Value.InHouse == pair2.Value.InHouse) || !_configService.Current.SortCurrentVisitorsTop)) {
            return pair2.Value.IsFriend.CompareTo(pair1.Value.IsFriend);
        } 
        // Filter in house to top 
        else if (_configService.Current.SortCurrentVisitorsTop && pair1.Value.InHouse != pair2.Value.InHouse) {
            return pair2.Value.InHouse.CompareTo(pair1.Value.InHouse);
        }
        // Other general sorts 
        else
        {
            switch (currentSpecs.ColumnIndex)
            {
                case 0: // Latest Entry
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.LatestEntry.CompareTo(pair1.Value.LatestEntry);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.LatestEntry.CompareTo(pair2.Value.LatestEntry);
                    break;
                case 1: // Name
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.Name.CompareTo(pair1.Value.Name);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.Name.CompareTo(pair2.Value.Name);
                    break;
                case 2: // Entry Count
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.EntryCount.CompareTo(pair1.Value.EntryCount);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.EntryCount.CompareTo(pair2.Value.EntryCount);
                    break;
                case 3: // Last Roll
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.LastRoll.CompareTo(pair1.Value.LastRoll);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.LastRoll.CompareTo(pair2.Value.LastRoll);
                    break;
                case 4: // Last Roll Max
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.LastRollMax.CompareTo(pair1.Value.LastRollMax);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.LastRollMax.CompareTo(pair2.Value.LastRollMax);
                    break;
                case 5: // Minutes Inside
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.MilisecondsInHouse.CompareTo(pair1.Value.MilisecondsInHouse);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.MilisecondsInHouse.CompareTo(pair2.Value.MilisecondsInHouse);
                    break;
                case 6: // First Seen
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.FirstSeen.CompareTo(pair1.Value.FirstSeen);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.FirstSeen.CompareTo(pair2.Value.FirstSeen);
                    break;
                case 7: // Last Seen
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.LastSeen.CompareTo(pair1.Value.LastSeen);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.LastSeen.CompareTo(pair2.Value.LastSeen);
                    break;
                case 8: // Last Seen
                    if (currentSpecs.SortDirection == ImGuiSortDirection.Descending)
                        return pair2.Value.WorldName.CompareTo(pair1.Value.WorldName);
                    else if (currentSpecs.SortDirection == ImGuiSortDirection.Ascending)
                        return pair1.Value.WorldName.CompareTo(pair2.Value.WorldName);
                    break;
                default:
                    break;
            }
        }

        return 0;
        });
        
        return guestList;
    }

    private void ResetGuestRolls(long houseId)
    {
        foreach (var (name, player) in _guestList.Guests)
        {
            _guestList.Guests[name].LastRoll = 0;
            _guestList.Guests[name].LastRollMax = 1000;
        }
    }
    
    private void ResetGuestTable(long houseId)
    {
        _guestList.ClearList();
    }

    private void DrawGuestTable(long houseId)
    {
        ImGui.BeginChild(1);

        if (ImGui.BeginTable("Guests", 9, ImGuiTableFlags.Sortable))
        {
            ImGui.TableSetupColumn("Latest Entry", ImGuiTableColumnFlags.DefaultSort);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Entries", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Roll", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Roll Max", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Minutes", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("First Seen");
            ImGui.TableSetupColumn("Last Seen");
            ImGui.TableSetupColumn("World");
            ImGui.TableHeadersRow();
            
            ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
            var sortedGuestList = GetSortedGuests(sortSpecs, houseId);

            foreach (var player in sortedGuestList)
            {
                var playerColor = Colors.GetGuestListColor(player.Value, true);
                var color = Colors.GetGuestListColor(player.Value, false);

                if (!player.Value.InHouse && _pluginState.CurrentHouse.HouseId == houseId) {
                    color[3] = .5f;
                    playerColor[3] = .5f;
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(color, player.Value.LatestEntry.ToString("h:mm tt"));
                ImGui.TableNextColumn();
                ImGui.TextColored(playerColor, player.Value.Name);
                if (ImGui.IsItemClicked()) {
                    _utilService.ChatPlayerLink(player.Value);
                }
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.EntryCount);
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.LastRoll);
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.LastRollMax);
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.GetTimeInHouse(_pluginState.CurrentHouse.HouseId == houseId));
                ImGui.TableNextColumn();
                ImGui.TextColored(color, player.Value.FirstSeen.ToString("h:mm tt"));
                ImGui.TableNextColumn();
                ImGui.TextColored(color, player.Value.LastSeen.ToString("h:mm tt"));
                ImGui.TableNextColumn();
                ImGui.TextColored(color, player.Value.WorldName);
            }

            ImGui.EndTable();
        }
        
        ImGui.EndChild();
    }
}
