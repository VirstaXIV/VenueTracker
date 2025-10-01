using System;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;
using VenueTracker.Services.Events;

namespace VenueTracker.Services.Mediator;
public record UiToggleMessage(Type UiType) : MessageBase;
public record LaunchTaskMessage : MessageBase;
public record LoginMessage : MessageBase;
public record LogoutMessage : MessageBase;
public record PriorityFrameworkUpdateMessage : SameThreadMessage;
public record FrameworkUpdateMessage : SameThreadMessage;
public record DelayedFrameworkUpdateMessage : SameThreadMessage;
public record EventMessage(Event Event) : MessageBase;

