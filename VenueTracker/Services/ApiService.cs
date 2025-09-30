using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Api;
using VenueTracker.Data;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Windows;

namespace VenueTracker.Services;

public class ApiService : IHostedService, IMediatorSubscriber
{
    private readonly ILogger<ApiService> _logger;
    private readonly ConfigService _configService;
    private readonly Request _request;
    private readonly PluginState _pluginState;
    private readonly UtilService _utilService;
    private readonly ConfigWindow _configWindow;
    
    public bool IsRequestingApi = false;
    public bool HadApiError = false;
    private bool _isLoggedInApi = false;
    
    public ApiService(ILogger<ApiService> logger, ConfigService configService, Request request, 
        PluginState pluginState, UtilService utilService, ConfigWindow configWindow)
    {
        _logger = logger;
        _configService = configService;
        _request = request;
        _pluginState = pluginState;
        _utilService = utilService;
        _configWindow = configWindow;
    }
    
    public VSyncMediator Mediator { get; }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public class RegistrationData
    {
        // ReSharper disable once InconsistentNaming
        public required string name { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string world { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string key { get; set; }
    }
    
    public class LoginData
    {
        // ReSharper disable once InconsistentNaming
        public required string token { get; set; }
    }
    
    public class RegistrationRequest
    {
        // ReSharper disable once InconsistentNaming
        public required string name { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string world { get; set; }
    }
    
    public class LoginRequest
    {
        // ReSharper disable once InconsistentNaming
        public required string name { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string world { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string key { get; set; }
    }
    
    public void ServerLoggedIn()
    {
        _isLoggedInApi = true;
    }

    public void ServerLoggedOut()
    {
        _isLoggedInApi = false;
    }

    public bool IsServerLoggedIn()
    {
        return _isLoggedInApi;
    }

    public async Task Login()
    {
        IsRequestingApi = true;
        var currentPlayer = _utilService.GetPlayerCharacter();

        var loginRequest = new LoginRequest
        {
            name = currentPlayer.Name.TextValue,
            world = currentPlayer.HomeWorld.Value.Name.ToString(),
            key = _configService.Current.ServerKey
        };
        
        var jsonContent = JsonSerializer.Serialize((loginRequest));
        
        var responseData = await _request.PostAsync(_configService.Current.EndpointUrl + "/login", jsonContent, false);
        if (responseData != null)
        {
            var loginData = JsonSerializer.Deserialize<LoginData>(responseData);

            if (loginData != null)
            {
                _pluginState.ServerToken = loginData.token;
                _configWindow.UpdateServerToken(_pluginState.ServerToken);
                ServerLoggedIn();
            }
            else
            {
                Plugin.Log.Warning("Failed to deserialize response data");
            }
        }
        else
        {
            Plugin.Log.Warning("Login request failed");
        }

        IsRequestingApi = false;
    }

    public async Task Register()
    {
        IsRequestingApi = true;
        var currentPlayer = _utilService.GetPlayerCharacter();

        var registrationRequest = new RegistrationRequest
        {
            name = currentPlayer.Name.TextValue,
            world = currentPlayer.HomeWorld.Value.Name.ToString()
        };

        var jsonContent = JsonSerializer.Serialize((registrationRequest));

        var responseData = await _request.PostAsync(_configService.Current.EndpointUrl + "/register", jsonContent, false);
        if (responseData != null)
        {
            var registrationData = JsonSerializer.Deserialize<RegistrationData>(responseData);

            if (registrationData != null)
            {
                _configService.Current.ServerKey = registrationData.key;
                _configService.Save();
                _configWindow.UpdateServerKey();
            }
            else
            {
                Plugin.Log.Warning("Failed to deserialize response data");
            }
        }
        else
        {
            Plugin.Log.Warning("Registration request failed");
        }

        IsRequestingApi = false;
    }

    public async Task GetVenue(string world, string district, int ward, int plot)
    {
        IsRequestingApi = true;
        
        var requestUri = $"{_configService.Current.EndpointUrl}/get_venue";
        var queryString = $"?world={world}&district={district}&ward={ward}&plot={plot}";

        var responseData = await _request.GetAsync(requestUri + queryString, true);
        if (responseData != null)
        {
            var venueData = JsonSerializer.Deserialize<dynamic>(responseData);

            if (venueData != null)
            {
                //
            }
            else
            {
                HadApiError = true;
                Plugin.Log.Warning("Failed to deserialize response data");
            }
        }
        else
        {
            HadApiError = true;
            Plugin.Log.Warning("Get Venue request failed");
        }

        IsRequestingApi = false;
    }
}
