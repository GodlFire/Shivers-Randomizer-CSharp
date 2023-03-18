using Shivers_Randomizer.room_randomizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using static Shivers_Randomizer.utils.AppHelpers;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const int POT_BOTTOM_OFFSET = 200;
    private const int POT_TOP_OFFSET = 210;
    private const int POT_FULL_OFFSET = 220;
    private readonly int[] EXTRA_LOCATIONS = { (int)PotLocation.LIBRARY_CABINET, (int)PotLocation.EAGLE_NEST, (int)PotLocation.SHAMAN_HUT };

    public MainWindow mainWindow;
    public Overlay overlay;
    public Multiplayer_Client? multiplayer_Client = null;// new Multiplayer_Client();

    private RectSpecial ShiversWindowDimensions = new();

    public UIntPtr processHandle;
    public UIntPtr MyAddress;
    public UIntPtr hwndtest;
    public bool? AddressLocated = null;

    public bool scrambling = false;
    public int Seed;
    public bool setSeedUsed;
    private Random rng;
    public int ScrambleCount;
    public List<int> Locations = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    public int roomNumber;
    public int roomNumberPrevious;
    public int numberIxupiCaptured;
    public int numberIxupiCapturedTemp;
    public int firstToTheOnlyXNumber;
    public bool finalCutsceneTriggered;
    private bool useFastTimer;
    private bool elevatorUndergroundSolved;
    private bool elevatorBedroomSolved;
    private bool elevatorThreeFloorSolved;
    private int elevatorSolveCountPrevious;
    private int multiplayerSyncCounter;
    private bool multiplayerScreenRedrawNeeded;
    

    public bool settingsVanilla;
    public bool settingsIncludeAsh;
    public bool settingsIncludeLightning;
    public bool settingsEarlyBeth;
    public bool settingsExtraLocations;
    public bool settingsExcludeLyre;
    public bool settingsSolvedLyre;
    public bool settingsEarlyLightning;
    public bool settingsRedDoor;
    public bool settingsFullPots;
    public bool settingsFirstToTheOnlyFive;
    public bool settingsRoomShuffle;
    public bool settingsIncludeElevators;
    public bool settingsMultiplayer;
    public bool settingsOnly4x4Elevators;
    public bool settingsElevatorsStaySolved;

    public bool currentlyTeleportingPlayer = false;
    public RoomTransition? lastTransitionUsed;

    public bool disableScrambleButton;
    public int[] multiplayerLocations = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
    public bool[] multiplayerIxupi = new[] { false, false, false, false, false, false, false, false, false, false };
    public int[] ixupiLocations = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    public bool currentlyRunningThreadOne = false;
    public bool currentlyRunningThreadTwo = false;

    public RoomTransition[] roomTransitions = Array.Empty<RoomTransition>();
    private AttachPopup scanner = new AttachPopup();

    List<Tuple<int, UIntPtr>> scriptsFound = new List<Tuple<int, UIntPtr>>();
    List<int> completeScriptList = new List<int>();
    bool scriptsLocated = false;
    bool scriptAlreadyModified = false;



    public App()
    {
        mainWindow = new MainWindow(this);
        overlay = new Overlay(this);
        rng = new Random();
        mainWindow.Show();
    }

    public void Scramble()
    {
        scrambling = true;
        mainWindow.button_Scramble.IsEnabled = false;

        if (multiplayer_Client != null)
        {
            settingsMultiplayer = multiplayer_Client.multiplayerEnabled;
        }

        //Check if seed was entered
        if (mainWindow.txtBox_Seed.Text != "")
        {
            //check if seed is too big, if not use it
            if (!int.TryParse(mainWindow.txtBox_Seed.Text, out Seed))
            {
                ScrambleFailure("Seed was not less then 2,147,483,647. Please try again with a smaller number.");
                return;
            }
            setSeedUsed = true;
        }
        else
        {
            setSeedUsed = false;
            //if not seed entered, seed to the system clock
            Seed = (int)DateTime.Now.Ticks;

        }

        //If not a set seed, hide the system clock seed number so that it cant be used to cheat (unlikely but what ever)
        Random rngHidden = new(Seed);
        
        if (!setSeedUsed)
        {
            Seed = rngHidden.Next();
        }
        rng = new(Seed);

        //If early lightning then set flags for timer
        finalCutsceneTriggered = false;

        //Reset elevator flags
        elevatorUndergroundSolved = false;
        elevatorBedroomSolved = false;
        elevatorThreeFloorSolved = false;

    Scramble:
        Locations = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        //If Vanilla is selected then use the vanilla placement algorithm
        if (settingsVanilla)
        {
            Locations[(int)PotLocation.DESK] = (int)IxupiPots.ASH_TOP;
            Locations[(int)PotLocation.SLIDE] = (int)IxupiPots.ELETRICITY_TOP;
            Locations[(int)PotLocation.PLANTS] = (int)IxupiPots.ASH_BOTTOM;
            VanillaPlacePiece((int)IxupiPots.WATER_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.WAX_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.OIL_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.CLOTH_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.WOOD_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.CRYSTAL_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.ELETRICITY_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.SAND_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.METAL_BOTTOM, rng);
            VanillaPlacePiece((int)IxupiPots.WATER_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.WAX_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.OIL_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.CLOTH_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.WOOD_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.CRYSTAL_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.SAND_TOP, rng);
            VanillaPlacePiece((int)IxupiPots.METAL_TOP, rng);
        }
        else if (!settingsFirstToTheOnlyFive) //Normal Scramble
        {
            List<int> PiecesNeededToBePlaced = new();
            List<int> PiecesRemainingToBePlaced = new();
            int numberOfRemainingPots = 20;
            int numberOfFullPots = 0;

            //Check if ash is added to the scramble
            if (!settingsIncludeAsh)
            {
                Locations[(int)PotLocation.DESK] = (int)IxupiPots.ASH_TOP;
                Locations[(int)PotLocation.PLANTS] = (int)IxupiPots.ASH_BOTTOM;
                numberOfRemainingPots -= 2;
            }
            //Check if lighting is added to the scramble
            if (!settingsIncludeLightning)
            {
                Locations[(int)PotLocation.SLIDE] = (int)IxupiPots.ELETRICITY_TOP;
                numberOfRemainingPots -= 1;
            }

            if (settingsFullPots)
            {
                if (settingsExcludeLyre && !settingsExtraLocations)
                {   //No more then 8 since ash/lighitng will be rolled outside of the count
                    numberOfFullPots = rng.Next(1, 9);//Roll how many completed pots. If no lyre and no extra locations you must have at least 1 completed to have room.
                }
                else
                {
                    numberOfFullPots = rng.Next(0, 9);//Roll how many completed pots
                }

                int FullPotRolled;
                for (int i = 0; i < numberOfFullPots; i++)
                {
                RollFullPot:
                    FullPotRolled = rng.Next(POT_FULL_OFFSET, POT_FULL_OFFSET + 10);//Grab a random pot
                    if (FullPotRolled == (int)IxupiPots.ASH_FULL || FullPotRolled == (int)IxupiPots.ELETRICITY_FULL)//Make sure its not ash or lightning
                    {
                        goto RollFullPot;
                    }

                    if (PiecesNeededToBePlaced.Contains(FullPotRolled))//Make sure it wasnt already selected
                    {
                        goto RollFullPot;
                    }
                    PiecesNeededToBePlaced.Add(FullPotRolled);
                    numberOfRemainingPots -= 2;
                }
                if (rng.Next(0, 2) == 1 && settingsIncludeAsh) //Is ash completed
                {
                    PiecesNeededToBePlaced.Add((int)IxupiPots.ASH_FULL);
                    numberOfRemainingPots -= 2;
                }
                if (rng.Next(0, 2) == 1 && settingsIncludeLightning) //Is lighting completed
                {
                    PiecesNeededToBePlaced.Add((int)IxupiPots.ELETRICITY_FULL);
                    numberOfRemainingPots -= 2;
                }
            }

            int pieceBeingAddedToList; //Add remaining peices to list
            while (numberOfRemainingPots != 0)
            {
                pieceBeingAddedToList = rng.Next(0, 20) + POT_BOTTOM_OFFSET;
                //Check if piece already added to list
                //Check if piece was ash and ash not included in scramble
                //Check if piece was lighting top and lightning not included in scramble
                if (PiecesNeededToBePlaced.Contains(pieceBeingAddedToList) ||
                    !settingsIncludeAsh && (pieceBeingAddedToList == (int)IxupiPots.ASH_BOTTOM || pieceBeingAddedToList == (int)IxupiPots.ASH_TOP) ||
                    !settingsIncludeLightning && pieceBeingAddedToList == (int)IxupiPots.ELETRICITY_TOP)
                {
                    continue;
                }
                //Check if completed pieces are used and the base pieces are rolled
                if ((pieceBeingAddedToList < POT_TOP_OFFSET && PiecesNeededToBePlaced.Contains(pieceBeingAddedToList + 20)) ||
                    (pieceBeingAddedToList >= POT_TOP_OFFSET && PiecesNeededToBePlaced.Contains(pieceBeingAddedToList + 10)))
                {
                    continue;
                }
                PiecesNeededToBePlaced.Add(pieceBeingAddedToList);
                numberOfRemainingPots -= 1;
            }

            int RandomLocation;
            PiecesRemainingToBePlaced = new List<int>(PiecesNeededToBePlaced);
            while (PiecesRemainingToBePlaced.Count > 0)
            {
                RandomLocation = rng.Next(0, 23);
                if (!settingsExtraLocations && EXTRA_LOCATIONS.Contains(RandomLocation)) //Check if extra locations are used
                {
                    continue;
                }
                if (settingsExcludeLyre && RandomLocation == (int)PotLocation.LYRE)//Check if lyre excluded
                {
                    continue;
                }
                if (Locations[RandomLocation] != 0) //Check if location is filled
                {
                    continue;
                }
                Locations[RandomLocation] = PiecesRemainingToBePlaced[0];
                PiecesRemainingToBePlaced.RemoveAt(0);
            }

            //Check for bad scramble
            //Check if oil behind oil
            //Check if cloth behind cloth
            //Check if oil behind cloth AND cloth behind oil
            int[] oil = { (int)IxupiPots.OIL_BOTTOM, (int)IxupiPots.OIL_TOP, (int)IxupiPots.OIL_FULL };
            int[] cloth = { (int)IxupiPots.CLOTH_BOTTOM, (int)IxupiPots.CLOTH_TOP, (int)IxupiPots.CLOTH_FULL };
            if (oil.Contains(Locations[(int)PotLocation.TAR_RIVER]) ||
                cloth.Contains(Locations[(int)PotLocation.BATHROOM]) ||
                oil.Contains(Locations[(int)PotLocation.BATHROOM]) && cloth.Contains(Locations[(int)PotLocation.TAR_RIVER]))
            {
                goto Scramble;
            }
        }
        else if (settingsFirstToTheOnlyFive) //First to the only X
        {
            List<int> PiecesNeededToBePlaced = new();
            List<int> PiecesRemainingToBePlaced = new();

            //Get number of sets
            firstToTheOnlyXNumber = int.Parse(mainWindow.txtBox_FirstToTheOnlyX.Text);
            int numberOfRemainingPots = 2 * firstToTheOnlyXNumber;

            //Check for invalid numbers
            if (numberOfRemainingPots == 0) //No Sets
            {
                ScrambleFailure("Number of Ixupi must be greater than 0.");
                return;
            }
            else if (numberOfRemainingPots == 2 && !settingsIncludeAsh && !settingsIncludeLightning)
            {
                ScrambleFailure("If selecting 1 pot set you must include either lighting or ash into the scramble.");
                return;
            }

            //If 1 set and either IncludeAsh/IncludeLighting is false then force the other. Else roll randomly from all available pots
            if (numberOfRemainingPots == 2 && (settingsIncludeAsh | settingsIncludeLightning))
            {
                if (!settingsIncludeAsh)//Force lightning
                {
                    PiecesNeededToBePlaced.Add((int)IxupiPots.ELETRICITY_BOTTOM);
                    Locations[(int)PotLocation.SLIDE] = (int)IxupiPots.ELETRICITY_TOP;
                }
                else if (!settingsIncludeLightning)//Force Ash
                {
                    Locations[(int)PotLocation.DESK] = (int)IxupiPots.ASH_TOP;
                    Locations[(int)PotLocation.PLANTS] = (int)IxupiPots.ASH_BOTTOM;
                }
            }
            else
            {
                List<Ixupi> SetsAvailable = Enum.GetValues<Ixupi>().ToList();

                //Determine which sets will be included in the scramble
                //First check if lighting/ash are included in the scramble. if not force them
                if (!settingsIncludeAsh)
                {
                    Locations[(int)PotLocation.DESK] = (int)IxupiPots.ASH_TOP;
                    Locations[(int)PotLocation.PLANTS] = (int)IxupiPots.ASH_BOTTOM;
                    numberOfRemainingPots -= 2;
                    SetsAvailable.Remove(Ixupi.ASH);
                }
                if (!settingsIncludeLightning)
                {
                    PiecesNeededToBePlaced.Add((int)IxupiPots.ELETRICITY_BOTTOM);
                    Locations[(int)PotLocation.SLIDE] = (int)IxupiPots.ELETRICITY_TOP;
                    numberOfRemainingPots -= 2;
                    SetsAvailable.Remove(Ixupi.ELETRICITY);
                }

                //Next select from the remaining sets available
                while (numberOfRemainingPots > 0)
                {
                    int setSelected = rng.Next(0, SetsAvailable.Count);
                    Ixupi ixupiSelected = SetsAvailable[setSelected];
                    //Check/roll for full pot
                    if (settingsFullPots && rng.Next(0, 2) == 1)
                    {
                        PiecesNeededToBePlaced.Add((int)ixupiSelected + POT_FULL_OFFSET);
                    }
                    else
                    {
                        PiecesNeededToBePlaced.Add((int)ixupiSelected + POT_BOTTOM_OFFSET);
                        PiecesNeededToBePlaced.Add((int)ixupiSelected + POT_TOP_OFFSET);
                    }

                    numberOfRemainingPots -= 2;
                    SetsAvailable.RemoveAt(setSelected);
                }

                int RandomLocation;
                PiecesRemainingToBePlaced = new List<int>(PiecesNeededToBePlaced);
                while (PiecesRemainingToBePlaced.Count > 0)
                {
                    RandomLocation = rng.Next(0, 23);
                    if (!settingsExtraLocations && EXTRA_LOCATIONS.Contains(RandomLocation)) //Check if extra locations are used
                    {
                        continue;
                    }
                    if (settingsExcludeLyre && RandomLocation == (int)PotLocation.LYRE) //Check if lyre excluded
                    {
                        continue;
                    }
                    if (Locations[RandomLocation] != 0) //Check if location is filled
                    {
                        continue;
                    }
                    Locations[RandomLocation] = PiecesRemainingToBePlaced[0];
                    PiecesRemainingToBePlaced.RemoveAt(0);
                }

                //Check for bad scramble
                //Check if oil behind oil
                //Check if cloth behind cloth
                //Check if oil behind cloth AND cloth behind oil
                //Check if a piece behind oil with no oil pot available
                //Check if a piece behind cloth with no cloth pot available
                int[] oil = { (int)IxupiPots.OIL_BOTTOM, (int)IxupiPots.OIL_TOP, (int)IxupiPots.OIL_FULL };
                int[] cloth = { (int)IxupiPots.CLOTH_BOTTOM, (int)IxupiPots.CLOTH_TOP, (int)IxupiPots.CLOTH_FULL };
                if (oil.Contains(Locations[(int)PotLocation.TAR_RIVER]) ||
                    cloth.Contains(Locations[(int)PotLocation.BATHROOM]) ||
                    oil.Contains(Locations[(int)PotLocation.BATHROOM]) && cloth.Contains(Locations[(int)PotLocation.TAR_RIVER]) ||
                    Locations[(int)PotLocation.TAR_RIVER] != 0 && !Locations.Any(pot => oil.Contains(pot)) ||
                    Locations[(int)PotLocation.BATHROOM] != 0 && !Locations.Any(pot => cloth.Contains(pot)))
                {
                    goto Scramble;
                }
            }
        }

        //Place pieces in memory
        PlacePieces();

        //Set bytes for red door, beth, and lyre
        if (!settingsVanilla)
        {
            SetKthBitMemoryOneByte(364, 7, settingsRedDoor);
            SetKthBitMemoryOneByte(381, 7, settingsEarlyBeth);
            SetKthBitMemoryOneByte(365, 0, settingsSolvedLyre);
        }

        //Set ixupi captured number
        if (settingsFirstToTheOnlyFive)
        {
            WriteMemory(1712, 10 - firstToTheOnlyXNumber);
        }
        else//Set to 0 if not running First to The Only X
        {
            WriteMemory(1712, 0);
        }

        if (settingsRoomShuffle)
        {
            roomTransitions = new RoomRandomizer(this, rng).RandomizeMap();
        }

        // Sets crawlspace in lobby
        SetKthBitMemoryOneByte(368, 6, settingsRoomShuffle);

        //Start fast timer for room shuffle
        if (settingsRoomShuffle)
        {
            FastTimer();
            useFastTimer = true;
        }
        else
        {
            useFastTimer = false;
        }

        ScrambleCount += 1;
        mainWindow.label_ScrambleFeedback.Content = $"Scramble Number: {ScrambleCount}";
        overlay.SetInfo();
        mainWindow.label_Flagset.Content = $"Flagset: {overlay.flagset}";

        //Set Seed info and flagset info
        if (setSeedUsed)
        {
            mainWindow.label_Seed.Content = $"Set Seed: {Seed}";
        } else
        {
            mainWindow.label_Seed.Content = $"Seed: {Seed}";
        }

        //-----------Multiplayer------------
        if (settingsMultiplayer && multiplayer_Client != null)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                currentlyRunningThreadOne = true;

                //Disable scramble button till all data is dont being received by server
                disableScrambleButton = true;

                //Send starting pots to server
                multiplayer_Client.sendServerStartingPots(Locations.ToArray());

                //Send starting skulls to server
                for (int i = 0; i < 6; i++)
                {
                    multiplayer_Client.sendServerSkullDial(i, ReadMemory(836 + i * 4, 1));
                }

                //Send starting flagset to server
                multiplayer_Client.sendServerFlagset(overlay.flagset);

                //Send starting seed
                multiplayer_Client.sendServerSeed(Seed);

                //Send starting skull dials to server


                //Reenable scramble button
                disableScrambleButton = false;

                currentlyRunningThreadOne = false;
            }).Start();
        }

        scrambling = false;
        mainWindow.button_Scramble.IsEnabled = true;



        
    }
    
    private UIntPtr testAddress;




    private void ScrambleFailure(string message)
    {
        new Message(message).ShowDialog();
        scrambling = false;
        mainWindow.button_Scramble.IsEnabled = true;
    }

    public void SetFlagset()
    {
        overlay.UpdateFlagset();
        mainWindow.label_Flagset.Content = $"Flagset: {overlay.flagset}";
    }

    public void PlacePieces()
    {
        IEnumerable<(int, int)> potPieces = Locations.Select((potPiece, index) => (potPiece, index));
        foreach(var (potPiece, index) in potPieces)
        {
            WriteMemory(index * 8, potPiece);
        }
    }

    public void DispatcherTimer()
    {
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(1)
        };
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private int fastTimerCounter = 0;
    private int slowTimerCounter = 0;
    public void FastTimer()
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        new Thread(() =>
        {
            while (useFastTimer)
            {
                if (stopwatch.ElapsedMilliseconds >= 4)
                {
                    fastTimerCounter += 1;

                    this.Dispatcher.Invoke(() =>
                    {
                        mainWindow.label_fastCounter.Content = fastTimerCounter;
                    });

                    GetRoomNumber();
                    
                    RoomShuffle();

                    stopwatch.Restart();
                }
            }
        }).Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        slowTimerCounter += 1;
        mainWindow.label_slowCounter.Content = slowTimerCounter;

        var windowExists = GetWindowRect(hwndtest, ref ShiversWindowDimensions);
        overlay.Left = ShiversWindowDimensions.Left;
        overlay.Top = ShiversWindowDimensions.Top + (int)SystemParameters.WindowCaptionHeight;
        overlay.labelOverlay.Foreground = windowExists && IsIconic(hwndtest) ? overlay.brushTransparent : overlay.brushLime;

        if (Seed == 0)
        {
            overlay.labelOverlay.Content = "Not yet randomized";
        }

        //Check if using the fast timer, if not get the room number
        if(!useFastTimer)
        {
            GetRoomNumber();
        }

        if (AddressLocated.HasValue)
        {
            mainWindow.label_ShiversDetected.Content = AddressLocated.Value ? "Shivers Detected! 🙂" : "Shivers not detected! 🙁";
            if (windowExists)
            {
                overlay.Show();
            }
            else
            {
                AddressLocated = false;
                overlay.Hide();
            }
        }
        else
        {
            mainWindow.label_ShiversDetected.Content = "";
        }

        #if DEBUG
                mainWindow.button_Scramble.IsEnabled = true;
        #else
                mainWindow.button_Scramble.IsEnabled = roomNumber == 922 && !scrambling;
        #endif

        //Early lightning
        if (settingsEarlyLightning && !settingsVanilla)
        {
            EarlyLightning();
        }

        //Elevators Stay Solved
        //Only 4x4 elevators.
        ElevatorSettings();

        //---------Multiplayer----------
        
        if (multiplayer_Client != null)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //if (settingsMultiplayer && runThreadIfAvailable && !currentlyRunningThreadTwo && !currentlyRunningThreadOne)
                if (settingsMultiplayer && !currentlyRunningThreadTwo && !currentlyRunningThreadOne)
                {
                    currentlyRunningThreadTwo = true;
                    disableScrambleButton = true;

                    //Request current pot list from server
                    multiplayer_Client.sendServerRequestPotList();

                    //Monitor each location and send a sync update to server if it differs
                    for (int i = 0; i < 23; i++)
                    {
                        int potRead = ReadMemory(i * 8, 1);
                        if (potRead != multiplayerLocations[i])//All locations are 8 apart in the memory so can multiply by i
                        {
                            multiplayerLocations[i] = potRead;
                            multiplayer_Client.sendServerPotUpdate(i, multiplayerLocations[i]);
                        }
                    }

                    //Check if a piece needs synced from another player
                    for (int i = 0; i < 23; i++)
                    {
                        if (ReadMemory(i * 8, 1) != multiplayer_Client.syncPiece[i])  //All locations are 8 apart in the memory so can multiply by i
                        {
                            WriteMemory(i * 8, multiplayer_Client.syncPiece[i]);
                            multiplayerLocations[i] = multiplayer_Client.syncPiece[i];

                            //Force a screen redraw if looking at pot being synced
                            PotSyncRedraw();
                        }
                    }

                    //Check if an ixupi was captured, if so send to the server
                    int ixupiCaptureRead = ReadMemory(-60, 2);

                    for (int i = 0; i < 10; i++)
                    {
                        if(IsKthBitSet(ixupiCaptureRead, i) && multiplayerIxupi[i] == false) //Check if ixupi at specific bit is now set, and if its not set in multiplayerIxupi list
                        {
                            multiplayerIxupi[i] = true;
                            multiplayer_Client.sendServerIxupiCaptured(i);
                        }
                    }

                    //Check what the latest ixupi captured list is and see if a sync needs completed
                    //A list is automatically sent on a capture, this is just backup, only pull a list only once every 10 seconds or so
                    multiplayerSyncCounter += 1;
                    if (multiplayerSyncCounter > 600)
                    {
                        multiplayerSyncCounter = 0;
                        multiplayer_Client.sendServerRequestIxupiCapturedList();
                    }
                    
                    if (ixupiCaptureRead < multiplayer_Client.ixupiCapture)
                    {
                        //Set the ixupi captured
                        WriteMemory(-60, multiplayer_Client.ixupiCapture);

                        //Redraw pots on the inventory bar by setting previous room to the name select
                        multiplayerScreenRedrawNeeded = true;

                        //Remove captured ixupi from the game and count how many have been captured
                        ixupiCaptureRead = multiplayer_Client.ixupiCapture;
                        int multiplayerNumCapturedIxupi = 0;

                        if (IsKthBitSet(ixupiCaptureRead, 0)) //Sand
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.SAND, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 1)) //Crystal
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CRYSTAL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 2)) //Metal
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.METAL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 3)) //Oil
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 4)) //Wood
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WOOD, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 5)) //Lightning
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.LIGHTNING, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 6)) //Ash
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.ASH, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 7)) //Water
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WATER, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 8)) //Cloth
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CLOTH, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                        if (IsKthBitSet(ixupiCaptureRead, 9)) //Wax
                        {
                            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WAX, 0);
                            multiplayerNumCapturedIxupi += 1;
                        }
                    }

                    //Synchronize Skull Dials
                    //If looking at a skull and the value in memory has changed, the player has changed it, send to server
                    int[] skullDialColor =
                    {
                        ReadMemory(836, 1),
                        ReadMemory(840, 1),
                        ReadMemory(844, 1),
                        ReadMemory(848, 1),
                        ReadMemory(852, 1),
                        ReadMemory(856, 1)

                    };
                    switch (roomNumber) //Player has changed a skull dial
                    {
                        case 11330: //Prehistoric
                            if (multiplayer_Client.skullDials[0] != skullDialColor[0])
                            {
                                multiplayer_Client.sendServerSkullDial(0, skullDialColor[0]);
                                multiplayer_Client.skullDials[0] = skullDialColor[0];
                            }
                            break;
                        case 14170: //Tar River
                            if (multiplayer_Client.skullDials[1] != skullDialColor[1])
                            {
                                multiplayer_Client.sendServerSkullDial(1, skullDialColor[1]);
                                multiplayer_Client.skullDials[1] = skullDialColor[1];
                            }
                            break;
                        case 24170: //Werewolf
                            if (multiplayer_Client.skullDials[2] != skullDialColor[2])
                            {
                                multiplayer_Client.sendServerSkullDial(2, skullDialColor[2]);
                                multiplayer_Client.skullDials[2] = skullDialColor[2];
                            }
                            break;
                        case 21400: //Burial
                            if (multiplayer_Client.skullDials[3] != skullDialColor[3])
                            {
                                multiplayer_Client.sendServerSkullDial(3, skullDialColor[3]);
                                multiplayer_Client.skullDials[3] = skullDialColor[3];
                            }
                            break;
                        case 20190: //Egypt
                            if (multiplayer_Client.skullDials[4] != skullDialColor[4])
                            {
                                multiplayer_Client.sendServerSkullDial(4, skullDialColor[4]);
                                multiplayer_Client.skullDials[4] = skullDialColor[4];
                            }
                            break;
                        case 23650: //Gods
                            if (multiplayer_Client.skullDials[5] != skullDialColor[5])
                            {
                                multiplayer_Client.sendServerSkullDial(5, skullDialColor[5]);
                                multiplayer_Client.skullDials[5] = skullDialColor[5];
                            }
                            break;
                    }
                    for (int i = 0; i < 6; i++)//Other player has changed a skull dial
                    {
                        if (multiplayer_Client.skullDials[i] != skullDialColor[i])
                        {
                            WriteMemory(836 + i * 4, multiplayer_Client.skullDials[i]);
                        }
                    }

                    //Check if a screen redraw allowed. 
                    if(multiplayerScreenRedrawNeeded)
                    {
                        //Check if screen redraw allowed
                        bool ScreenRedrawAllowed = CheckScreenRedrawAllowed();
                        if(ScreenRedrawAllowed)
                        {
                            multiplayerScreenRedrawNeeded = false;
                            WriteMemory(-432, 922);
                        }
                    }

                    disableScrambleButton = false;
                    currentlyRunningThreadTwo = false;
                }
            }).Start();
        }

        //Label for ixupi captured number
        numberIxupiCaptured = ReadMemory(1712, 1);
        mainWindow.label_ixupidNumber.Content = numberIxupiCaptured;

        //Label for base memory address
        mainWindow.label_baseMemoryAddress.Content = MyAddress.ToString("X8");







        //Modify Scripts
        if (scriptsLocated == false && processHandle != UIntPtr.Zero)
        {
            //Locate scripts
            LocateAllScripts();
        }

        if (scriptsLocated)
        {

            UIntPtr loadedScriptAddres = UIntPtr.Zero;

            if (roomNumber == 9470)
            {
                if(scriptAlreadyModified == false)
                {
                    //Grab the location of the 9470 script
                    loadedScriptAddres = LoadedScriptAddress(9470);

                    //Write changes to the script
                    WriteMemoryAnyAdress(loadedScriptAddres, 408, 0);

                    //Reload the screen
                    WriteMemory(-432, 990);

                    scriptAlreadyModified = true;
                }
            }
            else
            {
                scriptAlreadyModified = false;
            }
        }



    }



    
    private void GetRoomNumber()
    {
        //Monitor Room Number
        if (MyAddress != (UIntPtr)0x0 && processHandle != (UIntPtr)0x0) //Throws an exception if not checked in release mode.
        {
            int tempRoomNumber = ReadMemory(-424, 2);

            if (tempRoomNumber != roomNumber)
            {
                roomNumberPrevious = roomNumber;
                roomNumber = tempRoomNumber;
            }
            this.Dispatcher.Invoke(() =>
            {
                mainWindow.label_roomPrev.Content = roomNumberPrevious;
                mainWindow.label_room.Content = roomNumber;
            });
        }
    }

    private void PotSyncRedraw()
    {
        //If looking at pot then set the previous room to the menu to force a screen redraw on the pot
        if (roomNumber == 6220 || //Desk Drawer
            roomNumber == 7112 || //Workshop
            roomNumber == 8100 || //Library Cupboard
            roomNumber == 8490 || //Library Statue
            roomNumber == 9420 || //Slide
            roomNumber == 9760 || //Eagle
            roomNumber == 11310 || //Eagles Nest
            roomNumber == 12181 || //Ocean
            roomNumber == 14080 || //Tar River
            roomNumber == 16420 || //Theater
            roomNumber == 19220 || //Green House / Plant Room
            roomNumber == 20553 || //Egypt
            roomNumber == 21070 || //Chinese Solitaire
            roomNumber == 22190 || //Tiki Hut
            roomNumber == 23550 || //Lyre
            roomNumber == 24320 || //Skeleton
            roomNumber == 25050 || //Janitor Closet
            roomNumber == 29080 || //UFO
            roomNumber == 30420 || //Alchemy
            roomNumber == 31310 || //Puzzle Room
            roomNumber == 32570 || //Hanging / Gallows
            roomNumber == 35110    //Clock Tower
            )
        {
            WriteMemory(-432, 990);
        }
        else if (roomNumber == 24380 && IsKthBitSet(ReadMemory(380,1),8))//Anansi and anansi is open
        {
            WriteMemory(-432, 990);
        }
    }

    private bool CheckScreenRedrawAllowed()
    {

        if (roomNumber != 1162 || //Gear Puzzle Combo lock
            roomNumber != 1160 || //Gear Puzzle
            roomNumber != 1214 || //Stone Henge Puzzle
            roomNumber != 2340 || //Generator Panel
            roomNumber != 3500 || //Boat Control Open Water
            roomNumber != 3510 || //Boat Control Shore
            roomNumber != 3260 || //Water attack cutscene on boat
            roomNumber != 931 || //Windelnot Ghost cutscene
            roomNumber != 4630 || //Underground Elevator puzzle bottom
            roomNumber != 6300 || //Underground Elevator puzzle top
            roomNumber != 5010 || //Underground Elevator inside A
            roomNumber != 5030 || //Underground Elevator inside B
            roomNumber != 4620 || //Underground Elevator outside A
            roomNumber != 6290 || //Underground Elevator outside B
            roomNumber != 38130 || //Office Elevator puzzle bottom
            roomNumber != 37360 || //Office Elevator puzzle top
            roomNumber != 38010 || //Office Elevator inside A
            roomNumber != 38011 || //Office Elevator inside B
            roomNumber != 38110 || //Office Elevator outside A
            roomNumber != 37330 || //Office Elevator outside B
            roomNumber != 34010 || //3-Floor Elevator Inside
            roomNumber != 10100 || //3-Floor Elevator outside Floor 1
            roomNumber != 27212 || //3-Floor Elevator outside Floor 2
            roomNumber != 33140 || //3-Floor Elevator outside Floor 3
            roomNumber != 10101 || //3-Floor Elevator Puzzle Floor 1
            roomNumber != 27211 || //3-Floor Elevator Puzzle Floor 2
            roomNumber != 33500 || //3-Floor Elevator Puzzle Floor 3
            roomNumber != 6280 || //Ash fireplace
            roomNumber != 21050 || //Ash Burial
            roomNumber != 21430 || //Cloth Burial
            roomNumber != 20700 || //Cloth Egypt
            roomNumber != 25050 || //Cloth Janitor
            roomNumber != 9770 || //Crystal Lobby
            roomNumber != 12500 || //Crystal Ocean
            roomNumber != 32500 || //Lightning Electric Chair
            roomNumber != 39260 || //Lightning Generator
            roomNumber != 29190 || //Lightning UFO
            roomNumber != 37291 || //Metal bedroom
            roomNumber != 11340 || //Metal prehistoric
            roomNumber != 17090 || //Metal projector
            roomNumber != 19250 || //Sand plants
            roomNumber != 12200 || //Sand Ocean
            roomNumber != 11300 || //Tar prehistoric
            roomNumber != 14040 || //Tar underground
            roomNumber != 9700 || //Water fountain
            roomNumber != 25060 || //Water Janitor Closet
            roomNumber != 24360 || //Wax Anansi
            roomNumber != 8160 || //Wax library
            roomNumber != 22100 || //Wax tiki
            roomNumber != 27081 || //Wood blue hallways
            roomNumber != 23160 || //Wood Gods Room
            roomNumber != 24190 || //Wood Pegasus room
            roomNumber != 7180 || //Wood workshop
            roomNumber != 7111 || //Workshop puzzle
            roomNumber != 9930 || //Lobby Fountain Spigot
            roomNumber != 8430 || //Library Book Puzzle
            roomNumber != 9691 || //Theater Door Puzzle
            roomNumber != 18250 || //Geoffrey Puzzle
            roomNumber != 40260 || //Clock Tower Chains Puzzle
            roomNumber != 932 || //Beth Ghost cutscene
            roomNumber != 35170 || //Camera surveilence
            roomNumber != 35154 || //Juke Box
            roomNumber != 17180 || //Projector Puzzle
            roomNumber != 934 || //Theater Movie cutscene
            roomNumber != 11350 || //Skull Dial prehistoric
            roomNumber != 14170 || //Skull Dial underground
            roomNumber != 24170 || //Skull Dial werewolf
            roomNumber != 21400 || //Skull Dial burial
            roomNumber != 20190 || //Skull Dial egypt
            roomNumber != 23650 || //Skull Dial gods
            roomNumber != 12600 || //Atlantis puzzle
            roomNumber != 12410 || //Organ puzzle
            roomNumber != 12590 || //Sirens Song
            roomNumber != 13010 || //Underground Maze Door Puzzle
            roomNumber != 20510 || //Column of Ra puzzle A
            roomNumber != 20610 || //Column of Ra puzzle B
            roomNumber != 20311 || //Egypt Door Puzzle
            roomNumber != 21071 || //Chinese Solitair
            roomNumber != 22180 || //tiki drums puzzle
            roomNumber != 23590 || //Lyre Puzzle
            roomNumber != 23601 || //Red Door Puzzle
            roomNumber != 27090 || //Horse Painting Puzzle
            roomNumber != 28050 || //Fortune Teller
            roomNumber != 933 || //Merrick Ghost Cutscene
            roomNumber != 30421 || //Alchemy Puzzle
            roomNumber != 29045 || //UFO Puzzle
            roomNumber != 29260 || //Planet Alignment Puzzle
            roomNumber != 29510 || //Planets Aligned Message
            roomNumber != 24440 || //Anansi Key
            (roomNumber == 24380 && IsKthBitSet(ReadMemory(380, 1), 8)) || //Anansi Music Box and Box is closed
            roomNumber != 32161 || //Guillotine
            roomNumber != 32059 || //Gallows Puzzle
            roomNumber != 32059 || //Gallows Puzzle
            roomNumber != 32390 || //Gallows Lever
            roomNumber != 31090 || //Mastermind Puzzle
            roomNumber != 31270 || //Marble Flipper Puzzle
            roomNumber != 31330 || //Skull Door
            roomNumber != 31390 || //Slide Wheel
            roomNumber != 936 //Slide Cutscene
          )
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void RoomShuffle()
    {
        RoomTransition? transition = roomTransitions.FirstOrDefault(transition =>
            roomNumberPrevious == transition.From && roomNumber == transition.DefaultTo //&& lastTransitionUsed != transition
        );

        //Fix Torture Room Door Bug
        FixTortureDoorBug();

        if (transition != null)
        {
            lastTransitionUsed = transition;

            if (transition.ElevatorFloor.HasValue)
            {
                WriteMemory(916, transition.ElevatorFloor.Value);
            }

            if (transition.DefaultTo != transition.NewTo)
            {
                //Respawn Ixupi
                RespawnIxupi(transition.NewTo);

                //Check if merrick flashback already aquired
                bool merrickAquired = IsKthBitSet(ReadMemory(364, 1), 4);

                //Stop Audio to prevent soft locks
                StopAudio(transition.NewTo);

                //Restore Merrick flashback to original state
                if (!merrickAquired)
                {
                    SetKthBitMemoryOneByte(364, 4, false);
                }
            }
        }
    }

    private void RespawnIxupi(int destinationRoom)
    {
        int rngRoll;

        if(destinationRoom is 9020 or 9450 or 9680 or 9600 or 9560 or 9620 or 25010) //Water Lobby/Toilet
        {
            if (ReadMemory((int)IxupiLocationOffsets.WATER, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WATER, 9000); //Fountain
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WATER, 25000); //Toilet
                }
            }
        }

        if(destinationRoom is 8000 or 8250 or 24750 or 24330) //Wax Library/Anansi
        {
            if (ReadMemory((int)IxupiLocationOffsets.WAX, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 8000); //Library
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 22000); //Tiki
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WAX, 24000); //Anansi
                }
            }
        }

        if(destinationRoom is 6400 or 6270 or 6020 or 38100) //Ash Office
        {
            if (ReadMemory((int)IxupiLocationOffsets.ASH, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.ASH, 6000); //Office
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.ASH, 21000); //Burial
                }
            }
        }

        if(destinationRoom is 11240 or 11100 or 11020) //Oil Prehistoric
        {
            if (ReadMemory((int)IxupiLocationOffsets.OIL, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.OIL, 11000); //Animals
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.OIL, 14000); //Tar River
                }
            }
        }

        if(destinationRoom is 7010 or 24280 or 24180) //Wood Workshop/Pegasus
        {
            if (ReadMemory((int)IxupiLocationOffsets.WOOD, 2) != 0)
            {
                rngRoll = rng.Next(0, 4);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 7000); //Workshop
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 23000); //Gods Room
                }
                else if (rngRoll == 2)
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 24000); //Pegasus
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.WOOD, 36000); //Back Hallways
                }
            }
        }

        if(destinationRoom is 12230 or 12010) //Crystal Ocean
        {
            if (ReadMemory((int)IxupiLocationOffsets.CRYSTAL, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 9000); //Lobby
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 12000); //Ocean
                }
            }
        }

        if(destinationRoom is 12230 or 12010 or 19040) //Sand Ocean/Plants
        {
            if (ReadMemory((int)IxupiLocationOffsets.SAND, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.SAND, 12000); //Ocean
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.SAND, 19000); //Plants
                }
            }
        }

        if(destinationRoom is 17010 or 37010) //Metal Projector Room/Bedroom
        {
            if (ReadMemory((int)IxupiLocationOffsets.METAL, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 11000); //Prehistoric
                }
                else if (rngRoll == 1)
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 17000); //Projector Room
                }
                else
                {
                    WriteMemory((int)IxupiLocationOffsets.METAL, 37000); //Bedroom
                }
            }
        }
    }

    private void FixTortureDoorBug()
    {
        if (roomNumber == 32076 && !(roomNumberPrevious == 32076))
        {
            int currentValue = ReadMemory(368, 1);
            currentValue = SetKthBit(currentValue, 4, false);
            WriteMemory(368, currentValue);
        }
    }

    private void ElevatorSettings()
    {
        //Elevators Stay Solved
        if (settingsElevatorsStaySolved)
        {
            //Check if an elevator has been solved
            if (ReadMemory(912, 1) != elevatorSolveCountPrevious)
            {
                //Determine which elevator was solved
                if (roomNumber == 6300 || roomNumber == 4630)
                {
                    elevatorUndergroundSolved = true;
                }
                else if (roomNumber == 38130 || roomNumber == 37360)
                {
                    elevatorBedroomSolved = true;
                }
                else if (roomNumber == 10101 || roomNumber == 27211 || roomNumber == 33500)
                {
                    elevatorThreeFloorSolved = true;
                }
            }

            //Check if approaching an elevator and that elevator is solved, if so open the elevator and force a screen redraw
            //Check if elevator is already open or not
            int currentElevatorState = ReadMemory(361, 1);
            if (IsKthBitSet(currentElevatorState, 1) != true)
            {
                if (((roomNumber == 6290 || roomNumber == 4620) && elevatorUndergroundSolved) ||
                    ((roomNumber == 38110 || roomNumber == 37330) && elevatorBedroomSolved) ||
                    ((roomNumber == 10100 || roomNumber == 27212 || roomNumber == 33140) && elevatorThreeFloorSolved))

                {
                    //Set Elevator Open Flag
                    //Set previous room to menu to force a redraw on elevator
                    currentElevatorState = SetKthBit(currentElevatorState, 1, true);
                    WriteMemory(361, currentElevatorState);
                    WriteMemory(-432, 990);
                }
            }
            else
            //If the elevator state is already open, check if its supposed to be. If not close it. This can happen when elevators are included in the room shuffle
            //As you dont step off the elevator in the normal spot, so the game doesnt auto close the elevator
            {
                if (((roomNumber == 6290 || roomNumber == 4620) && !elevatorUndergroundSolved) ||
                    ((roomNumber == 38110 || roomNumber == 37330) && !elevatorBedroomSolved) ||
                    ((roomNumber == 10100 || roomNumber == 27212 || roomNumber == 33140) && !elevatorThreeFloorSolved))
                {
                    currentElevatorState = SetKthBit(currentElevatorState, 1, false);
                    WriteMemory(361, currentElevatorState);
                    WriteMemory(-432, 990);
                }
            }
        }

        //Only 4x4 elevators. Must place after elevators open flag
        if (settingsOnly4x4Elevators)
        {
            WriteMemory(912, 0);
        }

        elevatorSolveCountPrevious = ReadMemory(912, 1);
    }

    public static bool IsKthBitSet(int n, int k)
    {
        return (n & (1 << k)) > 0;
    }

    //Sets the kth bit of a value. 0 indexed
    public static int SetKthBit(int value, int k, bool set)
    {
        if(set)//ON
        {
            value |= (1 << k);
        }
        else//OFF
        {
            value &= ~(1 << k);
        }

        return value;
    }

    //Sets the kth bit on Memory with the specified offset. 0 indexed
    private void SetKthBitMemoryOneByte(int memoryOffset, int k, bool set)
    {
        WriteMemory(memoryOffset, SetKthBit(ReadMemory(memoryOffset, 1), k, set));
    }

    private void EarlyLightning()
    {
        int lightningLocation = ReadMemory(236, 2);

        //If in basement and Lightning location isnt 0. (0 means he has been captured already)
        if (roomNumber == 39010 && lightningLocation != 0)
        {
            WriteMemory(236, 39000);
        }

        numberIxupiCaptured = ReadMemory(1712, 1);

        if (numberIxupiCaptured == 10 && finalCutsceneTriggered == false)
        {
            //If moved properly to final cutscene, disable the trigger for final cutscene
            finalCutsceneTriggered = true;
            WriteMemory(-424, 935);
        }
    }

    public void StopAudio(int destination)
    {
        const int WM_LBUTTON = 0x0201;

        int tempRoomNumber = 0;

        //Kill Tunnel Music
        int oilLocation = ReadMemory(204, 2); //Record where tar currently is
        WriteMemory(204, 11000); //Move Oil to Plants
        WriteMemory(-424, 11170); //Move Player to Plants
        WriteMemory(-432, 11180); //Set Player Previous Room to trigger oil nearby sound
        Thread.Sleep(30);
        WriteMemory(204, oilLocation); //Move Oil back
        if (oilLocation == 0)
        {
            WriteMemory(205, 0); //Oil Location 2nd byte. WriteMemory function needs changed to allow you to choose how many bytes to write
        }

        //Trigger Merrick cutscene to stop audio
        while (tempRoomNumber != 933)
        {
            WriteMemory(-424, 933);
            Thread.Sleep(20);

            //Set previous room so fortune teller audio does not play at conclusion of cutscene
            WriteMemory(-432, 922);

            tempRoomNumber = ReadMemory(-424, 2);
        }

        //Set previous room so fortune teller audio does not play at conclusion of cutscene
        WriteMemory(-432, 922);

        //Force a mouse click to skip cutscene. Keep trying until it succeeds.
        int sleepTimer = 10;
        while (tempRoomNumber == 933)
        {
            Thread.Sleep(sleepTimer);
            tempRoomNumber = ReadMemory(-424, 2);
            PostMessage(hwndtest, WM_LBUTTON, 1, MakeLParam(580, 320));
            PostMessage(hwndtest, WM_LBUTTON, 0, MakeLParam(580, 320));
            sleepTimer += 10; //Make sleep timer longer every attempt so the user doesnt get stuck in a soft lock
        }

        bool atDestination = false;

        while (!atDestination)
        {
            WriteMemory(-424, destination);
            Thread.Sleep(50);
            tempRoomNumber = ReadMemory(-424, 2);
            if (tempRoomNumber == destination)
            {
                atDestination = true;
            }
        }
    }

    private void VanillaPlacePiece(int potPiece, Random rng)
    {
        /*
        0 = Desk
        1 = Drawers
        2 = Cupboard
        3 = Library
        4 = Slide
        5 = Eagles Head
        6 = Eagles Nest
        7 = Ocean
        8 = Tar River
        9 = Theater
        10 = Greenhouse
        11 = Egypt
        12 = Chinese
        13 = Tiki Hut
        14 = Lyre
        15 = Skeleton
        16 = Anansi
        17 = Janitors Closet / Cloth
        18 = Ufo
        19 = Alchemy
        20 = Puzzle
        21 = Hanging
        22 = Clock
        */

        int locationRand = rng.Next(0, 23);
        while (true)
        {
            if (locationRand >= 23)
            {
                locationRand = (int)PotLocation.DESK;
            }

            //Check if piece is cloth and location is janitors closest
            if (locationRand == (int)PotLocation.BATHROOM &&
                (potPiece == (int)IxupiPots.CLOTH_BOTTOM || potPiece == (int)IxupiPots.CLOTH_TOP))
            {
                locationRand += 1;
                continue;
            }

            //Checking oil is in the bathroom or tar river
            if ((locationRand == (int)PotLocation.TAR_RIVER || locationRand == (int)PotLocation.BATHROOM) &&
                (potPiece == (int)IxupiPots.OIL_BOTTOM || potPiece == (int)IxupiPots.OIL_TOP))
            {
                locationRand += 1;
                continue;
            }

            //For extra locations, is disabled in vanilla
            if (EXTRA_LOCATIONS.Contains(locationRand))
            {
                locationRand += 1;
                continue;
            }

            //Check if location is already filled
            if (Locations[locationRand] != 0)
            {
                locationRand += 1;
                continue;
            }

            break;
        }
        Locations[locationRand] = potPiece;
    }

    private void LocateAllScripts()
    {
            //Load in the list of script numbers
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Shivers_Randomizer.resources.ScriptList.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int number = int.Parse(line);
                    completeScriptList.Add(number);
                }
            }

            //Locate Scripts
            //This should find all of them
            LocateScript(4280);
            LocateScript(9170);
            LocateScript(13349);
            LocateScript(31520);

            //If any left then search specifically
            while (completeScriptList.Count > 0)
            {
                LocateScript(completeScriptList[0]);
            }

            scriptsFound.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            scriptsLocated = true;
    }

    private void LocateScript(int scriptToFind)
    {
        //Signature to scan for
        //byte[] toFind = new byte[] { 0x73, 0x63, 0x72, 0x69, 0x70, 0x74, 0x2E, 0x33, 0x33, 0x32, 0x30, 0x30 };
        byte[] toFind = new byte[7 + scriptToFind.ToString().Length];
        toFind[0] = 0x73;//'Script.'
        toFind[1] = 0x63;
        toFind[2] = 0x72;
        toFind[3] = 0x69;
        toFind[4] = 0x70;
        toFind[5] = 0x74;
        toFind[6] = 0x2E;


        for (int i = 0; i < scriptToFind.ToString().Length; i++)
        {
            toFind[i + 7] = (byte)(scriptToFind.ToString()[i]);
        }

        testAddress = scanner.AobScan2(processHandle, toFind);

        //Find start of memory block
        for (int i = 1; i < 20000; i++)
        {
            //Locate several FF in a row
            if (ReadMemoryAnyAddress(testAddress, i * -16, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 1, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 2, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 3, 1) == 255 &&
                ReadMemoryAnyAddress(testAddress, i * -16 + 4, 1) == 255)
            {
                testAddress -= 16 * i;
                break;
            }



        }

        if (testAddress != UIntPtr.Zero)
        {
            char[] letters = new char[6];

            for (int i = 0; i < 2500; i++)
            {
                int result = 0;

                //There are other files in the memory blocks, scripts heaps vocab font palette message. If its script continue, if its not script increment i, 
                //if its nothing break since it must be the end of the memory block
                for (int j = 0; j < 6; j++)
                {
                    letters[j] = (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + j, 1);
                }

                if (letters[0] != 115 && letters[1] != 99 && letters[2] != 114 && letters[3] != 105 && letters[4] != 112 && letters[5] != 116) //Not Script
                {
                    if ((letters[0] != 112 && letters[1] != 105 && letters[2] != 99) &&//Not pic
                        (letters[0] != 104 && letters[1] != 101 && letters[2] != 97 && letters[3] != 112) && //Not heap
                        (letters[0] != 102 && letters[1] != 111 && letters[2] != 110 && letters[3] != 116) && //Not font
                        (letters[0] != 118 && letters[1] != 111 && letters[2] != 99 && letters[3] != 97 && letters[4] != 98) && //Not vocab
                        (letters[0] != 112 && letters[1] != 97 && letters[2] != 108 && letters[3] != 101 && letters[4] != 116 && letters[5] != 116) && //Not palette
                        (letters[0] != 109 && letters[1] != 101 && letters[2] != 115 && letters[3] != 115 && letters[4] != 97 && letters[5] != 103) //Not message
                        )
                    {
                        break;
                    }
                    continue;
                }

                //If it is a script, grab the script number
                char[] charArray2 = new char[] { (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 7, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 8, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 9, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 10, 1),
                    (char)ReadMemoryAnyAddress(testAddress, 128 * i + 80 + 11, 1)};

                //Convert chars into ints
                foreach (char c in charArray2)
                {
                    if (c >= '0' && c <= '9') // Check if character is a numeric digit
                    {
                        int digitValue = c - '0'; // Convert character to int value
                        result = (result * 10) + digitValue; // Combine int values
                    }
                }

                //Add the script number and memory address to list
                //I cannot figure out why paletts are not gettign caught in the filter above above, so remove them manually
                if (result != 409 && result != 999)
                {
                    scriptsFound.Add(Tuple.Create(result, testAddress + i * 128 + 80));

                    //Remove the found script from are full list
                    completeScriptList.Remove(result);
                }
            }
        }
    }

    public void WriteMemory(int offset, int value)
    {
        uint bytesWritten = 0;
        uint numberOfBytes = 1;

        if (value < 256)
        { numberOfBytes = 1; }
        else if (value < 65536)
        { numberOfBytes = 2; }
        else if (value < 16777216)
        { numberOfBytes = 3; }
        else if (value <= 2147483647)
        { numberOfBytes = 4; }

        WriteProcessMemory(processHandle, (ulong)(MyAddress + offset), BitConverter.GetBytes(value), numberOfBytes, ref bytesWritten);
    }

    public void WriteMemoryTwoBytes(int offset, int value)
    {
        uint bytesWritten = 0;
        uint numberOfBytes = 2;

        WriteProcessMemory(processHandle, (ulong)(MyAddress + offset), BitConverter.GetBytes(value), numberOfBytes, ref bytesWritten);
    }

    public int ReadMemory(int offset, int numbBytesToRead)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[2];
        ReadProcessMemory(processHandle, (ulong)(MyAddress + offset), buffer, (ulong)buffer.Length, ref bytesRead);

        if (numbBytesToRead == 1)
        {
            return buffer[0];
        }
        else if (numbBytesToRead == 2)
        {
            return (buffer[0] + (buffer[1] << 8));
        }
        else
        {
            return buffer[0];
        }
    }

    public void WriteMemoryAnyAdress(UIntPtr anyAddress, int offset, int value)
    {
        uint bytesWritten = 0;
        uint numberOfBytes = 1;

        WriteProcessMemory(processHandle, (ulong)(anyAddress + offset), BitConverter.GetBytes(value), numberOfBytes, ref bytesWritten);
    }

    public int ReadMemoryAnyAddress(UIntPtr anyAddress, int offset, int numbBytesToRead)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[2];
        ReadProcessMemory(processHandle, (ulong)(anyAddress + offset), buffer, (ulong)buffer.Length, ref bytesRead);

        if (numbBytesToRead == 1)
        {
            return buffer[0];
        }
        else if (numbBytesToRead == 2)
        {
            return (buffer[0] + (buffer[1] << 8));
        }
        else
        {
            return buffer[0];
        }

    }

    public UIntPtr LoadedScriptAddress(int scriptBeingFound)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[8];
        ReadProcessMemory(processHandle, (ulong)scriptsFound.FirstOrDefault(t => t.Item1 == 9470).Item2 - 32, buffer, (ulong)buffer.Length, ref bytesRead);

        ulong addressValue = BitConverter.ToUInt64(buffer, 0);
        UIntPtr addressPtr = new UIntPtr(addressValue);

        return addressPtr;

    }
}
