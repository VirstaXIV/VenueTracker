using System;
using Dalamud.Configuration;
using Microsoft.Extensions.Logging;

namespace VenueTracker.Data.Config;

[Serializable]
public class VSyncConfig : IVSyncConfiguration
{
    public int Version { get; set; } = 1;
    public bool IsConfigWindowMovable { get; set; } = true;
    
    public bool SoundAlerts { get; set; } = false;
    public float SoundVolume { get; set; } = 1;
    public bool SortFriendsToTop { get; set; } = true;
    public bool SortCurrentVisitorsTop { get; set; } = true;

    public string EndpointUrl { get; set; } = "";
    public string ServerKey { get; set; } = "";
    
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
