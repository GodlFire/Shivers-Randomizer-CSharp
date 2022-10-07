using Shivers_Randomizer_x64;
using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Shivers_Randomizer
{
    /// <summary>
    /// Interaction logic for MainWindow_x64.xaml
    /// </summary>
    public partial class MainWindow_x64 : Window
    {
        private readonly App app;

        public MainWindow_x64(App app)
        {
            InitializeComponent();
            this.app = app;
        }

        //Display popup for attaching to shivers process
        private void Button_Attach_Click(object sender, RoutedEventArgs e)
        {
            AttachPopup_x64 attachPopup = new AttachPopup_x64(app);
            attachPopup.Show();
        }

        private void Button_Scramble_Click(object sender, RoutedEventArgs e)
        {
            app.settingsVanilla = checkBoxVanilla.IsChecked == true;
            app.settingsIncludeAsh = checkBoxIncludeAsh.IsChecked == true;
            app.settingsIncludeLightning = checkBoxIncludeLightning.IsChecked == true;
            app.settingsEarlyBeth = checkBoxEarlyBeth.IsChecked == true;
            app.settingsExtraLocations = checkBoxExtraLocations.IsChecked == true;
            app.settingsExcludeLyre = checkBoxExcludeLyre.IsChecked == true;
            app.settingsEarlyLightning = checkBoxEarlyLightning.IsChecked == true;
            app.settingsRedDoor = checkBoxRedDoor.IsChecked == true;
            app.settingsFullPots = checkBoxFullPots.IsChecked == true;
            app.settingsFirstToTheOnlyFive = checkBoxFirstToTheOnlyFive.IsChecked == true;
            app.settingsRoomShuffle = checkBoxRoomShuffle.IsChecked == true;
            app.Scramble();
        }

        private void CheckBoxVanilla_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxVanilla.IsChecked == true)
            {
                checkBoxIncludeAsh.IsEnabled = false;
                checkBoxIncludeAsh.IsChecked = false;
                checkBoxIncludeLightning.IsEnabled = false;
                checkBoxIncludeLightning.IsChecked = false;
                checkBoxEarlyBeth.IsEnabled = false;
                checkBoxEarlyBeth.IsChecked = false;
                checkBoxExtraLocations.IsEnabled = false;
                checkBoxExtraLocations.IsChecked = false;
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxExcludeLyre.IsChecked = false;
                checkBoxEarlyLightning.IsEnabled = false;
                checkBoxEarlyLightning.IsChecked = false;
                checkBoxRedDoor.IsEnabled = false;
                checkBoxRedDoor.IsChecked = false;
                checkBoxFullPots.IsEnabled = false;
                checkBoxFullPots.IsChecked = false;
                checkBoxFirstToTheOnlyFive.IsEnabled = false;
                checkBoxFirstToTheOnlyFive.IsChecked = false;
            }
            else
            {
                checkBoxIncludeAsh.IsEnabled = true;
                checkBoxIncludeLightning.IsEnabled = true;
                checkBoxEarlyBeth.IsEnabled = true;
                checkBoxExtraLocations.IsEnabled = true;
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxEarlyLightning.IsEnabled = false;
                checkBoxRedDoor.IsEnabled = true;
                checkBoxFullPots.IsEnabled = true;
                checkBoxFirstToTheOnlyFive.IsEnabled = true;
            }
        }

        private void CheckBoxFullPotsAndExtraLocations_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxExtraLocations.IsChecked == true || checkBoxFullPots.IsChecked == true)
            {
                checkBoxExcludeLyre.IsEnabled = true;
            }
            else
            {
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxExcludeLyre.IsChecked = false;
            }
        }

        private void CheckBoxIncludeLightning_Click(object sender, RoutedEventArgs e)
        {
            CheckLightning();

            //If lightning is not included in scramble no point to have early lightning capture enabled
            if (checkBoxIncludeLightning.IsChecked == false)
            {
                checkBoxEarlyLightning.IsEnabled = false;
                checkBoxEarlyLightning.IsChecked = false;
            }
            else
            {
                checkBoxEarlyLightning.IsEnabled = true;
            }
        }

        private void CheckBoxEarlyLightning_Click(object sender, RoutedEventArgs e)
        {
            CheckLightning();
        }

        private void CheckLightning()
        {
            // If lighting is included in scramble and no early lighting capture allowed early beth must be enabled.
            // If you dont you cant get 9 captures to open beth if there is a non lighting piece in slide.
            if (checkBoxIncludeLightning.IsChecked == true && checkBoxEarlyLightning.IsChecked == false)
            {
                checkBoxEarlyBeth.IsEnabled = false;
                checkBoxEarlyBeth.IsChecked = true;
            }
            else
            {
                checkBoxEarlyBeth.IsEnabled = true;
            }
        }

        private void Button_Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Welcome to Shivers Randomizer 2.4\n\nHow to use:\n1. Launch Shivers\n2. Attach process to shivers \n3. " +
                "Press New Game (In Shivers)\n4. Change Settings as desired\n5. Press scramble\n\n The scramble button will only enable on the registry page.\n\n If you load a game or restart shivers the randomizer must also be restarted.");
        }

        //Allows only numbers in the seed box input
        private void NumbersValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Write_Click(object sender, RoutedEventArgs e)
        {
            app.WriteMemory(0, Convert.ToInt32(txtBox_WriteValue.Text));
        }

        private void Button_Read_Click(object sender, RoutedEventArgs e)
        {
            label_Value.Content = app.ReadMemory();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            app.DispatcherTimer();

            Random rng = new Random();
            if (rng.Next() % 100 == 0)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    //If you dont do it this way the sound breaks your god damn ear drums if you try to attach while sound clip playing.
                    using (SoundPlayer player = new SoundPlayer(Shivers_Randomizer_x64.Properties.Resources.Siren))
                    {
                        player.PlaySync();
                    }
                });
            }
        }

        private void Button_Music_Click(object sender, RoutedEventArgs e)
        {
            //StopAudio(31410);
            //StopAudio(15060);
            app.StopAudio(23550);
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("(" + app.roomNumberPrevious.ToString() + "," + app.roomNumber.ToString() + ")");
        }

        private void Button_SetMemoryTest_Click(object sender, RoutedEventArgs e)
        {
            //Sets slide in lobby to get to tar
            app.WriteMemory(368, 64);
        }
    }
}
