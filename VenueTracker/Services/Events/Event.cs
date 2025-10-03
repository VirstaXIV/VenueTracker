using System;
using VenueTracker.Data;

namespace VenueTracker.Services.Events;

public record Event
{
    public DateTime EventTime { get; }
    public string UID { get; }
    public string Character { get; }
    public string EventSource { get; }
    public EventSeverity EventSeverity { get; }
    public string Message { get; }

    public Event(string? Character, UserData userData, string EventSource, EventSeverity EventSeverity, string Message)
    {
        EventTime = DateTime.Now;
        this.UID = userData.AliasOrUID;
        this.Character = Character ?? string.Empty;
        this.EventSource = EventSource;
        this.EventSeverity = EventSeverity;
        this.Message = Message;
    }

    public Event(UserData userData, string EventSource, EventSeverity EventSeverity, string Message) : this(null, userData, EventSource, EventSeverity, Message)
    {
    }

    public Event(string EventSource, EventSeverity EventSeverity, string Message)
        : this(new UserData(string.Empty), EventSource, EventSeverity, Message)
    {
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(UID))
            return $"{EventTime:HH:mm:ss.fff}\t[{EventSource}]{{{(int)EventSeverity}}}\t{Message}";
        return string.IsNullOrEmpty(Character) ? $"{EventTime:HH:mm:ss.fff}\t[{EventSource}]{{{(int)EventSeverity}}}\t<{UID}> {Message}" : $"{EventTime:HH:mm:ss.fff}\t[{EventSource}]{{{(int)EventSeverity}}}\t<{UID}\\{Character}> {Message}";
    }
}
