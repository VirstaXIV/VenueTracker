using System;
using System.Collections.Generic;
using System.Linq;
using VenueTracker.Utils;

namespace VenueTracker.Data;

[Serializable]
public class GuestList
{
    private static readonly string OutputFile = "guests.json";
    public Dictionary<string, Player> Guests { get; set; } = new();
    public long HouseId { get; set; } = 0;
    public House House { get; set; } = new();
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    public GuestList()
    {
    }
    
    public GuestList(long id, House house)
    {
        HouseId = id;
        House = new House(house);
    }
    
    public GuestList(GuestList list) 
    {
        Guests = list.Guests;
        HouseId = list.HouseId;
        House = list.House;
        StartTime = list.StartTime;
    }
    
    private string GetFileName()
    {
        return HouseId + "-" + OutputFile;
    }

    public void Save()
    {
        FileStore.SaveClassToFileInPluginDir(GetFileName(), GetType(), this);
    }
    
    public void Load()
    {
        if (HouseId == 0) return;

        // Don't attempt to load if there is no file 
        var fileInfo = FileStore.GetFileInfo(GetFileName());
        if (!fileInfo.Exists) return;

        GuestList loadedData = FileStore.LoadFile<GuestList>(GetFileName(), this);
        Guests = loadedData.Guests;
        HouseId = loadedData.HouseId;
        StartTime = loadedData.StartTime;
        // Don't replace venue if the incoming one is blank
        if (loadedData.House.Name.Length != 0)
            House = loadedData.House;
    }
    
    public void RemoveUsersNotInHouse() 
    {
        var matches = Guests.Where(kvp => kvp.Value.InHouse);
        Guests = matches.ToDictionary();
    }

    public void ClearList()
    {
        Guests = new Dictionary<string, Player>();
    }
}
