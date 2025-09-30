namespace VenueTracker.Services.Mediator;

public abstract record MessageBase
{
    public virtual bool KeepThreadContext => false;
    public virtual string? SubscriberKey => null;
}

public record SameThreadMessage : MessageBase
{
    public override bool KeepThreadContext => true;
}

public record KeyedMessage(string MessageKey, bool SameThread = false) : MessageBase
{
    public override string? SubscriberKey => MessageKey;
    public override bool KeepThreadContext => SameThread;
}
