using System;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VenueTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Plugin plugin;
    private Configuration Configuration;
    
    public ConfigWindow(Plugin plugin) : base("Venue Tracker###settings")
    {
        this.plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configValue = Configuration.SoundAlerts;
        if (ImGui.Checkbox("Add Doorbell Sound", ref configValue))
        {
            Configuration.SoundAlerts = configValue;
            Configuration.Save();
        }
        
        ImGui.Separator();
        ImGui.Spacing();
        
        var volume = Configuration.SoundVolume;
        if (ImGui.SliderFloat("Volume", ref volume, 0, 5))
        {
            Configuration.SoundVolume = volume;
            Configuration.Save();
            plugin.ReloadDoorbell();
        }
        
        ImGui.Separator();
        ImGui.Spacing();
        
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Music))
        {
            plugin.PlayDoorbell();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Test Sound");
        }
        
        ImGui.Separator();
        ImGui.Spacing();
    }
}
