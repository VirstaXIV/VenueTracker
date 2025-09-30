using System.Numerics;
using VenueTracker.Data;

namespace VenueTracker.UI;

public static class Colors
{
    public static readonly Vector4 Green = new Vector4(0,1,0,1);
    public static readonly Vector4 White = new Vector4(1,1,1,1);
    public static readonly Vector4 HalfWhite = new Vector4(.5f,.5f,.5f,1);

    // Colors for different entry counts 
    public static readonly Vector4 PlayerEntry2 = new Vector4(.92f,.7f,.35f,1);
    public static readonly Vector4 PlayerEntry3 = new Vector4(.97f,.47f,.1f,1);
    public static readonly Vector4 PlayerEntry4 = new Vector4(.89f,0.01f,0,1);

    public static readonly Vector4 PlayerBlue = new Vector4(.25f,0.65f,0.89f,1);

    public static ushort GetChatColor(Player player, bool nameOnly) {
        if (nameOnly) {
            if (player.IsFriend) return 526; // Blue 
        }

        return player.EntryCount switch
        {
            1 => 060,
            2 => 063,
            3 => 500,
            >= 4 => 518,
            _ => 003
        };
    }

    public static Vector4 GetGuestListColor(Player player, bool nameOnly) {
        if (nameOnly) {
            if (player.IsFriend) return PlayerBlue; // Blue 
        }

        return player.EntryCount switch
        {
            2 => PlayerEntry2,
            3 => PlayerEntry3,
            >= 4 => PlayerEntry4,
            _ => White
        };
    }
}
