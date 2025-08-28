namespace VenueTracker;

public class PluginState
{
    public House CurrentHouse = new();
    public bool PlayerInHouse { get; set; } = false;
    public int PlayersInHouse { get; set; } = 0;
    public string PlayerName { get; set; } = "";
    public ushort Territory { get; set; } = 0;
}
