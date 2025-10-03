using System.Linq;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using Sheet = Lumina.Excel.Sheets;


namespace VenueTracker.Utils;

public class TerritoryUtils(ILogger<TerritoryUtils> logger, IDataManager gameData)
{
    private readonly ILogger<TerritoryUtils> _logger = logger;

    // Mist locations 
    public const ushort MIST_SMALL = 282;
    public const ushort MIST_MEDIUM = 283;
    public const ushort MIST_LARGE = 284;
    public const ushort MIST_CHAMBER = 384;
    public const ushort MIST_APARTMENT = 608;

    // The Lavender Beds locations 
    public const ushort LAVENDER_SMALL = 342;
    public const ushort LAVENDER_MEDIUM = 343;
    public const ushort LAVENDER_LARGE = 344;
    public const ushort LAVENDER_CHAMBER = 385;
    public const ushort LAVENDER_APARTMENT = 609;

    // The Goblet
    public const ushort GOBLET_SMALL = 345;
    public const ushort GOBLET_MEDIUM = 346;
    public const ushort GOBLET_LARGE = 347;
    public const ushort GOBLET_LARGE_2 = 1251;
    public const ushort GOBLET_CHAMBER = 386;
    public const ushort GOBLET_APARTMENT = 610;

    // Shirogane 
    public const ushort SHIROGANE_SMALL = 649;
    public const ushort SHIROGANE_MEDIUM = 650;
    public const ushort SHIROGANE_LARGE = 651;
    public const ushort SHIROGANE_CHAMBER = 652;
    public const ushort SHIROGANE_APARTMENT = 655;

    // Empyreum 
    public const ushort EMPYREUM_SMALL = 980;
    public const ushort EMPYREUM_MEDIUM = 981;
    public const ushort EMPYREUM_LARGE = 982;
    public const ushort EMPYREUM_CHAMBER = 983;
    public const ushort EMPYREUM_APARTMENT = 999;

    private static readonly ushort[] HouseTerritoryIds = {
      MIST_SMALL, MIST_MEDIUM, MIST_LARGE, MIST_CHAMBER, MIST_APARTMENT,
      LAVENDER_SMALL, LAVENDER_MEDIUM, LAVENDER_LARGE, LAVENDER_CHAMBER, LAVENDER_APARTMENT, 
      GOBLET_SMALL, GOBLET_MEDIUM, GOBLET_LARGE, GOBLET_LARGE_2, GOBLET_CHAMBER, GOBLET_APARTMENT,
      SHIROGANE_SMALL, SHIROGANE_MEDIUM, SHIROGANE_LARGE, SHIROGANE_CHAMBER, SHIROGANE_APARTMENT,
      EMPYREUM_SMALL, EMPYREUM_MEDIUM, EMPYREUM_LARGE, EMPYREUM_CHAMBER, EMPYREUM_APARTMENT, 
    };

    private readonly ushort[] ChambrerTerritoryIds = {
      MIST_CHAMBER, LAVENDER_CHAMBER, GOBLET_CHAMBER, SHIROGANE_CHAMBER, EMPYREUM_CHAMBER, 
    };

    private readonly ushort[] PlotTerritoryIds = {
      MIST_SMALL, MIST_MEDIUM, MIST_LARGE,
      LAVENDER_SMALL, LAVENDER_MEDIUM, LAVENDER_LARGE,
      GOBLET_SMALL, GOBLET_MEDIUM, GOBLET_LARGE, GOBLET_LARGE_2,
      SHIROGANE_SMALL, SHIROGANE_MEDIUM, SHIROGANE_LARGE,
      EMPYREUM_SMALL, EMPYREUM_MEDIUM, EMPYREUM_LARGE, 
    };

    private readonly ushort[] SmallHouseTypes = {
      MIST_SMALL, LAVENDER_SMALL, GOBLET_SMALL, SHIROGANE_SMALL, EMPYREUM_SMALL,
    };

    private readonly ushort[] MediumHouseTypes = {
      MIST_MEDIUM, LAVENDER_MEDIUM, GOBLET_MEDIUM, SHIROGANE_MEDIUM, EMPYREUM_MEDIUM
    };

