using System.Windows;
using System.Windows.Media;

namespace Shivers_Randomizer_x64;

/// <summary>
/// Interaction logic for Overlay_x64.xaml
/// </summary>
public partial class Overlay_x64 : Window
{
    public string flagset = "";

    public Overlay_x64() => InitializeComponent();

    public readonly SolidColorBrush brushLime = new(Colors.Lime);
    public readonly SolidColorBrush brushTransparent = new(Colors.Transparent);

    public void SetInfo(int seedNumber, bool SetSeed, bool Vanilla, bool IncludeAsh, bool IncludeLightning, bool EarlyBeth, bool ExtraLocations, bool ExcludeLyre,
        bool EarlyLightning, bool RedDoor, bool FullPots, bool FirstToTheOnlyFive, bool RoomShuffle, bool Multiplayer)
    {
        string infoString = "";
        if (seedNumber != 0) { infoString = seedNumber.ToString(); }
        if (SetSeed) { infoString += " Set Seed"; }
        if (Vanilla)
        {
            flagset = "";
            infoString += " Vanilla";
        }
        else
        {
            flagset = " ";
            if (FirstToTheOnlyFive) { infoString += " FTTOF"; }
            if (IncludeAsh) { flagset += "A"; }
            if (IncludeLightning) { flagset += "I"; }
            if (EarlyBeth) { flagset += "B"; }
            if (ExtraLocations) { flagset += "O"; }
            if (ExcludeLyre) { flagset += "Y"; }
            if (EarlyLightning) { flagset += "G"; }
            if (RedDoor) { flagset += "R"; }
            if (FullPots) { flagset += "F"; }
            if (RoomShuffle) { flagset += "R"; }
            if (flagset == " ") { flagset = ""; }
        }

        if (Multiplayer) { infoString += " Multiplayer"; }

        labelOverlay.Content = infoString + flagset + " V2.4";
    }
}
