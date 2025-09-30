using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using VenueTracker.Windows;

namespace VenueTracker.Utils;

public class VenueSyncApi(Plugin plugin)
{
    public class RegistrationData
    {
        // ReSharper disable once InconsistentNaming
        public required string name { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string world { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string key { get; set; }
    }
    
    public class RegistrationRequest
    {
        // ReSharper disable once InconsistentNaming
        public required string name { get; set; }
        // ReSharper disable once InconsistentNaming
        public required string world { get; set; }
    }
    
    private static string CurrentEndpoint = "";
    private static HttpClient Client = new();
    private static bool HeadersChanged = true;

    public async Task<string?> PostAsync(string url, string content, bool authenticated = true)
    {
        if (url != CurrentEndpoint || HeadersChanged)
        {
            Client = new HttpClient()
            {
                BaseAddress = new Uri(url)
            };

            if (authenticated)
            {
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + plugin.Configuration.ServerKey);
            }
            
            HeadersChanged = false;
            CurrentEndpoint = url;
        }
        
        StringContent stringContent = new(content, Encoding.UTF8, "application/json");

        try
        {
            using HttpResponseMessage response = await Client.PostAsync("", stringContent);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        } catch {
            Plugin.Log.Warning("Failed to post to " + url);
        }
        
        return null;
    }

    public async Task Register(ConfigWindow configWindow)
    {
        plugin.IsRequestingApi = true;
        var currentPlayer = plugin.GetCurrentPlayer();

        if (currentPlayer != null)
        {
            var registrationRequest = new RegistrationRequest
            {
                name = currentPlayer.Name.TextValue,
                world = currentPlayer.HomeWorld.Value.Name.ToString()
            };

            var jsonContent = JsonSerializer.Serialize((registrationRequest));

            var responseData = await PostAsync(plugin.Configuration.EndpointUrl + "/register", jsonContent);
            if (responseData != null)
            {
                var registrationData = JsonSerializer.Deserialize<RegistrationData>(responseData);

                if (registrationData != null)
                {
                    plugin.Configuration.ServerKey = registrationData.key;
                    plugin.Configuration.Save();
                    configWindow.UpdateServerKey();
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
        }
        else
        {
            Plugin.Log.Warning("Could not find current player");
        }

        plugin.IsRequestingApi = false;
    }
}
