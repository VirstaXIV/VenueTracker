using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Dalamud.Bindings.ImGui;

namespace VenueTracker.UI;

public class GuestsListWidget
{
    private readonly Plugin plugin;
    private static unsafe string GetUserPath() => Framework.Instance()->UserPathString;
    
    public GuestsListWidget(Plugin plugin)
    {
        this.plugin = plugin;
    }
    
    public unsafe void Draw(long houseId)
    {
        DrawGuestTable(houseId);
    }

    private List<KeyValuePair<string, Player>> GetSortedGuests(ImGuiTableSortSpecsPtr sortSpecs, long houseId)
    {
        ImGuiTableColumnSortSpecsPtr currentSpecs = sortSpecs.Specs;

        var guestList = plugin.GuestList.Guests.ToList();
        return guestList;
    }

    private void DrawGuestTable(long houseId)
    {
        ImGui.BeginChild(1);

        if (ImGui.BeginTable("Guests", 7, ImGuiTableFlags.Sortable))
        {
            ImGui.TableSetupColumn("Latest Entry", ImGuiTableColumnFlags.DefaultSort);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Entries", ImGuiTableColumnFlags.WidthFixed, 60);
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

                if (!player.Value.InHouse && plugin.PluginState.CurrentHouse.HouseId == houseId) {
                    color[3] = .5f;
                    playerColor[3] = .5f;
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(color, player.Value.LatestEntry.ToString("h:mm tt"));
                ImGui.TableNextColumn();
                ImGui.TextColored(playerColor, player.Value.Name);
                if (ImGui.IsItemClicked()) {
                    plugin.ChatPlayerLink(player.Value);
                }
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.EntryCount);
                ImGui.TableNextColumn();
                ImGui.TextColored(color, "" + player.Value.GetTimeInHouse(plugin.PluginState.CurrentHouse.HouseId == houseId));
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
