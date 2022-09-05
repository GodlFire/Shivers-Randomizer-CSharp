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
            this.Left = xCoord;
            this.Top = yCoord;
        }

        public void DispatcherTimer()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        SolidColorBrush brushLime = new SolidColorBrush(Colors.Lime);
        SolidColorBrush brushTransparent = new SolidColorBrush(Colors.Transparent);
        void timer_Tick(object sender, EventArgs e)
        {
            this.Left = xCoord;
            this.Top = yCoord;

            //If window is minimized how the text
            if (isMinimized)
            {
                labelOverlay.Foreground = brushTransparent;
            }
            else
            {
                labelOverlay.Foreground = brushLime;
            }

            if (seed == 0)
            {
                labelOverlay.Content = "Not yet randomized";
            }
        }







        public void SetInfo(int seedNumber, bool SetSeed, bool Vanilla, bool IncludeAsh, bool IncludeLightning, bool EarlyBeth, bool ExtraLocations, bool ExcludeLyre, bool EarlyLightning, bool RedDoor, bool FullPots, bool FirstToTheOnlyFive)
        {
            seed = seedNumber;
            flagset = "";

            string infoString = "";
            if (seedNumber != 0){infoString = seedNumber.ToString();}
            if (SetSeed) { infoString = infoString + " Set Seed"; }
            if (Vanilla) 
            { 
                infoString = infoString + " Vanilla"; 
            }
            else
            {
                if (FirstToTheOnlyFive) { infoString = infoString + " FTTOF "; }
                if (IncludeAsh) { flagset = flagset + "A"; }
                if (IncludeLightning) { flagset = flagset + "I"; }
                if (EarlyBeth) { flagset = flagset + "B"; }
                if (ExtraLocations) { flagset = flagset + "O"; }
                if (ExcludeLyre) { flagset = flagset + "Y"; }
                if (EarlyLightning) { flagset = flagset + "G"; }
                if (RedDoor) { flagset = flagset + "R"; }
                if (FullPots) { flagset = flagset + "F"; }
            }
            


            labelOverlay.Content = infoString + " " + flagset + " V2.3";
        }
    }


}
