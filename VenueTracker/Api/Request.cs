using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VenueTracker.Api;

public class Request(Plugin plugin)
{
    private static HttpClient Client = new();
    
    public void Dispose()
    {
        Client.Dispose();
    }

    public async Task<string?> GetAsync(string url, bool authenticated = true)
    {
        Client = new HttpClient();
        if (authenticated)
        {
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + plugin.PluginState.ServerToken);
        }
        
        try
        {
            using HttpResponseMessage response = await Client.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        } catch {
            Plugin.Log.Warning("Failed to get " + url);
        }
        
        return null;
    }

    public async Task<string?> PostAsync(string url, string content, bool authenticated = true)
    {
        Client = new HttpClient();
        if (authenticated)
        {
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + plugin.PluginState.ServerToken);
        }
        
        StringContent stringContent = new(content, Encoding.UTF8, "application/json");

        try
        {
            using HttpResponseMessage response = await Client.PostAsync(new Uri(url), stringContent);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        } catch {
            Plugin.Log.Warning("Failed to post to " + url);
        }
        
        return null;
    }
}
