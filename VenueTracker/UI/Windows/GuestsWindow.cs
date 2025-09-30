using System.Numerics;
using Dalamud.Bindings.ImGui;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Components;

namespace VenueTracker.UI.Windows;

public class GuestsWindow : WindowMediatorSubscriberBase
{
    private readonly ConfigService _configService;
    private readonly ApiService _apiService;
    private readonly PluginState _pluginState;
    private readonly GuestsListWidget _guestsListWidget;
    
    public GuestsWindow(ILogger<GuestsWindow> logger, VSyncMediator mediator, ConfigService configService, 
        ApiService apiService, GuestsListWidget guestsListWidget, PluginState pluginState)
        : base(logger, mediator, "Guests##main")
    {
        _configService = configService;
        _apiService = apiService;
        _pluginState = pluginState;
        _guestsListWidget = guestsListWidget;
        
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }
    
    protected override void DrawInternal()
    {
        DrawGuestsContent();
    }

    private void DrawGuestsContent()
    {
        if (_pluginState.PlayerInHouse)
        {
            if (_apiService.IsServerLoggedIn())
            {
                if (_apiService.IsRequestingApi)
                {
                    ImGui.Text("Requesting API");
                }
                else
                {
                    if (!_apiService.HadApiError && _pluginState.CurrentWorld != null)
                    {
                        _ = _apiService.GetVenue(
                            _pluginState.CurrentWorld,
                            _pluginState.CurrentHouse.District,
                            _pluginState.CurrentHouse.Ward,
                            _pluginState.CurrentHouse.Plot);
                    }
                    else
                    {
                        ImGui.Text("Could not retrieve Venue Data");
                    }
                }
            }
            else
            {
                var typeText = "";
                if (_pluginState.CurrentHouse.Plot > 0) typeText += "P" + _pluginState.CurrentHouse.Plot;
                if (_pluginState.CurrentHouse.Room > 0) typeText += "Room" + _pluginState.CurrentHouse.Room;
                ImGui.Text(
                    "You are in a " + TerritoryUtils.GetHouseType(_pluginState.CurrentHouse.Type) + 
                    " in " + _pluginState.CurrentHouse.District + 
                    " W" + _pluginState.CurrentHouse.Ward + " " + typeText +
                    " With " + _guestsListWidget.GetActiveCount() + " People Inside" +
                    " And " + _guestsListWidget.GetTotalCount() + " People Total");
            }

            if (ImGui.Button("Reset Rolls"))
            {
                _guestsListWidget.ResetRolls(_pluginState.CurrentHouse.HouseId);
            }
            
            if (ImGui.Button("Reset Table"))
            {
                _guestsListWidget.ResetTable(_pluginState.CurrentHouse.HouseId);
            }
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            _guestsListWidget.Draw(_pluginState.CurrentHouse.HouseId);
        }
        else
        {
            ImGui.TextUnformatted($"Currently not in a house");
        }
    }
}
