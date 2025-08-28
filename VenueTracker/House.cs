namespace VenueTracker;

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
    public string WorldName => Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>()?.GetRow(WorldId).Name.ToString() ?? $"World_{WorldId}";
    public string DataCenter => Plugin.DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>()?.GetRow(WorldId).DataCenter.Value.Name.ToString() ?? "";
    
    
    public House()
    {
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