    private readonly ushort[] LargeHouseTypes = {
      MIST_LARGE, LAVENDER_LARGE, GOBLET_LARGE, GOBLET_LARGE_2, SHIROGANE_LARGE, EMPYREUM_LARGE
    };

    private readonly ushort[] ChamberTypes = {
      MIST_CHAMBER, LAVENDER_CHAMBER, GOBLET_CHAMBER, SHIROGANE_CHAMBER, EMPYREUM_CHAMBER
    };

    private readonly ushort[] AppartmentTypes = {
      MIST_APARTMENT, LAVENDER_APARTMENT, GOBLET_APARTMENT, SHIROGANE_APARTMENT, EMPYREUM_APARTMENT
    };

    private readonly ushort[] MistHouses = {
      MIST_SMALL, MIST_MEDIUM, MIST_LARGE, MIST_CHAMBER, MIST_APARTMENT
    };

    private readonly ushort[] LavenderHouses = {
      LAVENDER_SMALL, LAVENDER_MEDIUM, LAVENDER_LARGE, LAVENDER_CHAMBER, LAVENDER_APARTMENT
    };

    private readonly ushort[] GobletHouses = {
      GOBLET_SMALL, GOBLET_MEDIUM, GOBLET_LARGE, GOBLET_LARGE_2, GOBLET_CHAMBER, GOBLET_APARTMENT
    };

    private readonly ushort[] ShiroganeHouses = {
      SHIROGANE_SMALL, SHIROGANE_MEDIUM, SHIROGANE_LARGE, SHIROGANE_CHAMBER, SHIROGANE_APARTMENT
    };

    private readonly ushort[] EmpyreumHouses = {
      EMPYREUM_SMALL, EMPYREUM_MEDIUM, EMPYREUM_LARGE, EMPYREUM_CHAMBER, EMPYREUM_APARTMENT
    };

    private const uint SmallHouseIcon = 60751;
    private const uint MediumHouseIcon = 60752;
    private const uint LargeHouseIcon = 60753;
    private const uint ApartmentHouseIcon = 60789;

    // Returns true if sent territory id is a house 
    public bool IsHouse(ushort territory)
    {
      return HouseTerritoryIds.Contains(territory);
    }

    public string GetHouseType(ushort territory)
    {
      if (SmallHouseTypes.Contains(territory)) return "Small House";
      if (MediumHouseTypes.Contains(territory)) return "Medium House";
      if (LargeHouseTypes.Contains(territory)) return "Large House";
      if (ChamberTypes.Contains(territory)) return "Chamber";
      if (AppartmentTypes.Contains(territory)) return "Apartment";
      return "[unknown house type]";
    }

    public string GetHouseDistrict(ushort territory)
    {
      if (MistHouses.Contains(territory)) return "Mist";
      if (LavenderHouses.Contains(territory)) return "The Lavender Beds";
      if (GobletHouses.Contains(territory)) return "The Goblet";
      if (ShiroganeHouses.Contains(territory)) return "Shirogane";
      if (EmpyreumHouses.Contains(territory)) return "Empyreum";
      return "[unknown district]";
    }

    public bool IsChamber(ushort territory)
    {
      return ChambrerTerritoryIds.Contains(territory);
    }

    public bool IsPlotType(ushort territory)
    {
      return PlotTerritoryIds.Contains(territory);
    }

    public uint GetHouseIcon(ushort territory)
    {
      if (SmallHouseTypes.Contains(territory)) return SmallHouseIcon;
      if (MediumHouseTypes.Contains(territory)) return MediumHouseIcon;
      if (LargeHouseTypes.Contains(territory) || ChamberTypes.Contains(territory)) return LargeHouseIcon;
      return AppartmentTypes.Contains(territory) ? ApartmentHouseIcon : (uint)0;
    }

    public string GetDistrict (long houseId) {
      uint territoryId = (uint)((houseId >> 32) & 0xFFFF);
      var district = gameData.GetExcelSheet<Sheet.TerritoryType>().GetRow(territoryId).PlaceNameZone.RowId;

      return district switch
      {
          502 => "Mist",
          505 => "Goblet",
          507 => "Lavender Beds",
          512 => "Empyreum",
          513 => "Shirogane",
          _ => ""
      };
    }
}
