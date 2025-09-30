using System.Collections.Generic;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Microsoft.Extensions.Logging;
using VenueTracker.Services.Config;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Windows;

namespace VenueTracker.Services;

public sealed class UiService : DisposableMediatorSubscriberBase
{
    private readonly List<WindowMediatorSubscriberBase> _createdWindows = [];
    private readonly IUiBuilder _uiBuilder;
    private readonly ILogger<UiService> _logger;
    private readonly ConfigService _configService;
    private readonly WindowSystem _windowSystem;

    public UiService(
        ILogger<UiService> logger, IUiBuilder uiBuilder, ConfigService configService,
        WindowSystem windowSystem,
        IEnumerable<WindowMediatorSubscriberBase> windows, VSyncMediator vSyncMediator) : base(logger, vSyncMediator)
    {
        _logger = logger;
        _logger.LogTrace("Creating {type}", GetType().Name);
        _windowSystem = windowSystem;
        _uiBuilder = uiBuilder;
        _configService = configService;
        
        _uiBuilder.DisableGposeUiHide = true;
        _uiBuilder.Draw += Draw;
        _uiBuilder.OpenConfigUi += ToggleUi;
        
        foreach (var window in windows)
        {
            _windowSystem.AddWindow(window);
        }
    }
    
    public void ToggleUi()
    {
        Mediator.Publish(new UiToggleMessage(typeof(ConfigWindow)));
    }
    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _logger.LogTrace("Disposing {type}", GetType().Name);

        _windowSystem.RemoveAllWindows();

        foreach (var window in _createdWindows)
        {
            window.Dispose();
        }

        _uiBuilder.Draw -= Draw;
        _uiBuilder.OpenConfigUi -= ToggleUi;
    }

    private void Draw()
    {
        _windowSystem.Draw();
    }
}
