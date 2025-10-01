using System;
using System.Collections.Concurrent;
using System.Linq;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using VenueTracker.Services.Config;

namespace VenueTracker.Interop;

[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConfigService _configService;
    private readonly IPluginLog _pluginLog;

    public DalamudLoggingProvider(ConfigService configService, IPluginLog pluginLog)
    {
        _configService = configService;
        _pluginLog = pluginLog;
    }

    public ILogger CreateLogger(string categoryName)
    {
        string catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        if (catName.Length > 15)
        {
            catName = string.Join("", catName.Take(6)) + "..." + string.Join("", catName.TakeLast(6));
        }
        else
        {
            catName = string.Join("", Enumerable.Range(0, 15 - catName.Length).Select(_ => " ")) + catName;
        }

        return _loggers.GetOrAdd(catName, name => new DalamudLogger(name, _configService, _pluginLog));
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}
