using System;
using System.Linq;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using MessagePartColor = Archipelago.MultiClient.Net.Models.Color;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;

namespace Shivers_Randomizer;

public partial class Archipelago_Client : Window
{
    ArchipelagoSession? session;

    string? serverUrl;
    string? userName;
    string? password;

    LoginResult? cachedConnectionResult;

    private readonly App app;

    public bool IsConnected => session?.Socket.Connected ?? false;
    private Permissions? ForfeitPermissions => session?.RoomState.ForfeitPermissions;
    private Permissions? CollectPermissions => session?.RoomState.CollectPermissions;

    private string? ConnectionId => session?.ConnectionInfo.Uuid;

    private string? SeedString => session?.RoomState.Seed;

    private DeathLinkService GetDeathLinkService() => session.CreateDeathLinkService();

    private string? GetCurrentPlayerName() => session?.Players.GetPlayerAliasAndName(session.ConnectionInfo.Slot);

    private DataStorageHelper? DataStorage => session?.DataStorage;

    private int? Slot => session?.ConnectionInfo.Slot;
    private int? Team => session?.ConnectionInfo.Team;

    private RichTextBox serverMessageBox;

    public string[,] storagePlacementsArray = new string[0,0];
    private bool userHasScrolledUp;

    public Archipelago_Client(App app)
    {
        InitializeComponent();
        serverMessageBox = ServerMessageBox;
        this.app = app;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Disconnect();
        MainWindow.isArchipelagoClientOpen = false;
    }

    public LoginResult Connect(string server, string user, string pass = null, string connectionId = null)
    {
        if (IsConnected && cachedConnectionResult != null)
        {
            if (serverUrl == server && userName == user && password == pass)
            {
                return cachedConnectionResult;
            }

            Disconnect();
        }

        serverUrl = server;
        userName = user;
        password = pass;

        try
        {
            session = ArchipelagoSessionFactory.CreateSession(serverUrl);

            session.MessageLog.OnMessageReceived += (message) => OnMessageReceived(message, serverMessageBox);

            session.Socket.ErrorReceived += (exception, message) => Socket_ErrorReceived(exception, message, serverMessageBox);

            cachedConnectionResult = session.TryConnectAndLogin("Shivers", userName, ItemsHandlingFlags.AllItems, password: password, requestSlotData: true);

            if (IsConnected)
            {
                //Grab Pot placement data
                var jsonObject = ((LoginSuccessful)cachedConnectionResult).SlotData;
                JToken storagePlacements = jsonObject["storageplacements"] as JToken;
                storagePlacementsArray = new string[storagePlacements.Count(), 2];

                int i = 0;
                foreach (JToken token in storagePlacements)
                {
                    string key = token.Path.Split('.').Last().Replace("Accessible: Storage: ", "").Trim('\'', '[', ']');
                    string value = token.First.ToString().Replace(" DUPE", "");
                    storagePlacementsArray[i, 0] = key;
                    storagePlacementsArray[i, 1] = value;
                    i++;
                }
            }
        }
        catch (AggregateException e)
        {
            cachedConnectionResult = new LoginFailure(e.GetBaseException().Message);
        }

        return cachedConnectionResult;
    }
    
    static void Socket_ErrorReceived(Exception e, string message, RichTextBox richTextBox)
    {
        richTextBox.Dispatcher.Invoke(() =>
        {
            richTextBox.AppendText($"Socket Error: {message}" + Environment.NewLine);
            richTextBox.AppendText($"Socket Error: {e.Message}" + Environment.NewLine);
            foreach (var line in e.StackTrace?.Split('\n') ?? Array.Empty<string>())
            {
                richTextBox.AppendText($"    {line}" + Environment.NewLine);
            }
        });
    }

    public async void Disconnect()
    {
        if (session != null && IsConnected)
        {
            await session.Socket.DisconnectAsync();
        }

        serverUrl = null;
        userName = null;
        password = null;
        session = null;
        cachedConnectionResult = null;
        buttonConnect.Content = "Connect";

        app.StopArchipelago();
    }

    public void SetStatus(ArchipelagoClientState status) => SendPacket(new StatusUpdatePacket { Status = status });

