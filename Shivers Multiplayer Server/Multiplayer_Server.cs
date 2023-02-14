// A C# Program for Server
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Shivers_Multiplayer_Server;

class Multiplayer_Server
{
    private static int[] PotLocations = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static string flagset;
    private static string seed;
    private static bool startingInfoAlreadyReceived;
    private static string lastConsoleMessage;
    private static int ixupiCaptureList;
    private static int[] skullDial = new int[] { 0, 0, 0, 0, 0, 0 };

    public static List<Socket> handlerList = new();


    //Constructor
    public Multiplayer_Server()
    {

    }

    // Main Method
    public static int Main(string[] args)
    {
        StartListening();
        return 0;
    }




    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new();
    }

    // Thread signal.
    public static ManualResetEvent allDone = new(false);

    public static void StartListening()
    {
        // Establish the local endpoint for the socket. 
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new(IPAddress.Any, 11000);

        // Creation TCP/IP Socket using
        Socket listener = new(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {

            // Using Bind() method we associate a network address to the Server Socket
            // All client that will connect to this Server Socket must know this network Address
            listener.Bind(localEndPoint);

            // Using Listen() method we create the Client list that will want to connect to Server
            listener.Listen(100);

            Console.WriteLine("Waiting for a connection on port 11000...");

            while (true)
            {
                //Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\nPress ENTER to continue...");
        Console.Read();

    }

    
    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);


        // Create the state object.
        StateObject state = new()
        {
            workSocket = handler
        };

        //Add handler to a list
        handlerList.Add(handler);


        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
    }
    
    public static void ReadCallback(IAsyncResult ar)
    {
        byte[] bytes = new Byte[1024];
        String content = String.Empty;



        // Retrieve the state object and the handler socket from the asynchronous state object.
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(ar);
        
        if (IsSocketConnected(handler) && bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read more data.
            content = state.sb.ToString();
            if (content.EndsWith("<EOF>"))
            {
                List<string> stringReceivedList = new();

                //Check if more then 1 message has been received from server, if so add it to a queue
                foreach (Match m in Regex.Matches(content, "<EOF>").Cast<Match>())
                {
                    stringReceivedList.Add(content.Substring(0, content.IndexOf("<EOF>") + 5));
                    content = content.Substring(content.IndexOf("<EOF>") + 5, content.Length - content.IndexOf("<EOF>") - 5);
                }


                foreach (string stringReceivedToParsed in stringReceivedList)
                {
                    //Trim off "<EOF>"
                    string stringReceivedParsed = stringReceivedToParsed.Substring(0, stringReceivedToParsed.Length - 5);

                    if (stringReceivedParsed.StartsWith("Starting Pots:"))
                    {
                        //if (!startingInfoAlreadyReceived)
                        {
                            //Clean up string and then parse
                            stringReceivedParsed = stringReceivedParsed.Substring(14, stringReceivedParsed.Length - 14);
                            stringReceivedParsed = stringReceivedParsed.Substring(0, stringReceivedParsed.Length - 1);

                            string s = stringReceivedParsed;
                            string[] valuesstring = s.Split(',');
                            for (int i = 0; i < 23; i++)
                            {
                                PotLocations[i] = int.Parse(valuesstring[i]);
                            }
                            SendConsole("Starting pots received.");
                            Send(handler, "Starting pots received!");
                            
                        }
                        //else
                        {
                            //Send(handler, "Starting info already received!");
                        }

                    }
                    else if (stringReceivedParsed.StartsWith("Flagset:"))
                    {
                        if (!startingInfoAlreadyReceived)
                        {
                            //Clean up string and then parse
                            stringReceivedParsed = stringReceivedParsed.Substring(8, stringReceivedParsed.Length - 8);
                            if (stringReceivedParsed.Length == 0)
                            {
                                SendConsole("Flagset received: None");
                                Send(handler, "Flagset received: None");
                            }
                            else
                            {
                                SendConsole("Flagset received: " + stringReceivedParsed);
                                Send(handler, "Flagset received: " + stringReceivedParsed);
                            }
                            flagset = stringReceivedParsed;
                        }
                        else
                        {
                            Send(handler, "Starting info already received!");
                        }


                    }
                    else if (stringReceivedParsed.StartsWith("Seed:"))
                    {
                        if (!startingInfoAlreadyReceived)
                        {
                            //Clean up string and then parse
                            stringReceivedParsed = stringReceivedParsed.Substring(5, stringReceivedParsed.Length - 5);

                            seed = stringReceivedParsed;
                            SendConsole("Seed received: " + stringReceivedParsed);
                            Send(handler, "Seed received: " + stringReceivedParsed);
                            startingInfoAlreadyReceived = true;
                        }
                        else
                        {
                            Send(handler, "Starting info already received!");
                        }


                    }
                    else if (stringReceivedParsed.StartsWith("Sync Pot:"))
                    {
                        //Clean up string and then parse
                        stringReceivedParsed = stringReceivedParsed.Substring(9, stringReceivedParsed.Length - 9);
                        string[] valuesstring = stringReceivedParsed.Split(',');
                        int[] valuesint = { int.Parse(valuesstring[0]), int.Parse(valuesstring[1]) };
                        PotLocations[valuesint[0]] = valuesint[1];


                        //Send out sync to all connected clients
                        SendConsole($"Sync request for pot {valuesint[0]},{valuesint[1]}");
                        SendAll(handlerList, "Sync Pot:" + stringReceivedParsed);
                    }
                    else if (stringReceivedParsed.StartsWith("Request Pot List"))
                    {
                        //Build pot list string
                        string stringToSend = "Current Pot List:";
                        for (int i = 0; i < 23; i++)
                        {
                            if (i != 22)
                            {
                                stringToSend += (PotLocations[i].ToString() + ",");
                            }
                            else
                            {
                                stringToSend += PotLocations[i].ToString();
                            }
                        }

                        if(lastConsoleMessage.StartsWith("Sync request for pot "))
                        {
                            SendConsole("Pot List Sent");
                        }
                        
                        Send(handler, stringToSend);

                    }
                    else if (stringReceivedParsed.StartsWith("Solved:"))
                    {

                    }
                    else if (stringReceivedParsed.StartsWith("Captured:"))
                    {
                        //Clean up string and then parse
                        stringReceivedParsed = stringReceivedParsed.Substring(9, stringReceivedParsed.Length - 9);
                        int valueint = int.Parse(stringReceivedParsed);

                        //Set the Kth Bit
                        ixupiCaptureList = SetKthBit(ixupiCaptureList, valueint, true);

                        //Send out sync to all connected clients
                        SendConsole("Sync request for Ixupi Capture");
                        SendAll(handlerList, "Current Captured List:" + ixupiCaptureList);
                    }
                    else if (stringReceivedParsed.StartsWith("Request Ixupi Captured List"))
                    {
                        SendAll(handlerList, "Current Captured List:" + ixupiCaptureList);
                    }
                    else if (stringReceivedParsed.StartsWith("Skull Dial: "))
                    {
                        //Clean up string and then parse
                        stringReceivedParsed = stringReceivedParsed.Substring(12, stringReceivedParsed.Length - 12);

                        int dial = int.Parse(stringReceivedParsed.Substring(0,1));
                        int color = int.Parse(stringReceivedParsed.Substring(2, 1));

                        skullDial[dial] = color;

                        //Send skull data to clients
                        SendConsole($"Sync request for Skull Dial:{dial},{color}");
                        SendAll(handlerList, $"Sync Skull Dial:{dial},{color}");

                    }
                    else if (stringReceivedParsed.StartsWith("Test Connection"))
                    {
                        SendConsole($"Client connected ({handler.RemoteEndPoint})");
                        Send(handler, "Succesfully connected to Server");
                    }
                    else
                    {
                        Send(handler, "Unknown data received");
                    }
                }

                state.sb.Clear();
                state.buffer = new byte[1024];
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            else
            {
                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
        else if(!IsSocketConnected(handler))
        {
            //Socket is disconnected
            handler.Close();
            handlerList.Remove(handler);
        }

    }

    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data + "<EOF>");

        // Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
    }

    private static void SendAll(List<Socket> handlerList, string data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data + "<EOF>");

        // Begin sending the data to the remote device.
        foreach (Socket handler in handlerList)
        {
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
            //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static bool IsSocketConnected(Socket s)
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

    private static void SendConsole(string message)
    {
        lastConsoleMessage = message;
        Console.WriteLine(message);
    }
    public static bool IsKthBitSet(int n, int k)
    {
        return (n & (1 << k)) > 0;
    }

    //Sets the kth bit of a value. 0 indexed
    public static int SetKthBit(int value, int k, bool set)
    {
        if (set)//ON
        {
            value |= (1 << k);
        }
        else//OFF
        {
            value &= ~(1 << k);
        }

        return value;
    }
}