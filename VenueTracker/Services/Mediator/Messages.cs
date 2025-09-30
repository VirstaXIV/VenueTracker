using System;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;

namespace VenueTracker.Services.Mediator;
public record UiToggleMessage(Type UiType) : MessageBase;

