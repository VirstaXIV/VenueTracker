using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;

namespace VenueTracker.UI.Windows;

public class ConfigWindow : WindowMediatorSubscriberBase
{
    private readonly ConfigService _configService;
    private readonly DoorbellService _doorbellService;
    private readonly ApiService _apiService;
    private readonly PluginState _pluginState;
    
    private string _endpointUrl;
    private string _serverKey;
    private string _serverToken;
    
    public ConfigWindow(ILogger<ConfigWindow> logger, VSyncMediator mediator, 
        ConfigService configService, DoorbellService doorbellService, ApiService apiService, 
        PluginState pluginState) : base(logger, mediator, "Venue Tracker###settings")
    {
        _configService = configService;
        _doorbellService = doorbellService;
        _apiService = apiService;
        _pluginState = pluginState;
        _endpointUrl = _configService.Current.EndpointUrl;
        _serverKey = _configService.Current.ServerKey;
        _serverToken = _pluginState.ServerToken;
        
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(600, 2000),
        };
    }
    
    protected override void DrawInternal()
    {
        DrawSettingsContent();
    }

    public void UpdateServerKey()
    {
        _serverKey = _configService.Current.ServerKey;
    }

    public void UpdateServerToken(string token)
    {
        _serverToken = token;
    }

    private void DrawSettingsContent()
    {
        ImGui.Text("API Settings");
        ImGui.InputTextWithHint("Server", "https://ffxivvenuesync.com/api", ref _endpointUrl, 256);
        ImGui.SameLine();
        bool canAdd = _endpointUrl.Length > 0;
        if (!canAdd) ImGui.BeginDisabled();
        if (ImGui.Button("Save Endpoint"))
        {
            _configService.Current.EndpointUrl = _endpointUrl;
            _configService.Save();
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (_endpointUrl.Length == 0)
                ImGui.SetTooltip("Please enter a Url");
        }
        if (!canAdd) ImGui.EndDisabled();
        
        ImGui.Spacing();
        
        ImGui.InputTextWithHint("Access Key", "", ref _serverKey, 256);
        ImGui.SameLine();
        if (_apiService.IsRequestingApi) ImGui.BeginDisabled();
        if (ImGui.Button("Get Key"))
        {
            _ = _apiService.Register();
        }
        if (_apiService.IsRequestingApi) ImGui.EndDisabled();
        
        ImGui.Spacing();
        
        ImGui.InputTextWithHint("Access Token", "", ref _serverToken, 256);
        ImGui.SameLine();
        if (_apiService.IsRequestingApi) ImGui.BeginDisabled();
        if (ImGui.Button("Login"))
        {
            _ = _apiService.Login();
        }
        if (_apiService.IsRequestingApi) ImGui.EndDisabled();
        
        ImGui.Separator();
        ImGui.Text("General Settings");
        ImGui.Spacing();
        
        var friendsTopValue = _configService.Current.SortFriendsToTop;
        if (ImGui.Checkbox("Sort Friends to Top", ref friendsTopValue))
        {
            _configService.Current.SortFriendsToTop = friendsTopValue;
            _configService.Save();
        }
        ImGui.Spacing();
        
        var soundAlertsValue = _configService.Current.SoundAlerts;
        if (ImGui.Checkbox("Add Doorbell Sound", ref soundAlertsValue))
        {
            _configService.Current.SoundAlerts = soundAlertsValue;
            _configService.Save();
        }
        
        ImGui.Spacing();
        
        var volume = _configService.Current.SoundVolume;
        if (ImGui.SliderFloat("Volume", ref volume, 0, 5))
        {
            _configService.Current.SoundVolume = volume;
            _configService.Save();
            _doorbellService.ReloadDoorbell();
        }
        
        ImGui.Spacing();
        
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Music))
        {
            _doorbellService.PlayDoorbell();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Test Sound");
        }
        ImGui.Spacing();
    }
}
