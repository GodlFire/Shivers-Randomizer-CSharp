using System;
using System.Collections.Generic;

namespace Shivers_Randomizer.utils;

internal static class Constants
{
    public const int POT_BOTTOM_OFFSET = 200;
    public const int POT_TOP_OFFSET = 210;
    public const int POT_FULL_OFFSET = 220;
    public const int ARCHIPELAGO_BASE_ITEM_ID = 27000;
    public const int ARCHIPELAGO_BASE_LOCATION_ID = 27000;
    public static IReadOnlyList<Ixupi> IXUPI = Enum.GetValues<Ixupi>();
    public static IReadOnlyList<IxupiPot> IXUPI_POTS = Enum.GetValues<IxupiPot>();
    public static IReadOnlyList<IxupiPot> OIL_POTS = new List<IxupiPot> { IxupiPot.OIL_BOTTOM, IxupiPot.OIL_TOP, IxupiPot.OIL_FULL };
    public static IReadOnlyList<IxupiPot> CLOTH_POTS = new List<IxupiPot> { IxupiPot.CLOTH_BOTTOM, IxupiPot.CLOTH_TOP, IxupiPot.CLOTH_FULL };
    public static IReadOnlyList<PotLocation> POT_LOCATIONS = Enum.GetValues<PotLocation>();
    public static IReadOnlyList<PotLocation> EXTRA_LOCATIONS = new List<PotLocation> { PotLocation.LIBRARY_CABINET, PotLocation.EAGLE_NEST, PotLocation.SHAMAN_HUT };

    public static readonly IReadOnlyList<int> POT_ROOMS = new List<int>
    {
        6220,  // Desk Drawer
        7112,  // Workshop
        8100,  // Library Cupboard
        8490,  // Library Statue
        9420,  // Slide
        9760,  // Eagle
        11310, // Eagles Nest
        12181, // Ocean
        14080, // Tar River
        16420, // Theater
        19220, // Green House / Plant Room
        20553, // Egypt
        21070, // Chinese Solitaire
        22190, // Tiki Hut
        23550, // Lyre
        24320, // Skeleton
        25050, // Janitor Closet
        29080, // UFO
        30420, // Alchemy
        31310, // Puzzle Room
        32570, // Hanging / Gallows
        35110  // Clock Tower
    };

    public static readonly IReadOnlyList<int> REDRAW_ROOMS = new List<int>
    {
        1162,  // Gear Puzzle Combo lock
        1160,  // Gear Puzzle
        1214,  // Stone Henge Puzzle
        2340,  // Generator Panel
        3500,  // Boat Control Open Water
        3510,  // Boat Control Shore
        3260,  // Water attack cutscene on boat
        931,   // Windelnot Ghost cutscene
        4630,  // Underground Elevator puzzle bottom
        6300,  // Underground Elevator puzzle top
        5010,  // Underground Elevator inside A
        5030,  // Underground Elevator inside B
        4620,  // Underground Elevator outside A
        6290,  // Underground Elevator outside B
        38130, // Office Elevator puzzle bottom
        37360, // Office Elevator puzzle top
        38010, // Office Elevator inside A
        38011, // Office Elevator inside B
        38110, // Office Elevator outside A
        37330, // Office Elevator outside B
        34010, // 3-Floor Elevator Inside
        10100, // 3-Floor Elevator outside Floor 1
        27212, // 3-Floor Elevator outside Floor 2
        33140, // 3-Floor Elevator outside Floor 3
        10101, // 3-Floor Elevator Puzzle Floor 1
        27211, // 3-Floor Elevator Puzzle Floor 2
        33500, // 3-Floor Elevator Puzzle Floor 3
        6280,  // Ash fireplace
        21050, // Ash Burial
        21430, // Cloth Burial
        20700, // Cloth Egypt
        25050, // Cloth Janitor
        9770,  // Crystal Lobby
        12500, // Crystal Ocean
        32500, // Lightning Electric Chair
        39260, // Lightning Generator
        29190, // Lightning UFO
        37291, // Metal bedroom
        11340, // Metal prehistoric
        17090, // Metal projector
        19250, // Sand plants
        12200, // Sand Ocean
        11300, // Tar prehistoric
        14040, // Tar underground
        9700,  // Water fountain
        25060, // Water Janitor Closet
        24360, // Wax Anansi
        8160,  // Wax library
        22100, // Wax tiki
        27081, // Wood blue hallways
        23160, // Wood Gods Room
        24190, // Wood Pegasus room
        7180,  // Wood workshop
        7111,  // Workshop puzzle
        9930,  // Lobby Fountain Spigot
        8430,  // Library Book Puzzle
        9691,  // Theater Door Puzzle
        18250, // Geoffrey Puzzle
        40260, // Clock Tower Chains Puzzle
        932,   // Beth Ghost cutscene
        35170, // Camera surveilence
        35154, // Juke Box
        17180, // Projector Puzzle
        934,   // Theater Movie cutscene
        11350, // Skull Dial prehistoric
        14170, // Skull Dial underground
        24170, // Skull Dial werewolf
        21400, // Skull Dial burial
        20190, // Skull Dial egypt
        23650, // Skull Dial gods
        12600, // Atlantis puzzle
        12410, // Organ puzzle
        12590, // Sirens Song
        13010, // Underground Maze Door Puzzle
        20510, // Column of Ra puzzle A
        20610, // Column of Ra puzzle B
        20311, // Egypt Door Puzzle
        21071, // Chinese Solitair
        22180, // tiki drums puzzle
        23590, // Lyre Puzzle
        23601, // Red Door Puzzle
        27090, // Horse Painting Puzzle
        28050, // Fortune Teller
        933,   // Merrick Ghost Cutscene
        30421, // Alchemy Puzzle
        29045, // UFO Puzzle
        29260, // Planet Alignment Puzzle
        29510, // Planets Aligned Message
        24440, // Anansi Key
        32161, // Guillotine
        32059, // Gallows Puzzle
        32059, // Gallows Puzzle
        32390, // Gallows Lever
        31090, // Mastermind Puzzle
        31270, // Marble Flipper Puzzle
        31330, // Skull Door
        31390, // Slide Wheel
        936    // Slide Cutscene
    };
}
