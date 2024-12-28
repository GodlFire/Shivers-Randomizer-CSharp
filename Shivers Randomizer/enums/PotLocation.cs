using System.Runtime.Serialization;
namespace Shivers_Randomizer.enums;

internal enum PotLocation
{
    [EnumMember(Value = "Desk Drawer")] DESK_DRAWER,
    [EnumMember(Value = "Workshop Drawers")] WORKSHOP_DRAWERS,
    [EnumMember(Value = "Library Cabinet")] LIBRARY_CABINET,
    [EnumMember(Value = "Library Statue")] LIBRARY_STATUE,
    SLIDE,
    [EnumMember(Value = "Transforming Mask")] TRANSFORMING_MASK,
    [EnumMember(Value = "Eagles Nest")] EAGLES_NEST,
    OCEAN,
    [EnumMember(Value = "Tar River")] TAR_RIVER,
    THEATER,
    GREENHOUSE,
    EGYPT,
    [EnumMember(Value = "Chinese Solitaire")] CHINESE_SOLITAIRE,
    [EnumMember(Value = "Shaman Hut")] SHAMAN_HUT,
    LYRE,
    SKELETON,
    [EnumMember(Value = "Anansi Music Box")] ANANSI_MUSIC_BOX,
    [EnumMember(Value = "Janitor Closet")] JANITOR_CLOSET,
    UFO,
    ALCHEMY,
    [EnumMember(Value = "Skull Bridge")] SKULL_BRIDGE,
    GALLOWS,
    [EnumMember(Value = "Clock Tower")] CLOCK_TOWER,
}
