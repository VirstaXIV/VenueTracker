using System;
using Dalamud.Configuration;
using Microsoft.Extensions.Logging;

namespace VenueTracker.Data.Config;

[Serializable]
public class VSyncConfig : IVSyncConfiguration, IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool IsConfigWindowMovable { get; set; } = true;
    
    public bool SoundAlerts { get; set; } = false;
    public float SoundVolume { get; set; } = 1;
    public bool SortFriendsToTop { get; set; } = true;
    public bool SortCurrentVisitorsTop { get; set; } = true;

    public string EndpointUrl { get; set; } = "";
    public string ServerKey { get; set; } = "";

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