    public void OnMessageReceived(LogMessage message, RichTextBox richTextBox)
    {
        var parts = message.Parts.Select(p => new Part(p.Text, p.Color)).ToArray();
        Thread.Sleep(10); //Add a small sleep, hopefully this will help with sneaking through locked doors during a massive recieve/collect

        richTextBox.Dispatcher.Invoke(() =>
        {
            foreach (Part part in parts)
            {
                ModifyColors(part);

                System.Windows.Media.Color color = FromDrawingColor(part.Color);
                Brush brush = new SolidColorBrush(color);

                TextRange range = new(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd)
                {
                    Text = part.Text
                };
                range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            richTextBox.AppendText(Environment.NewLine);

            //Scroll text box automatically if the user hasnt scrolled up
            if(!userHasScrolledUp)
            {
                serverMessageBox.ScrollToEnd();
            }
        });
    }

    private static System.Windows.Media.Color FromDrawingColor(MessagePartColor drawingColor) =>
        System.Windows.Media.Color.FromArgb(255, drawingColor.R, drawingColor.G, drawingColor.B);

    private static void ModifyColors(Part part)
    {
        if (part.Color == MessagePartColor.Magenta)
        {
            part.Color.R = 238;
            part.Color.B = 238;
        }
        else if (part.Color == MessagePartColor.Green)
        {
            part.Color.R = 0;
            part.Color.G = 254;
            part.Color.B = 127;
        }
        else if (part.Color == MessagePartColor.Yellow)
        {
            part.Color.R = 246;
            part.Color.G = 246;
            part.Color.B = 207;
        }
        else if (part.Color == MessagePartColor.Plum)
        {
            part.Color.R = 175;
            part.Color.G = 153;
            part.Color.B = 239;
        }
        else if(part.Color == MessagePartColor.SlateBlue)
        {
            part.Color.R = 109;
            part.Color.G = 139;
            part.Color.B = 232;
        }
    }

    internal class Part
    {
        public string Text { get; set; }
        public MessagePartColor Color;

        public Part(string text, MessagePartColor color)
        {
            Text = text;
            Color = color;
        }
    }

    private void SendPacket(ArchipelagoPacketBase packet) => session?.Socket.SendPacket(packet);

    public void Say(string message) => SendPacket(new SayPacket { Text = message });

    private bool IsMe(int slot) => slot == session?.ConnectionInfo.Slot;

    private void ButtonConnect_Click(object sender, RoutedEventArgs e)
    {
        if(!IsConnected)
        {
            //Attempt wss connection, if fails attempt ws connection
            Connect("wss://" + serverIP.Text, slotName.Text, serverPassword.Text);
            if(!IsConnected)
            {
                Connect(serverIP.Text, slotName.Text, serverPassword.Text);
            }

            if(IsConnected)
            {
                buttonConnect.Content = "Disconnect";
            }
        }
        else
        {
            Disconnect();
        }
    }

    public void SendCheck(int checkID)
    {
        session?.Locations.CompleteLocationChecks(checkID);
    }

    public List<int> GetItemsFromArchipelagoServer()
    {
        ReadOnlyCollection<NetworkItem> networkItems = session?.Items.AllItemsReceived ?? new ReadOnlyCollection<NetworkItem>(new List<NetworkItem>());
        return (from NetworkItem item in networkItems select (int)item.Item).ToList();
    }

    public void Send_completion()
    {
        var statusUpdatePacket = new StatusUpdatePacket
        {
            Status = ArchipelagoClientState.ClientGoal
        };
        session?.Socket.SendPacket(statusUpdatePacket);
    }

    public List<long>? GetLocationsCheckedArchipelagoServer()
    {
        return session?.Locations.AllLocationsChecked.ToList();
    }

    public void SaveData(string key, int value)
    {
        if (session != null)
        {
            session.DataStorage[Scope.Slot, key] = value;
        }
    }

    public int? LoadData(string key)
    {
        return session?.DataStorage[Scope.Slot, key];
    }

    private void ServerMessageBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        userHasScrolledUp = e.VerticalOffset < e.ExtentHeight - e.ViewportHeight;
    }

    private void ButtonCommands_Click(object sender, RoutedEventArgs e)
    {
        string CommandMessage = commandBox.Text;
        commandBox.Text = "";
        Commands(CommandMessage);
    }
    private void CommandBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string CommandMessage = commandBox.Text;
            commandBox.Text = "";
            Commands(CommandMessage);
        }
    }

    public void Commands(string command)
    {
        if (!string.IsNullOrEmpty(command))
        {
            Say(command);
        }
    }
}
