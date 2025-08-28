using Dalamud.Configuration;
using System;

namespace VenueTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    
    public bool SoundAlerts { get; set; } = false;
    public float SoundVolume { get; set; } = 1;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
