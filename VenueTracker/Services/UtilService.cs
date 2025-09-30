using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Data;
using VenueTracker.Services.Mediator;

namespace VenueTracker.Services;

public class UtilService : IHostedService, IMediatorSubscriber
{
    private readonly ILogger<UtilService> _logger;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IChatGui _chatGui;
    
    public UtilService(ILogger<UtilService> logger, IClientState clientState, IFramework framework, IChatGui chatGui)
    {
        _logger = logger;
        _clientState = clientState;
        _framework = framework;
        _chatGui = chatGui;
    }
    
    public VSyncMediator Mediator { get; }
    
    public void EnsureIsOnFramework()
    {
        if (!_framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Can only be run on Framework");
    }
    
    public IPlayerCharacter GetPlayerCharacter()
    {
        EnsureIsOnFramework();
        return _clientState.LocalPlayer!;
    }
    
    public void ChatPlayerLink(Player player, string? message = null)
    {

        var messageBuilder = new SeStringBuilder();
        messageBuilder.Add(new PlayerPayload(player.Name, player.HomeWorld));
        if (message != null)
        {
            messageBuilder.AddText(message);
        }

        var entry = new XivChatEntry() { Message = messageBuilder.Build() };
        _chatGui.Print(entry);
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Mediator.UnsubscribeAll(this);
        return Task.CompletedTask;
    }
}