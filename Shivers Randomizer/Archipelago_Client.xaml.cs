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

namespace Shivers_Randomizer
{
    public partial class Archipelago_Client : Window
    {
        static ArchipelagoSession session;

        static string serverUrl;
        static string userName;
        static string password;

        static LoginResult cachedConnectionResult;

        public static bool IsConnected;
        public static Permissions ForfeitPermissions => session.RoomState.ForfeitPermissions;
        public static Permissions CollectPermissions => session.RoomState.CollectPermissions;

        public static string ConnectionId => session.ConnectionInfo.Uuid;

        public static string SeedString => session.RoomState.Seed;

        public static DeathLinkService GetDeathLinkService() => session.CreateDeathLinkService();

        public static string GetCurrentPlayerName() => session.Players.GetPlayerAliasAndName(session.ConnectionInfo.Slot);

        public static LocationCheckHelper LocationCheckHelper => session.Locations;

        public static DataStorageHelper DataStorage => session.DataStorage;

        public static int Slot => session.ConnectionInfo.Slot;
        public static int Team => session.ConnectionInfo.Team;

        private static RichTextBox serverMessageBox;

        public static string[,] storagePlacementsArray;

        public Archipelago_Client()
        {
            InitializeComponent();
            serverMessageBox = ServerMessageBox;
        }

        public static LoginResult Connect(string server, string user, string pass = null, string connectionId = null)
        {
            if (IsConnected && session.Socket.Connected && cachedConnectionResult != null)
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

                var result = session.TryConnectAndLogin("Shivers", userName, ItemsHandlingFlags.AllItems, password: password, requestSlotData: true);


                IsConnected = result.Successful;
                cachedConnectionResult = result;


                if (IsConnected)
                {
                    //Grab Pot placement data
                    var jsonObject = (((LoginSuccessful)result).SlotData);
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
                IsConnected = false;
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
                foreach (var line in e.StackTrace.Split('\n'))
                    richTextBox.AppendText($"    {line}" + Environment.NewLine);
            });


            
        }
        public static void Disconnect()
        {
            session?.Socket.DisconnectAsync();

            serverUrl = null;
            userName = null;
            password = null;

            IsConnected = false;

            session = null;

            cachedConnectionResult = null;
        }
        /*
        public static NetworkItem? GetNextItem(int currentIndex) =>
            session.Items.AllItemsReceived.Count > currentIndex
                ? session.Items.AllItemsReceived[currentIndex]
                : default(NetworkItem?);
        */

        public static void SetStatus(ArchipelagoClientState status) => SendPacket(new StatusUpdatePacket { Status = status });


        static void OnMessageReceived(LogMessage message, RichTextBox richTextBox)
        {
            var parts = message.Parts.Select(p => new Part(p.Text, FromDrawingColor(p.Color))).ToArray();

            richTextBox.Dispatcher.Invoke(() =>
            {
                foreach (Part part in parts)
                {
                    System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb((byte)part.Color.Alpha, (byte)part.Color.Red, (byte)part.Color.Green, (byte)part.Color.Blue);
                    System.Windows.Media.Brush brush = new SolidColorBrush(color);

                    TextRange range = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd)
                    {
                        Text = part.Text
                    };
                    range.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                }
                richTextBox.AppendText(Environment.NewLine);
            });
        }
        static Color FromDrawingColor(MessagePartColor drawingColor) => new Color(drawingColor.R, drawingColor.G, drawingColor.B, 255);

        public class Part
        {
            public string Text { get; set; }
            public Color Color { get; set; }

            public Part(string text, Color color)
            {
                Text = text;
                Color = color;
            }
        }



        public class Color
        {
            public int Red { get; set; }
            public int Green { get; set; }
            public int Blue { get; set; }
            public int Alpha { get; set; }

            public Color(int red, int green, int blue, int alpha)
            {
                Red = red;
                Green = green;
                Blue = blue;
                Alpha = alpha;
            }
        }

        /*
        static void OnMessageReceived(LogMessage message, TextBox textBox)
        {
            textBox.Dispatcher.Invoke(() =>
            {
                textBox.Text += message.ToString() + Environment.NewLine;
            });
        }
        */
        static void SendPacket(ArchipelagoPacketBase packet) => session?.Socket?.SendPacket(packet);

        public static void Say(string message) => SendPacket(new SayPacket { Text = message });

        static bool IsMe(int slot) => slot == session.ConnectionInfo.Slot;
        /*
        public static void UpdateChecks(ItemLocationMap itemLocationMap) =>
            Task.Factory.StartNew(() => { UpdateChecksTask(itemLocationMap); });

        static void UpdateChecksTask(ItemLocationMap itemLocationMap)
        {
            var locations = itemLocationMap
                .Where(l => l.IsPickedUp && !(l is ExternalItemLocation))
                .Select(l => LocationMap.GetLocationId(l.Key))
                .ToArray();

            ReconnectIfNeeded();

            session.Locations.CompleteLocationChecks(locations);
        }
        */
        static void ReconnectIfNeeded()
        {
            if (IsConnected && session.Socket.Connected)
                return;

            Connect(serverUrl, userName, password, session.ConnectionInfo.Uuid);
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
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

                if (!IsConnected)
                {
                    buttonConnect.Content = "Connect";
                }
            }
        }

        public void sendCheck(int checkID)
        {
            session.Locations.CompleteLocationChecks(checkID);
        }

        public List<int> GetItemsFromArchipelagoServer()
        {
            List<int> itemList = new List<int>();

            foreach (NetworkItem item in session.Items.AllItemsReceived)
            {
                itemList.Add((int)item.Item);
            }

            return itemList;
        }

        public void send_completion()
        {
            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            session.Socket.SendPacket(statusUpdatePacket);
        }

        public List<long> GetLocationsCheckedArchipelagoServer()
        {
            return session.Locations.AllLocationsChecked.ToList();
        }

        public void SaveData(string key, int value)
        {
            session.DataStorage[Scope.Slot, key] = value;
        }

        public int LoadData(string key)
        {
            return session.DataStorage[Scope.Slot, key];
        }

        private bool userHasScrolledUp;
        private void ServerMessageBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset < e.ExtentHeight - e.ViewportHeight)
            {
                userHasScrolledUp = true;
            }
            else
            {
                userHasScrolledUp = false;
            }
        }
        private void ServerMessageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!userHasScrolledUp)
            {
                ServerMessageBox.ScrollToEnd();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
            MainWindow.isArchipelagoClientOpen = false;
        }
    }
}
