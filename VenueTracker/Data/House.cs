namespace VenueTracker.Data;

public class House
{
    public long HouseId {get; set;} = 0;
    public int Plot {get; set;} = 0;
    public int Ward {get; set;} = 0;
    public int Room {get; set;} = 0;
    public string Name {get; set;} = "";
    public string District {get; set;} = "";
    public uint WorldId {get; set;} = 0;
    public ushort Type {get; set;} = 0;
    public string Notes {get; set;} = "";
    public string WorldName {get; set;} = "";
    public string DataCenter {get; set;} = "";
    
    public House() {
    }
    
    public House(House club) {
        HouseId = club.HouseId;
        Plot = club.Plot;
        Ward = club.Ward;
        Room = club.Room;
        Name = club.Name;
        District = club.District;
        WorldId = club.WorldId;
        Type = club.Type;
        Notes = club.Notes;
    }

}
