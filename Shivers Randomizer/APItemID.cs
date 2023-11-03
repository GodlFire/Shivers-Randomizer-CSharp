namespace Shivers_Randomizer;

public class APItemID
{
    public const int BaseItemID = 27000;

    internal enum POTS
    {
        WATERBOTTOM = BaseItemID,
        WAXBOTTOM,
        ASHBOTTOM,
        OILBOTTOM,
        CLOTHBOTTOM,
        WOODBOTTOM,
        CRYSTALBOTTOM,
        LIGHTNINGBOTTOM,
        SANDBOTTOM,
        METALBOTTOM,
        WATERTOP,
        WAXTOP,
        ASHTOP,
        OILTOP,
        CLOTHTOP,
        WOODTOP,
        CRYSTALTOP,
        LIGHTNINGTOP,
        SANDTOP,
        METALTOP
    }
    internal enum KEYS
    {
        OFFICEELEVATOR = BaseItemID + 20,
        BEDROOMELEVATOR,
        THREEFLOORELEVATOR,
        WORKSHOP,
        OFFICE,
        PREHISTORIC,
        GREENHOUSE,
        OCEAN,
        PROJECTOR,
        GENERATOR,
        EGYPT,
        LIBRARY,
        TIKI,
        UFO,
        TORTURE,
        PUZZLE,
        BEDROOM,
        UNDERGROUNDLAKEROOM,
        JANITORCLOSET,
        FRONTDOOR
    }
    internal enum ABILITIES
    {
        CRAWLING = BaseItemID + 50,
    }

    internal enum FILLER
    {
        EASIERLYRE = BaseItemID + 91,
        WATERLOBBY,
        WAXLIBRARY,
        WAXANANSI,
        WAXTIKI,
        ASHOFFICE,
        ASHBURIAL,
        OILPREHISTORIC,
        CLOTHEGYPT,
        CLOTHBURIAL,
        WOODWORKSHOP,
        WOODBLUEMAZE,
        WOODPEGASUS,
        WOODGODS,
        CRYSTALLOBBY,
        CRYSTALOCEAN,
        SANDGREENHOUSE,
        SANDOCEAN,
        METALPROJECTOR,
        METALBEDROOM,
        METALPREHISTORIC,
        HEAL
    }
}