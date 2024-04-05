using System;

namespace Shivers_Randomizer;

internal static class APItemID
{
    public const int BaseItemID = 27000;
    public static readonly int AP_POTS_COUNT = Enum.GetValues<POTS>().Length;

    internal enum POTS
    {
        WATER_BOTTOM = BaseItemID,
        WAX_BOTTOM,
        ASH_BOTTOM,
        OIL_BOTTOM,
        CLOTH_BOTTOM,
        WOOD_BOTTOM,
        CRYSTAL_BOTTOM,
        LIGHTNING_BOTTOM,
        SAND_BOTTOM,
        METAL_BOTTOM,
        WATER_TOP,
        WAX_TOP,
        ASH_TOP,
        OIL_TOP,
        CLOTH_TOP,
        WOOD_TOP,
        CRYSTAL_TOP,
        LIGHTNING_TOP,
        SAND_TOP,
        METAL_TOP
    }
    internal enum KEYS
    {
        OFFICE_ELEVATOR = BaseItemID + 20,
        BEDROOM_ELEVATOR,
        THREE_FLOOR_ELEVATOR,
        WORKSHOP,
        OFFICE,
        PREHISTORIC,
        GREENHOUSE,
        OCEAN,
        PROJECTOR,
        GENERATOR,
        EGYPT,
        LIBRARY,
        SHAMAN,
        UFO,
        TORTURE,
        PUZZLE,
        BEDROOM,
        UNDERGROUND_LAKE_ROOM,
        JANITOR_CLOSET,
        FRONT_DOOR
    }
    internal enum ABILITIES
    {
        CRAWLING = BaseItemID + 50,
    }

    internal enum FILLER
    {
        EASIER_LYRE = BaseItemID + 91,
        WATER_LOBBY,
        WAX_LIBRARY,
        WAX_ANANSI,
        WAX_SHAMAN,
        ASH_OFFICE,
        ASH_BURIAL,
        OIL_PREHISTORIC,
        CLOTH_EGYPT,
        CLOTH_BURIAL,
        WOOD_WORKSHOP,
        WOOD_BLUE_MAZE,
        WOOD_PEGASUS,
        WOOD_GODS,
        CRYSTAL_LOBBY,
        CRYSTAL_OCEAN,
        SAND_GREENHOUSE,
        SAND_OCEAN,
        METAL_PROJECTOR,
        METAL_BEDROOM,
        METAL_PREHISTORIC,
        HEAL
    }
}