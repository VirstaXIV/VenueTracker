using VenueTracker.Data.Config;

namespace VenueTracker.Services.Config
{
    public class ConfigService : ConfigurationServiceBase<VSyncConfig>
    {
        public const string ConfigName = "config.json";

        public ConfigService(string configDir) : base(configDir)
        {
        }

        public override string ConfigurationName => ConfigName;
    }
}
