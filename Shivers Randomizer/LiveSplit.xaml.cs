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

    public bool connected = false;
    public bool timerStarted = false;
    public bool didSplitOnEnter = false;

    public LiveSplit(App app)
    {
        InitializeComponent();
        this.app = app;
        _socket = new(SocketType.Stream, ProtocolType.Tcp);
    }

    public void Disconnect()
    {
        _socket.Close();
        connected = false;
        button_Connect.Content = "Connect";
    }

    public void Ixupicaptured(int numberIxupiCaptured)
    {
        if (timerStarted)
        {
            if (settingsSplitCaptures)
            {
                Split();
            }
            else if (numberIxupiCaptured == 10)
            {
                Split();
            }
        }
    }

    public void RoomChange(int roomNumberPrevious, int roomNumber)
    {
        if (!timerStarted && roomNumberPrevious == 1012 && (roomNumber == 1000 || roomNumber == 1010))
        {
            Start();
        }

        if (settingsSplitEnter && timerStarted && !didSplitOnEnter)
        {
            if (app.settingsRoomShuffle)
            {
                if (roomNumberPrevious == 2330 && roomNumber == 3020)
                {
                    Split();
                    didSplitOnEnter = true;
                }
            }
            else
            {
                if (roomNumberPrevious == 4620 && roomNumber == 5010)
                {
                    Split();
                    didSplitOnEnter = true;
                }
            }
        }

        if (timerStarted && (roomNumber <= 0 || roomNumber == 910))
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
            
            Close();
        }
        catch (SocketException)
        {
            label_Feedback.Content = "Failed to connect. Make sure LiveSplit Server is started and port is correct.";
            button_Connect.Content = "Connect";
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

    private void Start()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("starttimer\r\n"));
            timerStarted = true;
            app.mainWindow.button_LiveSplit.IsEnabled = false;
        }
        catch (SocketException)
        {

        }
    }

    private void Reset()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("reset\r\n"));
            timerStarted = false;
            didSplitOnEnter = false;
            app.mainWindow.button_LiveSplit.IsEnabled = true;
        }
        catch (SocketException)
        {

        }
    }

    private void Split()
    {
        try
        {
            _socket.Send(Encoding.ASCII.GetBytes("split\r\n"));
        }
        catch (SocketException)
        {

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
