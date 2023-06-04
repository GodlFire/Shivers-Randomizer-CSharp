using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Shivers_Randomizer;

public partial class LiveSplit : Window
{
    private readonly App app;
    private readonly Socket _socket;
    private const string Default_Port = "16834";
    private bool settingsSplitEnter;
    private bool settingsSplitCaptures;
    private bool settingsSpliFirstBlood;
    private bool settingsSplitJaffra;

    private bool connected = false;
    private bool timerStarted = false;
    private bool didSplitOnEnter = false;
    private bool didSplitOnElevator = false;
    private bool didSplitOnFirstBlood = false;
    private bool didSplitOnLibrary = false;
    private bool didSplitOnBeth = false;
    private bool didSplitOnJukebox = false;

    public LiveSplit(App app)
    {
        InitializeComponent();
        this.app = app;
        _socket = new(SocketType.Stream, ProtocolType.Tcp);
    }

    public void Disconnect()
    {
        if (connected)
        {
            if (timerStarted)
            {
                Reset();
            }

            _socket.Close();
            connected = false;
            button_Connect.Content = "Connect";
            settingsSplitEnter = false;
            settingsSplitCaptures = false;
            settingsSpliFirstBlood = false;
            settingsSplitJaffra = false;
        }
    }

    public void BethRiddleFound()
    {
        if (settingsSplitJaffra && timerStarted && !didSplitOnBeth && App.IsKthBitSet(app.ReadMemory(376, 1), 7)) // Final Riddle: Beth's Body Page 17 +178 Bit 8
        {
            Split();
            didSplitOnBeth = true;
        }
    }

    public void HealthChanged(int healthPrevious, int health, int roomNumber)
    {
        if (settingsSpliFirstBlood && timerStarted && !didSplitOnFirstBlood && roomNumber == 3260 && healthPrevious == 100 && health == 90)
        {
            Split();
            didSplitOnFirstBlood = true;
        }
    }

    public void IxupiCaptured(int numberIxupiCaptured)
    {
        if (timerStarted)
        {
            if (settingsSplitCaptures || settingsSplitJaffra && numberIxupiCaptured == 1)
            {
                Split();
            }
            else if (numberIxupiCaptured == 10)
            {
                Split();
            }
        }
    }

    public void JukeboxSet()
    {
        if (settingsSplitJaffra && timerStarted && !didSplitOnJukebox && App.IsKthBitSet(app.ReadMemory(377, 1), 5)) // Song set on jukebox +179 Bit 6
        {
            Split();
            didSplitOnJukebox = true;
        }
    }

    public void RoomChange(int roomNumberPrevious, int roomNumber)
    {
        // Timer starts on turning away from gate.
        if (!timerStarted && roomNumberPrevious == 1012 && (roomNumber == 1000 || roomNumber == 1010))
        {
            Start();
        }

        if (settingsSplitEnter && timerStarted && !didSplitOnEnter)
        {
            if (app.settingsRoomShuffle)
            {
                SplitOnRoomChange(ref didSplitOnEnter, roomNumberPrevious, roomNumber, 2330, 3020);
            }
            else
            {
                SplitOnRoomChange(ref didSplitOnElevator, roomNumberPrevious, roomNumber, 4620, 5010);
            }
        }

        if (settingsSplitJaffra && timerStarted)
        {
            SplitOnRoomChange(ref didSplitOnEnter, roomNumberPrevious, roomNumber, 2310, 2330);
            SplitOnRoomChange(ref didSplitOnElevator, roomNumberPrevious, roomNumber, 4620, 5010);
            SplitOnRoomChange(ref didSplitOnLibrary, roomNumberPrevious, roomNumber, 8030, 9450);
        }

        // Reset timer if on main menu or app closes (maybe?)
        if (timerStarted && roomNumber == 910)
        {
            Reset();
        }
    }

    private void Button_Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!connected)
            {
                _socket.Connect("localhost", Convert.ToInt32(txtBox_Port.Text));
                connected = true;
            }

            button_Connect.Content = "Update";
            settingsSplitEnter = checkBox_SplitEnter.IsChecked == true;
            settingsSplitCaptures = checkBox_SplitCaptures.IsChecked == true;
            settingsSpliFirstBlood = checkBox_SplitFirstBlood.IsChecked == true;
            settingsSplitJaffra = checkBox_SplitJaffra.IsChecked == true;
            
            Close();
        }
        catch (SocketException)
        {
            label_Feedback.Content = "Failed to connect. Make sure LiveSplit Server is started and port is correct.";
            button_Connect.Content = "Connect";
        }
    }

    private void CheckBox_SplitJaffra_Click(object sender, RoutedEventArgs e)
    {
        if (checkBox_SplitJaffra.IsChecked == true)
        {
            checkBox_SplitEnter.IsChecked = false;
            checkBox_SplitEnter.IsEnabled = false;
            checkBox_SplitCaptures.IsChecked = false;
            checkBox_SplitCaptures.IsEnabled = false;
            checkBox_SplitFirstBlood.IsChecked = false;
            checkBox_SplitFirstBlood.IsEnabled = false;
        }
        else
        {
            checkBox_SplitEnter.IsEnabled = true;
            checkBox_SplitCaptures.IsEnabled = true;
            checkBox_SplitFirstBlood.IsEnabled = true;
        }
    }

    private void NumbersValidation(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void TxtBox_Port_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (button_Connect != null)
        {
            button_Connect.IsEnabled = txtBox_Port.Text != string.Empty;
        }
    }

    private void Reset()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("reset\r\n"));
            timerStarted = false;
            didSplitOnEnter = false;
            didSplitOnElevator = false;
            didSplitOnFirstBlood = false;
            didSplitOnLibrary = false;
            didSplitOnBeth = true;
            didSplitOnJukebox = false;
            Dispatcher.Invoke(() =>
            {
                app.mainWindow.button_LiveSplit.IsEnabled = true;
            });
        }
        catch (SocketException)
        {

        }
    }

    private void Start()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("starttimer\r\n"));
            timerStarted = true;
            Dispatcher.Invoke(() =>
            {
                app.mainWindow.button_LiveSplit.IsEnabled = false;
            });
        }
        catch (SocketException)
        {

        }
    }

    public void Split()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("split\r\n"));
        }
        catch (SocketException)
        {

        }
    }

    private void SplitOnRoomChange(ref bool didSplit, int roomNumberPrevious, int roomNumber, int expectedPreviousRoom, int expectedRoom)
    {
        if (!didSplit && roomNumberPrevious == expectedPreviousRoom && roomNumber == expectedRoom)
        {
            Split();
            didSplit = true;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (txtBox_Port.Text == string.Empty)
        {
            txtBox_Port.Text = Default_Port;
        }
        
        label_Feedback.Content = string.Empty;
        e.Cancel = true;
        Hide();
    }
}
