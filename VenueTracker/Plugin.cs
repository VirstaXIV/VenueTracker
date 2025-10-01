using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System;
using System.Threading.Tasks;
using VenueTracker.Api;
using VenueTracker.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data.Config;
using VenueTracker.Interop;
using VenueTracker.Services;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Components;
using VenueTracker.UI.Windows;

namespace VenueTracker;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;
    public static Plugin Self;
    public Action<IFramework>? RealOnFrameworkUpdate { get; set; }
    public void OnFrameworkUpdate(IFramework framework)
    {
        RealOnFrameworkUpdate?.Invoke(framework);
    }

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IDataManager gameData,
        IFramework framework, IObjectTable objectTable, IClientState clientState, ICondition condition, IChatGui chatGui,
        IGameGui gameGui, IDtrBar dtrBar, IToastGui toastGui, IPluginLog pluginLog, ITargetManager targetManager, INotificationManager notificationManager,
        ITextureProvider textureProvider, IContextMenu contextMenu, IGameInteropProvider gameInteropProvider,
        INamePlateGui namePlateGui, IGameConfig gameConfig, IPartyList partyList)
    {
        Plugin.Self = this;
        _host = new HostBuilder()
                .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
                .ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddDalamudLogging(pluginLog);
                    lb.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(new WindowSystem("VenueSync"));
                    
                    // dalamud services
                    collection.AddSingleton(_ => pluginInterface);
                    collection.AddSingleton(_ => pluginInterface.UiBuilder);
                    collection.AddSingleton(_ => commandManager);
                    collection.AddSingleton(_ => gameData);
                    collection.AddSingleton(_ => framework);
                    collection.AddSingleton(_ => objectTable);
                    collection.AddSingleton(_ => clientState);
                    collection.AddSingleton(_ => condition);
                    collection.AddSingleton(_ => gameGui);
                    collection.AddSingleton(_ => chatGui);
                    collection.AddSingleton(_ => dtrBar);
                    collection.AddSingleton(_ => toastGui);
                    collection.AddSingleton(_ => pluginLog);
                    collection.AddSingleton(_ => targetManager);
                    collection.AddSingleton(_ => notificationManager);
                    collection.AddSingleton(_ => textureProvider);
                    collection.AddSingleton(_ => contextMenu);
                    collection.AddSingleton(_ => gameInteropProvider);
                    collection.AddSingleton(_ => namePlateGui);
                    collection.AddSingleton(_ => gameConfig);
                    collection.AddSingleton(_ => partyList);
                    
                    // VenueSync Services
                    collection.AddSingleton<VSyncMediator>();
                    collection.AddSingleton<Request>();
                    collection.AddSingleton<DoorbellService>();
                    collection.AddSingleton<HookService>();
                    collection.AddSingleton<ApiService>();
                    collection.AddSingleton<PluginState>();
                    collection.AddSingleton<GuestsListWidget>();
                    collection.AddSingleton<GuestList>();
                    collection.AddSingleton<UtilService>();
                    
                    collection.AddSingleton((s) => new ConfigService(pluginInterface.ConfigDirectory.FullName));
                    collection.AddSingleton<IConfigService<IVSyncConfiguration>>(s => s.GetRequiredService<ConfigService>());
                    collection.AddSingleton<ConfigurationSaveService>();
                    
                    // scoped services
                    collection.AddScoped<WindowMediatorSubscriberBase, ConfigWindow>();
                    collection.AddScoped<CommandManagerService>();
                    
                    collection.AddHostedService(p => p.GetRequiredService<ConfigurationSaveService>());
                    collection.AddHostedService(p => p.GetRequiredService<UtilService>());
                    collection.AddHostedService(p => p.GetRequiredService<VSyncMediator>());
                    collection.AddHostedService(p => p.GetRequiredService<PluginService>());
                })
                .Build();
            
        _ = Task.Run(async () => {
            try
            {
                await _host.StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                pluginLog.Error(e, "HostBuilder startup exception");
            }
        }).ConfigureAwait(false);
    }
    
    public void Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}
