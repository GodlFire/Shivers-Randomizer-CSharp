using System.Collections.Generic;

namespace Shivers_Randomizer.utils;

public record AddressedValue(int Location, int Value);

public record ArchipelagoDataStorage
(
    Dictionary<string, AddressedValue> SkullDials,
    Dictionary<string, AddressedValue> IxupiDamage,
    int PlayerLocation = 1012,
    bool Jukebox = false,
    bool TarRiverShortcut = false,
    int Health = 100,
    int HealItemsReceived = 0,
    int IxupiCapturedStates = 0
);
