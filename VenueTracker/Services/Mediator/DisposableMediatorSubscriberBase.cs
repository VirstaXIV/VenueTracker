﻿using System;
using Microsoft.Extensions.Logging;

namespace VenueTracker.Services.Mediator;

public abstract class DisposableMediatorSubscriberBase(ILogger logger, VSyncMediator mediator) : MediatorSubscriberBase(logger, mediator), IDisposable
{
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Logger.LogTrace("Disposing {type} ({this})", GetType().Name, this);
        UnsubscribeAll();
    }
}
