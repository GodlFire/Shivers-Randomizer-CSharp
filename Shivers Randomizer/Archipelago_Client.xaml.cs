using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using Shivers_Randomizer.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Shivers_Randomizer.utils.AppHelpers;
using static Shivers_Randomizer.utils.Constants;
using MessagePartColor = Archipelago.MultiClient.Net.Models.Color;

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

    public string[,] storagePlacementsArray = new string[0,0];
    public bool slotDataSettingElevators;
    public bool slotDataSettingEarlyBeth;
    public bool slotDataEarlyLightning;
    public int slotDataIxupiCapturesNeeded = 10;
    private bool userHasScrolledUp;
    private const int MAX_MESSAGES = 1000;
    private readonly Queue<LogMessage> pendingMessages = new();
    private readonly DispatcherTimer messageTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(2)
    };

    public Archipelago_Client(App app)
    {
        InitializeComponent();
        this.app = app;
        messageTimer.Tick += MessageTimer_Tick;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        messageTimer.Stop();
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

            session.MessageLog.OnMessageReceived += OnMessageReceived;

            session.Socket.ErrorReceived += Socket_ErrorReceived;

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

                //Grab elevator setting
                TryGetBoolSetting(jsonObject, "elevatorsstaysolved", out slotDataSettingElevators);

                //Grab early beth setting
                TryGetBoolSetting(jsonObject, "earlybeth", out slotDataSettingEarlyBeth);

                //Grab early lightning setting
                TryGetBoolSetting(jsonObject, "earlylightning", out slotDataEarlyLightning);

                //Grab goal ixupi capture setting
                slotDataIxupiCapturesNeeded = TryGetIntSetting(jsonObject, "ixupicapturesneeded", 10);
            }
            else if (cachedConnectionResult is LoginFailure failure)
            {
                string messageToPrint = "";
                failure.Errors.ToList().ForEach(error =>
                {
                    messageToPrint += $"Connection Error: {error}{Environment.NewLine}";
                });

                ServerMessageBox.Dispatcher.Invoke(() =>
                {
                    ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
                    ScrollMessages();
                });
            }
        }
        catch (AggregateException e)
        {
            cachedConnectionResult = new LoginFailure(e.GetBaseException().Message);
        }

        return cachedConnectionResult;
    }

    public static bool TryGetBoolSetting(Dictionary<string, object> jsonObject, string key, out bool result)
    {
        result = false;

        if (jsonObject.ContainsKey(key))
        {
            JToken token = (jsonObject[key] as JToken);

            if (token != null && token.Count() > 0)
            {
                result = (bool)token[0];
                return true;
            }
        }

        MessageBox.Show("Could not find the setting for '" + key + "' from the server. The option will be turned off.");

        return false;
    }

    public static int TryGetIntSetting(Dictionary<string, object> jsonObject, string key, int defaultValue)
    {
        if (jsonObject.ContainsKey(key))
        {
            JToken token = (jsonObject[key] as JToken);

            if (token != null && token.Count() > 0)
            {
                if (int.TryParse(token[0].ToString(), out int result))
                {
                    return result;
                }
            }
        }

        MessageBox.Show($"Could not find the setting for '{key}' from the server. The default value ({defaultValue}) will be used.");

        return defaultValue;
    }


    private void Socket_ErrorReceived(Exception e, string message)
    {
        string messageToPrint = $"Socket Error: {message}{Environment.NewLine}";
        messageToPrint += $"Socket Error: {e.Message}{Environment.NewLine}";
        foreach (var line in e.StackTrace?.Split('\n') ?? Array.Empty<string>())
        {
            messageToPrint += $"    {line}{Environment.NewLine}";
        }

        ServerMessageBox.Dispatcher.Invoke(() =>
        {
            ServerMessageBox.AppendTextWithColor(messageToPrint, Brushes.Red);
            ScrollMessages();
        });
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

    private void OnMessageReceived(LogMessage message)
    {
        pendingMessages.Enqueue(message);
        if (!messageTimer.IsEnabled)
        {
            messageTimer.Start();
        }
    }

    private void MessageTimer_Tick(object? sender, EventArgs e)
    {
        if (pendingMessages.TryDequeue(out var message))
        {
            var messageParts = message.Parts.Select(p =>
            {
                var part = new Part(p.Text, p.Color);
                ModifyColors(part);
                return part;
            }).ToList();
                
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                var document = ServerMessageBox.Document;
                while (document.Blocks.Count > MAX_MESSAGES)
                {
                    document.Blocks.Remove(document.Blocks.FirstBlock);
                }

                messageParts.ForEach(part =>
                {
                    System.Windows.Media.Color color = FromDrawingColor(part.Color);
                    ServerMessageBox.AppendTextWithColor(part.Text, new SolidColorBrush(color));
                });
                ServerMessageBox.AppendTextWithColor(Environment.NewLine, Brushes.Transparent);
                ScrollMessages();
            });
        } else {
            messageTimer.Stop();
        }
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

    private void ButtonConnect_Click(object sender, RoutedEventArgs e)
    {
        using (new CursorBusy())
        {
            if (!IsConnected)
            {
                // Attempt wss connection, if fails attempt ws connection
                Connect("wss://" + serverIP.Text, slotName.Text, serverPassword.Text);
                if (!IsConnected)
                {
                    Connect(serverIP.Text, slotName.Text, serverPassword.Text);
                }

                if (IsConnected)
                {
                    buttonConnect.Content = "Disconnect";
                }
            }
            else
            {
                Disconnect();
            }
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
        session?.DataStorage[Scope.Slot, "Health"].Initialize(100);
        session?.DataStorage[Scope.Slot, "WaterDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "WaxDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "AshDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "OilDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "ClothDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "WoodDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "CrystalDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "LightningDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "SandDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "MetalDamage"].Initialize(0);
        session?.DataStorage[Scope.Slot, "HealItemsReceived"].Initialize(0);
        session?.DataStorage[Scope.Slot, "IxupiCapturedStates"].Initialize(0);
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
        LabelStorageTransformingMask.Content = connected ? ConvertPotNumberToString(app.ReadMemory(40, 1)) : "";
        LabelStorageEaglesNest.Content = connected ? ConvertPotNumberToString(app.ReadMemory(48, 1)) : "";
        LabelStorageOcean.Content = connected ? ConvertPotNumberToString(app.ReadMemory(56, 1)) : "";
        LabelStorageTarRiver.Content = connected ? ConvertPotNumberToString(app.ReadMemory(64, 1)) : "";
        LabelStorageTheater.Content = connected ? ConvertPotNumberToString(app.ReadMemory(72, 1)) : "";
        LabelStorageGreenhouse.Content = connected ? ConvertPotNumberToString(app.ReadMemory(80, 1)) : "";
        LabelStorageEgypt.Content = connected ? ConvertPotNumberToString(app.ReadMemory(88, 1)) : "";
        LabelStorageChineseSolitaire.Content = connected ? ConvertPotNumberToString(app.ReadMemory(96, 1)) : "";
        LabelStorageShamanHut.Content = connected ? ConvertPotNumberToString(app.ReadMemory(104, 1)) : "";
        LabelStorageLyre.Content = connected ? ConvertPotNumberToString(app.ReadMemory(112, 1)) : "";
        LabelStorageSkeleton.Content = connected ? ConvertPotNumberToString(app.ReadMemory(120, 1)) : "";
        LabelStorageAnansi.Content = connected ? ConvertPotNumberToString(app.ReadMemory(128, 1)) : "";
        LabelStorageJanitorCloset.Content = connected ? ConvertPotNumberToString(app.ReadMemory(136, 1)) : "";
        LabelStorageUFO.Content = connected ? ConvertPotNumberToString(app.ReadMemory(144, 1)) : "";
        LabelStorageAlchemy.Content = connected ? ConvertPotNumberToString(app.ReadMemory(152, 1)) : "";
        LabelStorageSkullBridge.Content = connected ? ConvertPotNumberToString(app.ReadMemory(160, 1)) : "";
        LabelStorageGallows.Content = connected ? ConvertPotNumberToString(app.ReadMemory(168, 1)) : "";
        LabelStorageClockTower.Content = connected ? ConvertPotNumberToString(app.ReadMemory(176, 1)) : "";
        
        // Update keys
        LabelKeyOfficeElevator.Foreground = connected && items.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR ) ? Brushes.White : Brushes.Gray;
        LabelKeyBedroomElevator.Foreground = connected && items.Contains((int)APItemID.KEYS.BEDROOM_ELEVATOR) ? Brushes.White : Brushes.Gray;
        LabelKeyThreeFloorElevator.Foreground = connected && items.Contains((int)APItemID.KEYS.THREE_FLOOR_ELEVATOR) ? Brushes.White : Brushes.Gray;
        LabelKeyWorkshop.Foreground = connected && items.Contains((int)APItemID.KEYS.WORKSHOP) ? Brushes.White : Brushes.Gray;
        LabelKeyOffice.Foreground = connected && items.Contains((int)APItemID.KEYS.OFFICE_ELEVATOR) ? Brushes.White : Brushes.Gray;
        LabelKeyPrehistoric.Foreground = connected && items.Contains((int)APItemID.KEYS.PREHISTORIC) ? Brushes.White : Brushes.Gray;
        LabelKeyGreenhouse.Foreground = connected && items.Contains((int)APItemID.KEYS.GREENHOUSE) ? Brushes.White : Brushes.Gray;
        LabelKeyOcean.Foreground = connected && items.Contains((int)APItemID.KEYS.OCEAN) ? Brushes.White : Brushes.Gray;
        LabelKeyProjector.Foreground = connected && items.Contains((int)APItemID.KEYS.PROJECTOR) ? Brushes.White : Brushes.Gray;
        LabelKeyGenerator.Foreground = connected && items.Contains((int)APItemID.KEYS.GENERATOR) ? Brushes.White : Brushes.Gray;
        LabelKeyEgypt.Foreground = connected && items.Contains((int)APItemID.KEYS.EGYPT) ? Brushes.White : Brushes.Gray;
        LabelKeyLibrary.Foreground = connected && items.Contains((int)APItemID.KEYS.LIBRARY) ? Brushes.White : Brushes.Gray;
        LabelKeyShaman.Foreground = connected && items.Contains((int)APItemID.KEYS.SHAMAN) ? Brushes.White : Brushes.Gray;
        LabelKeyUFO.Foreground = connected && items.Contains((int)APItemID.KEYS.UFO) ? Brushes.White : Brushes.Gray;
        LabelKeyTorture.Foreground = connected && items.Contains((int)APItemID.KEYS.TORTURE) ? Brushes.White : Brushes.Gray;
        LabelKeyPuzzle.Foreground = connected && items.Contains((int)APItemID.KEYS.PUZZLE) ? Brushes.White : Brushes.Gray;
        LabelKeyBedroom.Foreground = connected && items.Contains((int)APItemID.KEYS.BEDROOM) ? Brushes.White : Brushes.Gray;
        LabelKeyUndergroundLake.Foreground = connected && items.Contains((int)APItemID.KEYS.UNDERGROUND_LAKE_ROOM) ? Brushes.White : Brushes.Gray;
        LabelKeyJantiorCloset.Foreground = connected && items.Contains((int)APItemID.KEYS.JANITOR_CLOSET) ? Brushes.White : Brushes.Gray;
        LabelKeyFrontDoor.Foreground = connected && items.Contains((int)APItemID.KEYS.FRONT_DOOR) ? Brushes.White : Brushes.Gray;
        LabelKeyCrawling.Foreground = connected && items.Contains((int)APItemID.ABILITIES.CRAWLING) ? Brushes.White : Brushes.Gray;
        LabelEasierLyre.Visibility = connected && items.Contains((int)APItemID.FILLER.EASIER_LYRE) ? Visibility.Visible : Visibility.Hidden;
        LabelEasierLyre.Content = connected ? "Easier Lyre x " + (items?.Count(item => item == ((int)APItemID.FILLER.EASIER_LYRE)) ?? 0) : "";
    }

    public async void ReportNewItemsReceived()
    {
        int lastItemCount = await LoadData("LastReceivedItemValue") ?? 0;
        List<int> items = GetItemsFromArchipelagoServer();
        Brush plumBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 153, 239));

        if (lastItemCount < items.Count)
        {
            ServerMessageBox.Dispatcher.Invoke(() =>
            {
                ServerMessageBox.AppendTextWithColor($"Since you last connected you have received the following items:{Environment.NewLine}", Brushes.LimeGreen);
                for (int i = lastItemCount; i < items.Count; i++)
                {
                    string itemName = GetItemName(items[i]) ?? "Error Retrieving Item";
                    string message = $"{itemName} {Environment.NewLine}";

                    if (items[i] < ARCHIPELAGO_BASE_ITEM_ID + 90)
                    {
                        ServerMessageBox.AppendTextWithColor(message, plumBrush);
                    }
                    else
                    {
                        ServerMessageBox.AppendTextWithColor(message, Brushes.Cyan);
                    }
                }

                ScrollMessages();
            });
            SaveData("LastReceivedItemValue", items.Count);
        }
    }

    public void MoveToRegistry()
    {
        ServerMessageBox.Dispatcher.Invoke(() =>
        {
            ServerMessageBox.AppendTextWithColor($"Please press New Game in Shivers.{Environment.NewLine}", Brushes.Red);
            ScrollMessages();
        });
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
            ServerMessageBox.ScrollToEnd();
        }
    }
}
