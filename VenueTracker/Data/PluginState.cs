namespace VenueTracker.Data;

public class PluginState
{
    public House CurrentHouse = new();
    public string? CurrentWorld { get; set; } = "";
    public bool PlayerInHouse { get; set; } = false;
    public int PlayersInHouse { get; set; } = 0;
    public string PlayerName { get; set; } = "";
    public string PlayerWorld { get; set; } = "";
    public ushort Territory { get; set; } = 0;
    public string ServerToken { get; set; } = "";
}
