using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services.Mediator;

namespace VenueTracker.Api;

public class Request(ILogger<Request> logger, PluginState pluginState, VSyncMediator mediator) : IHostedService, IMediatorSubscriber
{
    private static HttpClient _client = new();

    public VSyncMediator Mediator { get; } = mediator;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task<string?> GetAsync(string url, bool authenticated = true)
    {
        _client = new HttpClient();
        if (authenticated)
        {
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + pluginState.ServerToken);
        }
        
        try
        {
            using HttpResponseMessage response = await _client.GetAsync(new Uri(url));
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        } catch {
            logger.LogWarning("Failed to get " + url);
        }
        
        return null;
    }

    public async Task<string?> PostAsync(string url, string content, bool authenticated = true)
    {
        _client = new HttpClient();
        if (authenticated)
        {
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + pluginState.ServerToken);
        }
        
        StringContent stringContent = new(content, Encoding.UTF8, "application/json");

        try
        {
            using HttpResponseMessage response = await _client.PostAsync(new Uri(url), stringContent);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        } catch {
            logger.LogWarning("Failed to post to " + url);
        }
        
        return null;
    }
}
