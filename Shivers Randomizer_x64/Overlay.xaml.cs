using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for Overlay.xaml
/// </summary>
public partial class Overlay : Window
{
    public readonly App app;
    public string flagset = "";

    public Overlay(App app)
    {
        InitializeComponent();
        this.app = app;
    }

    public readonly SolidColorBrush brushLime = new(Colors.Lime);
    public readonly SolidColorBrush brushTransparent = new(Colors.Transparent);

    public void SetInfo()
    {
        string infoString = "";
        if (app.Seed != 0) { infoString = app.Seed.ToString(); }
        if (app.setSeedUsed) { infoString += " Set Seed"; }
        if (app.settingsVanilla)
        {
            flagset = "";
            infoString += " Vanilla";
        }
        else
        {
            flagset = " ";
            if (app.settingsFirstToTheOnlyFive) { infoString += " FTTOF"; }
            if (app.settingsIncludeAsh) { flagset += "A"; }
            if (app.settingsIncludeLightning) { flagset += "I"; }
            if (app.settingsEarlyBeth) { flagset += "B"; }
            if (app.settingsExtraLocations) { flagset += "O"; }
            if (app.settingsExcludeLyre) { flagset += "Y"; }
            if (app.settingsEarlyLightning) { flagset += "G"; }
            if (app.settingsRedDoor) { flagset += "D"; }
            if (app.settingsFullPots) { flagset += "F"; }
            if (app.settingsOnly4x4Elevators) { flagset += "4"; }
            if (app.settingsElevatorsStaySolved) { flagset += "S"; }
            if (flagset == " ") { flagset = ""; }
        }

        if (app.settingsRoomShuffle)
        {
            if (flagset == "")
            {
                flagset += " R";
            }
            else
            {
                flagset += "R";
            }

            if (app.settingsIncludeElevators) { flagset += "E"; }
        }
        if (app.settingsMultiplayer) { infoString += " Multiplayer"; }

        labelOverlay.Content = infoString + flagset + " v" +  Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
    }
}
