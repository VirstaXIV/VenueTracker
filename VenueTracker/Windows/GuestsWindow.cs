using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using VenueTracker.UI;

namespace VenueTracker.Windows;

public class GuestsWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly GuestsListWidget guestsListWidget;
    
    public GuestsWindow(Plugin plugin)
        : base("Guests##main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.plugin = plugin;
        guestsListWidget = new GuestsListWidget(plugin);
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (plugin.PluginState.PlayerInHouse)
        {
            var typeText = "";
            if (plugin.PluginState.CurrentHouse.Plot > 0) typeText += "P" + plugin.PluginState.CurrentHouse.Plot;
            if (plugin.PluginState.CurrentHouse.Room > 0) typeText += "Room" + plugin.PluginState.CurrentHouse.Room;
            ImGui.Text(
                "You are in a " + TerritoryUtils.GetHouseType(plugin.PluginState.CurrentHouse.Type) + 
                " in " + plugin.PluginState.CurrentHouse.District + 
                " W" + plugin.PluginState.CurrentHouse.Ward + " " + typeText +
                " With " + guestsListWidget.GetCount() + " People Inside");

            if (ImGui.Button("Reset Rolls"))
            {
                guestsListWidget.ResetRolls(plugin.PluginState.CurrentHouse.HouseId);
            }
            
            if (ImGui.Button("Reset Table"))
            {
                guestsListWidget.ResetTable(plugin.PluginState.CurrentHouse.HouseId);
            }
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            guestsListWidget.Draw(plugin.PluginState.CurrentHouse.HouseId);
        }
        else
        {
            ImGui.TextUnformatted($"Currently not in a house");
        }
    }
}
