using System;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VenueTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private string endpointUrl;
    private string serverKey;
    
    public ConfigWindow(Plugin plugin) : base("Venue Tracker###settings")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;
        endpointUrl = this.plugin.Configuration.EndpointUrl;
        serverKey = this.plugin.Configuration.ServerKey;
    }

    public void Dispose() { }

    public void UpdateServerKey()
    {
        serverKey = this.plugin.Configuration.ServerKey;
    }

    public override void Draw()
    {
        ImGui.InputTextWithHint("Server", "https://ffxivvenuesync.com/api", ref endpointUrl, 256);
        ImGui.SameLine();
        bool canAdd = endpointUrl.Length > 0;
        if (!canAdd) ImGui.BeginDisabled();
        if (ImGui.Button("Save Endpoint"))
        {
            plugin.Configuration.EndpointUrl = endpointUrl;
            plugin.Configuration.Save();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (endpointUrl.Length == 0)
                ImGui.SetTooltip("Please enter a Url");
        }
        if (!canAdd) ImGui.EndDisabled();
        
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.InputTextWithHint("Access Key", "", ref serverKey, 256);
        ImGui.SameLine();
        if (plugin.IsRequestingApi) ImGui.BeginDisabled();
        if (ImGui.Button("Get Key"))
        {
            _ = plugin.VenueSyncApi.Register(this);
        }
        if (plugin.IsRequestingApi) ImGui.EndDisabled();
        
        
        var friendsTopValue = configuration.SortFriendsToTop;
        if (ImGui.Checkbox("Sort Friends to Top", ref friendsTopValue))
        {
            configuration.SortFriendsToTop = friendsTopValue;
            configuration.Save();
        }
        
        ImGui.Separator();
        ImGui.Spacing();
        
        var soundAlertsValue = configuration.SoundAlerts;
        if (ImGui.Checkbox("Add Doorbell Sound", ref soundAlertsValue))
        {
            configuration.SoundAlerts = soundAlertsValue;
            configuration.Save();
        }
        
        ImGui.Separator();
        ImGui.Spacing();
        
        var volume = configuration.SoundVolume;
        if (ImGui.SliderFloat("Volume", ref volume, 0, 5))
        {
            configuration.SoundVolume = volume;
            configuration.Save();
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
