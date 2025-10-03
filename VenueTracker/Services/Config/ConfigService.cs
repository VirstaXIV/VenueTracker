using VenueTracker.Data.Config;

namespace VenueTracker.Services.Config
{
    public class ConfigService(string configDir) : ConfigurationServiceBase<VSyncConfig>(configDir)
    {
        private const string configName = "config.json";
        public override string ConfigurationName => configName;
    }
}
