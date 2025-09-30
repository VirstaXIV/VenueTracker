using System;
using VenueTracker.Data.Config;

namespace VenueTracker.Services.Config;

public interface IConfigService<out T> : IDisposable where T : IVSyncConfiguration
{
    T Current { get; }
    string ConfigurationName { get; }
    string ConfigurationPath { get; }
    public event EventHandler? ConfigSave;
    void UpdateLastWriteTime();
}
