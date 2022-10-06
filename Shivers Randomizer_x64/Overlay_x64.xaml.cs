using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Shivers_Randomizer_x64
{
    /// <summary>
    /// Interaction logic for Overlay_x64.xaml
    /// </summary>
    public partial class Overlay_x64 : Window
    {
        public static int xCoord = 600;
        public static int yCoord = 600;
        public static bool isMinimized = false;
        public static int seed = 0;
        public static string flagset = "";

        public Overlay_x64()
        {
            InitializeComponent();
            DispatcherTimer();
            Left = xCoord;
            Top = yCoord;
        }

        public void DispatcherTimer()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private readonly SolidColorBrush brushLime = new SolidColorBrush(Colors.Lime);
        private readonly SolidColorBrush brushTransparent = new SolidColorBrush(Colors.Transparent);

        private void Timer_Tick(object sender, EventArgs e)
        {
            Left = xCoord;
            Top = yCoord;

            //If window is minimized how the text
            labelOverlay.Foreground = isMinimized ? brushTransparent : brushLime;

            if (seed == 0)
            {
                labelOverlay.Content = "Not yet randomized";
            }
        }

        public void SetInfo(int seedNumber, bool SetSeed, bool Vanilla, bool IncludeAsh, bool IncludeLightning, bool EarlyBeth, bool ExtraLocations, bool ExcludeLyre,
            bool EarlyLightning, bool RedDoor, bool FullPots, bool FirstToTheOnlyFive, bool RoomShuffle)
        {
            seed = seedNumber;
            flagset = "";

            string infoString = "";
            if (seedNumber != 0) { infoString = seedNumber.ToString(); }
            if (SetSeed) { infoString += " Set Seed"; }
            if (Vanilla)
            {
                infoString += " Vanilla";
            }
            else
            {
                if (FirstToTheOnlyFive) { infoString += " FTTOF "; }
                if (IncludeAsh) { flagset += "A"; }
                if (IncludeLightning) { flagset += "I"; }
                if (EarlyBeth) { flagset += "B"; }
                if (ExtraLocations) { flagset += "O"; }
                if (ExcludeLyre) { flagset += "Y"; }
                if (EarlyLightning) { flagset += "G"; }
                if (RedDoor) { flagset += "R"; }
                if (FullPots) { flagset += "F"; }
                if (RoomShuffle) { flagset += "R"; }
            }

            labelOverlay.Content = infoString + " " + flagset + " V2.4";
        }
    }
}
