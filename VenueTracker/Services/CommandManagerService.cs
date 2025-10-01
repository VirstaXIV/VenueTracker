using System;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using VenueTracker.Services.Mediator;
using VenueTracker.UI.Windows;

namespace VenueTracker.Services;

public class CommandManagerService : IDisposable
{
    private const string _commandName = "/vsync";
    
    private readonly ICommandManager _commandManager;
    private readonly VSyncMediator _mediator;
    
    public CommandManagerService(ICommandManager commandManager, VSyncMediator mediator)
    {
        _commandManager = commandManager;
        _mediator = mediator;
        
        _commandManager.AddHandler(_commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Guests UI"
        });
    }
    
    public void Dispose()
    {
        _commandManager.RemoveHandler(_commandName);
    }
    
    private void OnCommand(string command, string args)
    {
        _mediator.Publish(new UiToggleMessage(typeof(GuestsWindow)));
    }
}
