using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Shivers_Randomizer
{
    /// <summary>
    /// Interaction logic for Multiplayer_Server.xaml
    /// </summary>
    public partial class Multiplayer_Client : Window
    {
        int port;
        public bool serverResponded;
        public bool multiplayerEnabled;
        public int[] syncPiece = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public int[,] syncPiece2D = new int[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 },
                                                { 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 },{ 0, 0 }};
        public int ixupiCapture;
        public bool syncIxupi;
        public int[] skullDials = new int[] { 0, 0, 0, 0, 0, 0 };



        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();

        public Multiplayer_Client()
        {
            InitializeComponent();

        }

        void Multiplayer_Server_Closing(object sender, EventArgs e)
        {
            //If window is closed, close the socket connection.
            if (IsSocketConnected(socketConnection))
            {
                disconnectSocket();
            }

        }

        Socket socketConnection;
        void ExecuteClient()
        {

            try
            {

                // Establish the remote endpoint for the socket. This example uses port 11000 on the local computer.
                IPAddress ipAddr = null;
                if (this.serverIP.Text == "localhost")
                {
                    ipAddr = IPAddress.Loopback;
                }
                else
                {
                    IPAddress.TryParse(this.serverIP.Text, out ipAddr);
                }

                int.TryParse(this.serverPort.Text, out port);

                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);

                // Creation TCP/IP Socket using Socket Class Constructor
                socketConnection = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {

                    // Connect Socket to the remote endpoint using method Connect()
                    socketConnection.Connect(localEndPoint);

                    this.buttonConnect.Content = "Disconnect";

                    // We print EndPoint information that we are connected
                    WriteToChat("Socket connected to " + socketConnection.RemoteEndPoint.ToString());

                    // Creation of message that we will send to Server
                    byte[] messageSent = Encoding.ASCII.GetBytes("Test Connection<EOF>");
                    int byteSent = socketConnection.Send(messageSent);

                    // Data buffer
                    byte[] messageReceived = new byte[1024];

                    // We receive the message using the method Receive(). 
                    //This method returns number of bytes received, that we'll use to convert them to string
                    //int byteRecv = socketConnection.Receive(messageReceived);
                    //WriteToChat("Successfuly connected to Server");


                }

                // Manage of Socket's Exceptions
                catch (ArgumentNullException ane)
                {

                    WriteToChat("ArgumentNullException:" + ane.ToString());
                }

                catch (SocketException se)
                {

                    WriteToChat("SocketException:" + se.ToString());
                }

                catch (Exception e)
                {
                    WriteToChat("Unexpected exception:" + e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }

        private void WriteToChat(string message)
        {
            chatBox.AppendText("\n" + message);
            chatBox.ScrollToEnd();
        }


        DispatcherTimer timer = new DispatcherTimer();
        public void DispatcherTimer()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (IsSocketConnected(socketConnection))
            {
                multiplayerEnabled = true;

                // see if message from server
                // Data buffer
                byte[] messageReceived = new byte[1024];

                //We receive the message using the method Receive(). 
                //This method returns number of bytes received, that we'll use to convert them to string
                if (socketConnection.Available > 0)
                {
                    int byteRecv = socketConnection.Receive(messageReceived);
                    string stringReceived = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);
                    if(stringReceived.EndsWith("<EOF>"))
                    {
                        
                        List<string> stringReceivedList = new List<string>();
                        
                        //Check if more then 1 message has been received from server, if so add it to a queue
                        foreach (Match m in Regex.Matches(stringReceived, "<EOF>"))
                        {
                            stringReceivedList.Add(stringReceived.Substring(0,stringReceived.IndexOf("<EOF>") + 5));
                            stringReceived = stringReceived.Substring(stringReceived.IndexOf("<EOF>") + 5, stringReceived.Length - stringReceived.IndexOf("<EOF>") - 5);
                        }
                        
                        foreach (string stringReceivedToParsed in stringReceivedList)
                        {
                            //Sync pot request received from server
                            //Trim off "<EOF>"
                            string stringReceivedParsed = stringReceivedToParsed.Substring(0, stringReceivedToParsed.Length - 5);
                            if (stringReceivedParsed.StartsWith("Sync Pot:"))
                            {
                                WriteToChat("Server: " + stringReceivedParsed.Substring(0, stringReceivedParsed.Length));
                                //Clean up string and then parse
                                stringReceivedParsed = stringReceivedParsed.Substring(9, stringReceivedParsed.Length - 9);
                                string[] valuesstring = stringReceivedParsed.Split(',');
                                int[] valuesint = { int.Parse(valuesstring[0]), int.Parse(valuesstring[1]) };
                                syncPiece2D[valuesint[0], 0] = 1;
                                syncPiece2D[valuesint[0], 1] = valuesint[1];
                            }
                            if (stringReceivedParsed.StartsWith("Current Pot List:"))
                            {
                                //Clean up string and then parse
                                stringReceivedParsed = stringReceivedParsed.Substring(17, stringReceivedParsed.Length - 17);
                                string[] valuesstring = stringReceivedParsed.Split(',');
                                for (int i = 0; i < 23; i++)
                                {
                                    syncPiece[i] = int.Parse(valuesstring[i]);
                                }
                            }
                            if (stringReceivedParsed.StartsWith("Current Captured List:"))
                            {
                                WriteToChat("Received Current Captured List");
                                //Clean up string and then parse
                                stringReceivedParsed = stringReceivedParsed.Substring(22, stringReceivedParsed.Length - 22);
                                int valueint = int.Parse(stringReceivedParsed);

                                ixupiCapture = valueint;
                            }
                            if (stringReceivedParsed.StartsWith("Sync Skull Dial:"))
                            {
                                
                                //Clean up string and then parse
                                stringReceivedParsed = stringReceivedParsed.Substring(16, stringReceivedParsed.Length - 16);

                                int dial = int.Parse(stringReceivedParsed.Substring(0, 1));
                                int color = int.Parse(stringReceivedParsed.Substring(2, 1));

                                WriteToChat($"Sync Skull:{dial},{color}");
                                skullDials[dial] = color;
                            }

                            
                        }
                        serverResponded = true;
                    }
                }
            }
            else
            {
                multiplayerEnabled = false;
                timer.Stop();
                timer.Tick -= timer_Tick;
                WriteToChat("Disconnected");
                this.buttonConnect.Content = "Connect";
            }
        }

        private void disconnectSocket()
        {
            socketConnection.Shutdown(SocketShutdown.Both);
            socketConnection.Close();
            socketConnection = null;
        }

        private bool IsSocketConnected(Socket s)
        {
            if (s != null)
            {
                return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
            }
            else
            {
                return false;
            }

        }

        public void sendServerStartingPots(int[] locations)
        {
            if (IsSocketConnected(socketConnection))
            {
                string messageString = ("Starting Pots:");
                for (int i = 0; i < 23; i++)
                {
                    messageString = messageString + locations[i] + ",";
                }
                sendServerMessage(messageString);
            }
        }

        public void sendServerFlagset(string flagset)
        {
            if (IsSocketConnected(socketConnection))
            {
                sendServerMessage("Flagset:" + flagset);
            }
        }

        public void sendServerSeed(int seed)
        {
            if (IsSocketConnected(socketConnection))
            {
                sendServerMessage("Seed:" + seed.ToString());
            }
        }

        public void sendServerMessage(string messageString)
        {
            serverResponded = false;
            messageString = messageString + "<EOF>";
            byte[] messageStringConverted = Encoding.ASCII.GetBytes(messageString);
            int byteSent = socketConnection.Send(messageStringConverted);
        }

        public void sendServerPotUpdate(int location, int pieceNumber)
        {
            if(IsSocketConnected(socketConnection))
            {
                sendServerMessage("Sync Pot:" + location.ToString() + "," + pieceNumber.ToString());
            }
        }

        public void sendServerRequestPotList()
        {
            if(IsSocketConnected(socketConnection))
            {
                sendServerMessage("Request Pot List");
            }
        }

        public void sendServerIxupiCaptured(int ixupiCaptureValue)
        {
            if (IsSocketConnected(socketConnection))
            {
                sendServerMessage("Captured:" + ixupiCaptureValue.ToString());
            }

        }
        public void sendServerRequestIxupiCapturedList()
        {
            if (IsSocketConnected(socketConnection))
            {
                sendServerMessage("Request Ixupi Captured List");
            }
        }

        public void sendServerSkullDial(int dial, int color)
        {
            if (IsSocketConnected(socketConnection))
            {
                sendServerMessage($"Skull Dial: {dial},{color}");
            }
        }
        

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSocketConnected(socketConnection))
            {
                ExecuteClient();
                DispatcherTimer();
            }
            else
            {
                // Close Socket using the method Close()
                disconnectSocket();
            }

        }
    }
}
