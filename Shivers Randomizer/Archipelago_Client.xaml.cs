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
using System.Windows.Media;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using static Shivers_Randomizer.utils.AppHelpers;
using static Shivers_Randomizer.utils.Constants;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Shivers_Randomizer;

public partial class Archipelago_Client : Window
{
    ArchipelagoSession? session;

    string? serverUrl;
    string? userName;
    string? password;

    LoginResult? cachedConnectionResult;

    private readonly App app;

    public bool IsConnected => (session?.Socket.Connected ?? false) && (cachedConnectionResult?.Successful ?? false);
    private Permissions? ForfeitPermissions => session?.RoomState.ForfeitPermissions;
    private Permissions? CollectPermissions => session?.RoomState.CollectPermissions;

    private string? ConnectionId => session?.ConnectionInfo.Uuid;

    private string? SeedString => session?.RoomState.Seed;

    private DeathLinkService GetDeathLinkService() => session.CreateDeathLinkService();

    private string? GetCurrentPlayerName() => session?.Players.GetPlayerAliasAndName(session.ConnectionInfo.Slot);

    private DataStorageHelper? DataStorage => session?.DataStorage;

    private int? Slot => session?.ConnectionInfo.Slot;
    private int? Team => session?.ConnectionInfo.Team;

    private readonly RichTextBox serverMessageBox;

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
        app.archipelago_Client = null;
    }

    public LoginResult Connect(string server, string user, string? pass = null, string? connectionId = null)
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
                // Grab Pot placement data
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
            else if (cachedConnectionResult is LoginFailure failure)
            {
                string messageToPrint = "";
                failure.Errors.ToList().ForEach(error =>
                {
                    messageToPrint += $"Connection Error: {error}{Environment.NewLine}";
                });

                serverMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                ScrollMessages();
            }
        }
        catch (AggregateException e)
        {
            cachedConnectionResult = new LoginFailure(e.GetBaseException().Message);
        }

        return cachedConnectionResult;
    }
    
    private void Socket_ErrorReceived(Exception e, string message, RichTextBox richTextBox)
    {
        string messageToPrint = $"Socket Error: {message}{Environment.NewLine}";
        messageToPrint += $"Socket Error: {e.Message}{Environment.NewLine}";
        foreach (var line in e.StackTrace?.Split('\n') ?? Array.Empty<string>())
        {
            messageToPrint += $"    {line}{Environment.NewLine}";
        }

        richTextBox.AppendTextWithColor(messageToPrint, Brushes.Red);
        ScrollMessages();
    }

    public async void Disconnect()
    {
        if (session != null)
        {
            if (IsConnected)
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
    }

    public void SetStatus(ArchipelagoClientState status) => SendPacket(new StatusUpdatePacket { Status = status });

    public void OnMessageReceived(LogMessage message, RichTextBox richTextBox)
    {
        var parts = message.Parts.Select(p => new Part(p.Text, p.Color)).ToArray();
        Thread.Sleep(10); // Add a small sleep, hopefully this will help with sneaking through locked doors during a massive recieve/collect

        richTextBox.Dispatcher.Invoke(() =>
        {
            foreach (Part part in parts)
            {
                ModifyColors(part);
                System.Windows.Media.Color color = FromDrawingColor(part.Color);
                richTextBox.AppendTextWithColor(part.Text, new SolidColorBrush(color));
            }
            richTextBox.AppendText(Environment.NewLine);
            ScrollMessages();
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
            // Attempt wss connection, if fails attempt ws connection
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

    public async Task<int?> LoadData(string key)
    {
        DataStorageElement? data = session?.DataStorage[Scope.Slot, key];
        if (data == null)
        {
            return null;
        }

        return (await data.GetAsync()).Value<int?>();
    }

    public void Commands(string command)
    {
        if (!string.IsNullOrEmpty(command))
        {
            Say(command);
        }
    }

    public void InitilizeDataStorage(int skullDialPrehistoric, int skullDialTarRiver, int skullDialWerewolf, int skullDialBurial, int skullDialEgypt, int skullDialGods)
    {
        // Initilize Data storage
        session?.DataStorage[Scope.Slot, "PlayerLocation"].Initialize(1012);
        session?.DataStorage[Scope.Slot, "LastRecievedItemValue"].Initialize(0);
        session?.DataStorage[Scope.Slot, "SkullDialPrehistoric"].Initialize(skullDialPrehistoric);
        session?.DataStorage[Scope.Slot, "SkullDialTarRiver"].Initialize(skullDialTarRiver);
        session?.DataStorage[Scope.Slot, "SkullDialWerewolf"].Initialize(skullDialWerewolf);
        session?.DataStorage[Scope.Slot, "SkullDialBurial"].Initialize(skullDialBurial);
        session?.DataStorage[Scope.Slot, "SkullDialEgypt"].Initialize(skullDialEgypt);
        session?.DataStorage[Scope.Slot, "SkullDialGods"].Initialize(skullDialGods);
        session?.DataStorage[Scope.Slot, "Jukebox"].Initialize(0);
        session?.DataStorage[Scope.Slot, "TarRiverShortcut"].Initialize(0);
    }

    public void ArchipelagoUpdateWindow(int roomNumber, List<int> items)
    {
        // Update storage
        bool connected = IsConnected && roomNumber != 910 && roomNumber != 922;
        LabelStorageDeskDrawer.Content = connected ? ConvertPotNumberToString(app.ReadMemory(0, 1)) : "";
        LabelStorageWorkshopDrawers.Content = connected ? ConvertPotNumberToString(app.ReadMemory(8, 1)) : "";
        LabelStorageLibraryCabinet.Content = connected ? ConvertPotNumberToString(app.ReadMemory(16, 1)) : "";
        LabelStorageLibraryStatue.Content = connected ? ConvertPotNumberToString(app.ReadMemory(24, 1)) : "";
        LabelStorageSlide.Content = connected ? ConvertPotNumberToString(app.ReadMemory(32, 1)) : "";
        LabelStorageEaglesHead.Content = connected ? ConvertPotNumberToString(app.ReadMemory(40, 1)) : "";
        LabelStorageEaglesNest.Content = connected ? ConvertPotNumberToString(app.ReadMemory(48, 1)) : "";
        LabelStorageOcean.Content = connected ? ConvertPotNumberToString(app.ReadMemory(56, 1)) : "";
        LabelStorageTarRiver.Content = connected ? ConvertPotNumberToString(app.ReadMemory(64, 1)) : "";
        LabelStorageTheater.Content = connected ? ConvertPotNumberToString(app.ReadMemory(72, 1)) : "";
        LabelStorageGreenhouse.Content = connected ? ConvertPotNumberToString(app.ReadMemory(80, 1)) : "";
        LabelStorageEgypt.Content = connected ? ConvertPotNumberToString(app.ReadMemory(88, 1)) : "";
        LabelStorageChineseSolitaire.Content = connected ? ConvertPotNumberToString(app.ReadMemory(96, 1)) : "";
        LabelStorageTikiHut.Content = connected ? ConvertPotNumberToString(app.ReadMemory(104, 1)) : "";
        LabelStorageLyre.Content = connected ? ConvertPotNumberToString(app.ReadMemory(112, 1)) : "";
        LabelStorageSkeleton.Content = connected ? ConvertPotNumberToString(app.ReadMemory(120, 1)) : "";
        LabelStorageAnansi.Content = connected ? ConvertPotNumberToString(app.ReadMemory(128, 1)) : "";
        LabelStorageJanitorCloset.Content = connected ? ConvertPotNumberToString(app.ReadMemory(136, 1)) : "";
        LabelStorageUFO.Content = connected ? ConvertPotNumberToString(app.ReadMemory(144, 1)) : "";
        LabelStorageAlchemy.Content = connected ? ConvertPotNumberToString(app.ReadMemory(152, 1)) : "";
        LabelStorageSkullBridge.Content = connected ? ConvertPotNumberToString(app.ReadMemory(160, 1)) : "";
        LabelStorageHanging.Content = connected ? ConvertPotNumberToString(app.ReadMemory(168, 1)) : "";
        LabelStorageClockTower.Content = connected ? ConvertPotNumberToString(app.ReadMemory(176, 1)) : "";

        // Update keys
        LabelKeyOfficeElevator.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 20) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyBedroomElevator.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 21) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyThreeFloorElevator.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 22) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyWorkshop.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 23) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyLobby.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 24) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyPrehistoric.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 25) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyGreenhouse.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 26) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyOcean.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 27) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyProjector.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 28) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyGenerator.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 29) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyEgypt.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 30) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyLibrary.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 31) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyTiki.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 32) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyUFO.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 33) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyTorture.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 34) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyPuzzle.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 35) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyBedroom.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 36) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyUndergroundLake.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 37) ? Visibility.Visible : Visibility.Hidden;
        LabelKeyCrawling.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 50) ? Visibility.Visible : Visibility.Hidden;
        LabelEasierLyre.Visibility = connected && items.Contains(ARCHIPELAGO_BASE_ITEM_ID + 91) ? Visibility.Visible : Visibility.Hidden;
        LabelEasierLyre.Content = connected ? "Easier Lyre x " + (items?.Count(item => item == (ARCHIPELAGO_BASE_ITEM_ID + 91)) ?? 0) : "";
    }

    public async void ReportNewItemsReceived()
    {
        int lastItemCount = await LoadData("LastReceivedItemValue") ?? 0;
        List<int> items = GetItemsFromArchipelagoServer();
        Brush plumBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 153, 239));

        if (lastItemCount < items.Count)
        {
            serverMessageBox.AppendTextWithColor($"Since you last connected you have received the following items:{Environment.NewLine}", Brushes.LimeGreen);
            for (int i = lastItemCount; i < items.Count; i++)
            {
                string itemName = GetItemName(items[i]) ?? "Error Retrieving Item";
                string message = $"{itemName} {Environment.NewLine}";

                if (items[i] < ARCHIPELAGO_BASE_ITEM_ID + 90)
                {
                    serverMessageBox.AppendTextWithColor(message, plumBrush);
                }
                else
                {
                    serverMessageBox.AppendTextWithColor(message, Brushes.Cyan);
                }
            }

            ScrollMessages();
            SaveData("LastReceivedItemValue", items.Count);
        }
    }

    public void MoveToRegistry()
    {
        serverMessageBox.AppendTextWithColor($"Please move to registry page.{Environment.NewLine}", Brushes.Red);
        ScrollMessages();
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

    private string GetItemName(int itemID)
    {
        return session?.Items.GetItemName(itemID) ?? "Error retrieving item";
    }

    private void ScrollMessages()
    {
        if (!userHasScrolledUp)
        {
            serverMessageBox.ScrollToEnd();
        }
    }
}
