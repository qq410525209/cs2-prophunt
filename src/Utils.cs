using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

static public class Utils
{
    static public void PrintToChat(CCSPlayerController player, string message)
    {
        player.PrintToChat(Plugin.Instance.Config.Prefix + $" {ChatColors.Grey}" + message);
    }

    static public void PrintToChatAll(string message)
    {
        Server.PrintToChatAll(Plugin.Instance.Config.Prefix + $" {ChatColors.Grey}" + message);
    }

    static public float CalculateDistance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    static public Vector Offset(Vector a, Vector b)
    {
        return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    static public class Spawns
    {
        static public Vector[] Inferno = {
            new Vector(940, 1404, 157),
            new Vector(940, 1364, 157),
            new Vector(940, 1334, 157),
            new Vector(940, 1304, 157),
            new Vector(1000, 2915, 192),
            new Vector(474, -354, 132),
            new Vector(504, -354, 132),
            new Vector(534, -354, 132),
            new Vector(564, -354, 132),
            new Vector(1850, 871, 192),
            new Vector(1820, 871, 192),
            new Vector(1710, 871, 192),
        };
        static public Vector[] Mirage = {
            new Vector(1766, 660, -160), // oob t spawn
            new Vector(1736, 660, -160),
            new Vector(1706, 660, -160),
            new Vector(1676, 660, -160),
            new Vector(806, -2244, 232), // Palace A site
            new Vector(776, -2244, 232),
            new Vector(746, -2244, 232),
            new Vector(1007, -957, 64), // OOB t spawn but cool
            new Vector(30, -994, -23), // 2 trees at mid
            new Vector(-1990, -905, 8), // i like oob
        };

        static public Vector[] Office = {
            new Vector(-73,-1268,-180),
            new Vector(-43,-1268,-180),
            new Vector(-33,-1268,-180),
            new Vector(1180,-511,-120),
            new Vector(1180,-411,-120),
            new Vector(-2052,-1430,-250),
            new Vector(-583,420,-323),
            new Vector(-1237,-2910,-355),
            new Vector(-1207,-2910,-355),
            new Vector(-1167,-2910,-355),
        };
    }
}