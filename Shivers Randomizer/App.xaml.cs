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
    public LiveSplit? liveSplit = null;
    public Multiplayer_Client? multiplayer_Client = null;
    public Archipelago_Client? archipelago_Client = null;

    private RectSpecial ShiversWindowDimensions = new();

    public UIntPtr processHandle;
    public UIntPtr MyAddress;
    public Process? shiversProcess;
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
    public int health;
    public int healthPrevious;
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
    private readonly AttachPopup scanner = new();

    readonly List<Tuple<int, UIntPtr>> scriptsFound = new();
    readonly List<int> completeScriptList = new();
    bool scriptsLocated = false;
    bool scriptAlreadyModified = false;

    //private List<NetworkItem> archipelagoReceivedItems;
    private List<int> archipelagoReceivedItems = new();
    private bool archipelagoInitialized;
    private bool archipelagoTimerTick;
    private bool archipelagoRegistryMessageSent;
    private readonly bool[] archipelagoPiecePlaced = new[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
    private readonly int archipelagoBaseLocationID = 27000;
    private readonly int archipelagoBaseItemID = 27000;
    private bool archipelagoRunningTick;
    private bool archipelagoCheckStoneTablet;
    private bool archipelagoCheckBasilisk;
    private bool archipelagoCheckSirenSong;
    private bool archipelagoCheckEgyptianSphinx;
    private bool archipelagoCheckGallowsPlaque;
    private bool archipelagoCheckGeoffreyWriting;
    private bool archipelagoGeneratorSwitchOn;
    private bool archipelagoGeneratorSwitchScreenRefreshed;
    List<int> archipelagoCompleteScriptList = new();

    public App()
    {
        mainWindow = new(this);
        overlay = new(this);
        rng = new();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        archipelago_Client?.Disconnect();
        liveSplit?.Disconnect();
        base.OnExit(e);
    }

    public void StopArchipelago()
    {
        stopArchipelagoTimerEvent?.Set();
        archipelagoTimerThread?.Join();
        stopScriptModificationTimerEvent?.Set();
        scriptModificationTimerThread?.Join();

        //Reset initilization info
        //If player was on the boat move to pause menu screen and then to main menu, else move straight to main menu. This clears the boat state flag
        if(roomNumber >= 3120 && roomNumber <= 3320)
        {
            WriteMemory(-424, 990); // Move to pause menu
            Thread.Sleep(500);
            WriteMemory(-424, 910); // Move to main menu
        }
        else
        {
            WriteMemory(-424, 910); // Move to main menu
        }
        archipelagoInitialized = false;
        archipelagoRegistryMessageSent = false;
        archipelagoTimerTick = false;
        archipelagoRunningTick = false;
        archipelagoReceivedItems.Clear();
        Array.Fill(archipelagoPiecePlaced, false);

        //Reset flags
        archipelagoCheckStoneTablet = false;
        archipelagoCheckBasilisk = false;
        archipelagoCheckSirenSong = false;
        archipelagoCheckEgyptianSphinx = false;
        archipelagoCheckGallowsPlaque = false;
        archipelagoCheckGeoffreyWriting = false;
        archipelagoGeneratorSwitchOn = false;
        archipelagoGeneratorSwitchScreenRefreshed = false;
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
            if (numberOfRemainingPots == 2 && (settingsIncludeAsh ^ settingsIncludeLightning))
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

                    Dispatcher.Invoke(() =>
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

    private Thread? archipelagoTimerThread = null;
    private Thread? scriptModificationTimerThread = null;
    private ManualResetEvent? stopArchipelagoTimerEvent = null;
    private ManualResetEvent? stopScriptModificationTimerEvent = null;

    public void StartArchipelagoTimer()
    {
        stopArchipelagoTimerEvent = new ManualResetEvent(false);
        Stopwatch stopwatch = new();
        stopwatch.Start();

        archipelagoTimerThread = new Thread(() =>
        {
            while (!stopArchipelagoTimerEvent.WaitOne(0))
            {
                if (stopwatch.ElapsedMilliseconds >= 1)
                {
                    Thread.Sleep(2000);
                    archipelagoTimerTick = true;
                    stopwatch.Restart();
                }
            }

        });
        archipelagoTimerThread.Start();
    }

    public void StartScriptModificationTimer()
    {
        stopScriptModificationTimerEvent = new ManualResetEvent(false);
        Stopwatch stopwatch = new();
        stopwatch.Start();

        scriptModificationTimerThread = new Thread(() =>
        {
            while (!stopScriptModificationTimerEvent.WaitOne(0))
            {
                if (stopwatch.ElapsedMilliseconds >= 10)
                {
                    //Modify Scripts
                    ArchipelagoModifyScripts();

                    //Check if a player slipped through a door without a key
                    ArchipelagoKeyCheck();

                    stopwatch.Restart();
                }
            }

        });
        scriptModificationTimerThread.Priority = ThreadPriority.Highest;
        scriptModificationTimerThread.Start();
    }
    private void Timer_Tick(object? sender, EventArgs e)
    {
        slowTimerCounter += 1;
        mainWindow.label_slowCounter.Content = slowTimerCounter;

        //Check that the process is still Shivers, if so disconnect archipelago and livesplit
        Process tempProcess = Process.GetProcessById(shiversProcess?.Id ?? 0);
        var windowExists = GetWindowRect((UIntPtr)(long)(shiversProcess?.MainWindowHandle ?? IntPtr.Zero), ref ShiversWindowDimensions);
        var windowIconic = IsIconic((UIntPtr)(long)(shiversProcess?.MainWindowHandle ?? IntPtr.Zero));

        if (shiversProcess != null && !tempProcess.MainWindowTitle.Contains("Shivers") && !tempProcess.MainWindowTitle.Contains("Status"))
        {
            archipelago_Client?.Close();
            liveSplit?.Disconnect();

            shiversProcess = null;
            processHandle = UIntPtr.Zero;
            MyAddress = UIntPtr.Zero;
            AddressLocated = false;
            scriptsLocated = false;
            scriptAlreadyModified = false;
        }

        overlay.Left = ShiversWindowDimensions.Left;
        overlay.Top = ShiversWindowDimensions.Top + (int)SystemParameters.WindowCaptionHeight;
        overlay.labelOverlay.Foreground = windowExists && windowIconic ? overlay.brushTransparent : overlay.brushLime;

        if (Seed == 0)
        {
            overlay.labelOverlay.Content = "Not yet randomized";
        }
        if (archipelago_Client != null)
        {
            overlay.OverlayArchipelago();
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

        //Label for ixupi captured number
        int numberIxupiCapturedTemp = ReadMemory(1712, 1);
        if (numberIxupiCapturedTemp > numberIxupiCaptured)
        {
            numberIxupiCaptured = numberIxupiCapturedTemp;
            mainWindow.label_ixupidNumber.Content = numberIxupiCaptured;
            liveSplit?.IxupiCaptured(numberIxupiCaptured);
        }

        //Label for base memory address
        mainWindow.label_baseMemoryAddress.Content = MyAddress.ToString("X8");

        //Early lightning
        if (settingsEarlyLightning && !settingsVanilla)
        {
            EarlyLightning();
        }

        //Elevators Stay Solved
        //Only 4x4 elevators.
        ElevatorSettings();

        //Check if oil is captured in room shuffle
        if (settingsRoomShuffle)
        {
            CheckOil();
        }

        int healthTemp = ReadMemory(-40, 1);
        if (healthTemp != health)
        {
            healthPrevious = health;
            health = healthTemp;
            liveSplit?.HealthChanged(healthPrevious, health, roomNumber);
        }

        liveSplit?.BethRiddleFound();
        liveSplit?.JukeboxSet();
        

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


        //---------Archipelago----------
        
        mainWindow.button_Archipelago.IsEnabled = MyAddress != UIntPtr.Zero;
        
        if (archipelago_Client?.IsConnected ?? false && AddressLocated.HasValue && AddressLocated.Value)
        {
            mainWindow.button_Scramble.IsEnabled = false;

            //Prevent the player from entering the save/load screen
            ArchipelagoPreventSaveLoad();

            //Initialization
            if (!archipelagoInitialized)
            {
                if (roomNumber == 922)
                {
                    StartArchipelagoTimer(); //2 second timer so we arent hitting the archipelago server as fast as possible
                    StartScriptModificationTimer();
                    archipelagoInitialized = true;

                    //Remove all pot pieces from museum
                    //Start be clearing any pot data
                    for (int i = 0; i < Locations.Count; i++)
                    {
                        Locations[i] = 0;
                    }

                    //Place empty locations
                    PlacePieces();

                    //Initilize data storage
                    archipelago_Client?.InitilizeDataStorage(ReadMemory(836, 1), ReadMemory(840, 1), ReadMemory(844, 1), ReadMemory(848, 1), ReadMemory(852, 1), ReadMemory(856, 1));


                    //Load flags
                    ArchipelagoLoadFlags();
                    
                    new Thread(() =>
                    {
                        ArchipelagoLoadData();
                    }).Start();
                }
                else
                {
                    //If player isnt on registry page, move player to title screen, also send message to player to tell them to move to the registry page
                    if(!archipelagoRegistryMessageSent)
                    {
                        //Brush brush = new SolidColorBrush(Colors.Red);
                        //archipelago_Client.ServerMessageBox.Foreground = brush;
                        archipelago_Client.ServerMessageBox.AppendText("Please move to registry page" + Environment.NewLine);
                        archipelagoRegistryMessageSent = true;
                    }
                }
            }
            else
            {
                if (archipelagoTimerTick == true && !archipelagoRunningTick)
                {
                    archipelagoRunningTick = true;

                    //Get items
                    archipelagoReceivedItems = archipelago_Client?.GetItemsFromArchipelagoServer()!;

                    //Send Checks
                    if (!windowIconic)
                    {
                        ArchipelagoSendChecks();
                    }

                    //If received a pot piece, place it in the museum.
                    ArchipelagoPlacePieces();

                    //Save Data
                    ArchipelagoSaveData();

                    //Update client window to show pot locations
                    ArchipelagoUpdateWindow();

                    //Check for victory
                    if (numberIxupiCaptured == 10)
                    {
                        archipelago_Client?.Send_completion();
                    }

                    archipelagoTimerTick = false;
                    archipelagoRunningTick = false;
                }

                //Allow outside access
                ArchipelagoOutsideAccess();

                //Always available ixupi from filler items
                ArchipelagoAvailableIxupi();

                //Set flags for checks that are sent based on room number. These need captured imedietly and not on the send checks timer
                if (roomNumber == 23311) //Stone Tablet Message Seen
                {
                    archipelagoCheckStoneTablet = true;
                }
                if (roomNumber == 7162) //Basilisk Paper Seen
                {
                    archipelagoCheckBasilisk = true;
                }
                if (roomNumber == 12590 && (ReadMemory(-400, 2) == 5857 || ReadMemory(-400, 2) == 5975)) //Siren Song Heard
                {
                    archipelagoCheckSirenSong = true;
                }
                if (roomNumber == 20572 && ReadMemory(-400, 2) == 5975) //Egyptian Sphinx Heard
                {
                    archipelagoCheckEgyptianSphinx = true;
                }
                if (roomNumber == 32810) //Gallows Plaque Seen
                {
                    archipelagoCheckGallowsPlaque = true;
                }
                if (roomNumber == 34040) //Geoffrey Writing In Elevator Seen
                {
                    archipelagoCheckGeoffreyWriting = true;
                }

                //----TODO: Exclude locations----
                //----TODO: Fix the freeze if server is stopped before closing client, it hangs on send check in client.cs

                //If player goes back to main menu reinitilize
                if (roomNumber == 910)
                {
                    archipelagoInitialized = false;
                    archipelagoRegistryMessageSent = false;
                }
            }
        }
        else
        {
            archipelago_Client?.Disconnect();
        }
    }

    private void ArchipelagoKeyCheck()
    {
        //Checks for if a player went through a door without a key
        if (roomNumber > 1000 && roomNumberPrevious > 1000)
        {
            if (
            (roomNumber == 5010 && roomNumberPrevious == 4620 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 20) ?? false)) || //Office Elevator
            (roomNumber == 38010 && roomNumberPrevious == 38110 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 21) ?? false)) || //Bedroom Elevator from Office side
            (roomNumber == 34030 && roomNumberPrevious == 10100 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false)) || //3-Floor Elevator from Maintenance Tunnels
            (roomNumber == 34030 && roomNumberPrevious == 27212 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false)) || //3-Floor Elevator from Blue Maze Bottom
            (roomNumber == 34030 && roomNumberPrevious == 33140 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false)) || //3-Floor Elevator from Blue Maze Top
            (roomNumber == 7010 && roomNumberPrevious == 6260 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 23) ?? false)) || //Workshop
            (roomNumber == 9020 && roomNumberPrevious == 6030 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 24) ?? false)) || //Lobby from Office
            (roomNumber == 11020 && roomNumberPrevious == 9590 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 25) ?? false)) || //Prehistoric
            (roomNumber == 19040 && roomNumberPrevious == 11320 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 26) ?? false)) || //Greenhouse
            (roomNumber == 12010 && roomNumberPrevious == 11120 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 27) ?? false)) || //Ocean
            (roomNumber == 17010 && roomNumberPrevious == 18230 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 28) ?? false)) || //Projector Room
            (roomNumber == 39010 && roomNumberPrevious == 10290 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 29) ?? false)) || //Generator Room
            (roomNumber == 20060 && roomNumberPrevious == 9570 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 30) ?? false)) || //Egypt from Lobby
            (roomNumber == 9560 && roomNumberPrevious == 20040 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 30) ?? false)) || //Lobby from Egypt
            (roomNumber == 8000 && roomNumberPrevious == 9470 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 31) ?? false)) || //Library
            (roomNumber == 22020 && roomNumberPrevious == 21440 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 32) ?? false)) || //Tiki
            (roomNumber == 29460 && roomNumberPrevious == 30010 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 33) ?? false)) || //UFO from Inventions side
            (roomNumber == 30020 && roomNumberPrevious == 29450 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 33) ?? false)) || //UFO from UFO side
            (roomNumber == 32010 && roomNumberPrevious == 30430 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 34) ?? false)) || //Toture
            (roomNumber == 31020 && roomNumberPrevious == 32450 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 35) ?? false)) || //Puzzle
            (roomNumber == 37010 && roomNumberPrevious == 37300 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 36) ?? false)) || //Bedroom
            (roomNumber == 3020 && roomNumberPrevious == 2330 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 37) ?? false)) || //Underground Lake Room
            //(roomNumber == 25010 && roomNumberPrevious == 26310) || //FOR FUTURE: Janitor
            (roomNumber == 38110 && roomNumberPrevious == 6030 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false)) || //Office Crawl Space
            (roomNumber == 10460 && roomNumberPrevious == 18240 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false)) || //Theater Backhalls Crawlspace
            (roomNumber == 9620 && roomNumberPrevious == 15260 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false)) || //Tar River Crawlspace from Tar River
            (roomNumber == 15240 && roomNumberPrevious == 9630 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false)) || //Tar River Crawlspace from Lobby
            (roomNumber == 27024 && roomNumberPrevious == 20150 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false)) || //Egypt Crawl Space from Egypt Side
            (roomNumber == 20160 && roomNumberPrevious == 27023 && !(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false))//Egypt Crawl Space from Blue Halls Side
            )

            {
                WriteMemory(-424, roomNumberPrevious);
            }
        }
    }

    private void ArchipelagoLoadData()
    {
        //Load player location
        int playerlocation = archipelago_Client?.LoadData("PlayerLocation") ?? 0;

        if (playerlocation >= 1000 && archipelagoCompleteScriptList.Contains(playerlocation))
        {
            WriteMemory(-424, playerlocation);
        }
        else
        {
            WriteMemory(-424, 1012);
        }

        WriteMemory(-432, 922); //Refresh screen to redraw inventory

        //Load skull dials
        int[] skullAddresses = { 836, 840, 844, 848, 852, 856 };
        string[] skullKeys = { "SkullDialPrehistoric", "SkullDialTarRiver", "SkullDialWerewolf", "SkullDialBurial", "SkullDialEgypt", "SkullDialGods" };

        foreach (int address in skullAddresses)
        {
            string key = skullKeys[(address - 836) / 4];
            var data = archipelago_Client?.LoadData(key);
            if (data != null)
            {
                WriteMemory(address, data.Value);
            }
        }

        //Load Jukebox State
        if ((archipelago_Client?.LoadData("Jukebox") ?? 0) == 1)
        {
            //Check not obtained but jukebox was set
            ArchipelagoSetFlagBit(377, 5); //Jukebox Set
        }

        //Load Tar River Shortcut flag
        if ((archipelago_Client?.LoadData("TarRiverShortcut") ?? 0) == 1)
        {
            ArchipelagoSetFlagBit(368, 6); //Tar River Shortcut open flag set
        }
    }

    private void ArchipelagoSaveData()
    {
        //Make sure in the game
        if(roomNumber >= 1000)
        {
            //Save player location, but not on the boat
            if (archipelagoCompleteScriptList.Contains(roomNumber) && !(roomNumber >= 3120 && roomNumber <= 3320 || roomNumber == 12600))
            {
                archipelago_Client?.SaveData("PlayerLocation", roomNumber);
            }

            //Save skull dials
            archipelago_Client?.SaveData("SkullDialPrehistoric", ReadMemory(836, 1));
            archipelago_Client?.SaveData("SkullDialTarRiver", ReadMemory(840, 1));
            archipelago_Client?.SaveData("SkullDialWerewolf", ReadMemory(844, 1));
            archipelago_Client?.SaveData("SkullDialBurial", ReadMemory(848, 1));
            archipelago_Client?.SaveData("SkullDialEgypt", ReadMemory(852, 1));
            archipelago_Client?.SaveData("SkullDialGods", ReadMemory(856, 1));

            //Save jukebox state
            int jukeboxState = IsKthBitSet(ReadMemory(377, 1), 5) ? 1 : 0;
            archipelago_Client?.SaveData("Jukebox", jukeboxState);

            //Save Tar River shortcute flag
            int tarRivershortcut = IsKthBitSet(ReadMemory(368, 1), 6) ? 1 : 0;
            archipelago_Client?.SaveData("TarRiverShortcut", tarRivershortcut);
        }
        


    }

    private void ArchipelagoPreventSaveLoad()
    {
        //If on the save/load screen and 
        if (roomNumber == 993 || roomNumber == 927)
        {
            if(roomNumberPrevious != 0) //There is a previous room number
            {
                WriteMemory(-424, roomNumberPrevious);
            }
            else //If no previous room number, that means the player attached after already being on the screen
                 //This means we can go ahead and move them to title screen
            {
                WriteMemory(-424, 910); //Set room to title
                
            }
            WriteMemory(-432, roomNumber); //Set previous room number in memory to restart the title music if that is the screen the player was on
        }

    }

    private void ArchipelagoAvailableIxupi()
    {
        //Get ixupi captured list
        int ixupiCaptured = ReadMemory(-60, 2);

        if (!IsKthBitSet(ixupiCaptured, 7)) //Water not captured
        {
            if (roomNumber >= 9000 && roomNumber < 10000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 92) ?? false)) //Lobby
            {
                WriteMemory((int)IxupiLocationOffsets.WATER, 9000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 9)) //Wax not captured
        {
            if (roomNumber >= 8000 && roomNumber < 9000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 93) ?? false)) //Library
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 8000);
            }
            else if (roomNumber >= 24000 && roomNumber < 25000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 94) ?? false)) //Anansi Room
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 24000);
            }
            else if (roomNumber >= 22000 && roomNumber < 23000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 95) ?? false)) //Tiki Room
            {
                WriteMemory((int)IxupiLocationOffsets.WAX, 22000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 6)) //Ash not captured
        {
            if (roomNumber >= 6000 && roomNumber < 7000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 96) ?? false)) //Office
            {
                WriteMemory((int)IxupiLocationOffsets.ASH, 6000);
            }
            else if (roomNumber >= 21000 && roomNumber < 22000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 97) ?? false)) //Burial Room
            {
                WriteMemory((int)IxupiLocationOffsets.ASH, 21000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 3)) //Oil not captured
        {
            if (roomNumber >= 11000 && roomNumber < 12000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 98) ?? false)) //Prehistoric
            {
                WriteMemory((int)IxupiLocationOffsets.OIL, 11000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 8)) //Cloth not captured
        {
            if (roomNumber >= 20000 && roomNumber < 21000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 99) ?? false)) //Egypt
            {
                WriteMemory((int)IxupiLocationOffsets.CLOTH, 20000);
            }
            else if (roomNumber >= 21000 && roomNumber < 22000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 100) ?? false)) //Burial Room
            {
                WriteMemory((int)IxupiLocationOffsets.CLOTH, 21000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 4)) //Wood not captured
        {
            if (roomNumber >= 7000 && roomNumber < 8000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 101) ?? false)) //Workshop
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 7000);
            }
            else if (roomNumber >= 27000 && roomNumber < 28000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 102) ?? false)) //Blue Maze
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 36000);
            }
            else if (roomNumber >= 24000 && roomNumber < 25000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 103) ?? false)) //Pegasus Room
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 24000);
            }
            else if (roomNumber >= 23000 && roomNumber < 24000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 104) ?? false)) //Gods Room
            {
                WriteMemory((int)IxupiLocationOffsets.WOOD, 23000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 1)) //Crystal not captured
        {
            if (roomNumber >= 9000 && roomNumber < 10000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 105) ?? false)) //Lobby
            {
                WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 9000);
            }
            else if (roomNumber >= 12000 && roomNumber < 13000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 106) ?? false)) //Ocean
            {
                WriteMemory((int)IxupiLocationOffsets.CRYSTAL, 12000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 0)) //Sand not captured
        {
            if (roomNumber >= 19000 && roomNumber < 20000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 107) ?? false)) //Plants
            {
                WriteMemory((int)IxupiLocationOffsets.SAND, 19000);
            }
            else if (roomNumber >= 12000 && roomNumber < 13000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 108) ?? false)) //Ocean
            {
                WriteMemory((int)IxupiLocationOffsets.SAND, 12000);
            }
        }

        if (!IsKthBitSet(ixupiCaptured, 2)) //Metal not captured
        {
            if (roomNumber >= 17000 && roomNumber < 18000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 109) ?? false)) //Projector
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 17000);
            }
            else if (roomNumber >= 37000 && roomNumber < 38000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 110) ?? false)) //Bedroom
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 37000);
            }
            else if (roomNumber >= 11000 && roomNumber < 12000 && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 111) ?? false)) //Prehistoric
            {
                WriteMemory((int)IxupiLocationOffsets.METAL, 11000);
            }
        }

        //Remove captured Ixupi, this needs to be called or else ixupi can get stuck in the game
        ArchipelagoRemoveCapturedIxupi();

    }

    private void ArchipelagoOutsideAccess()
    {
        int generatorByte = ReadMemory(361, 1);
        if (!archipelagoGeneratorSwitchOn && IsKthBitSet(generatorByte, 5)) //Check if switch is pulled, if so set flag
        {
            archipelagoGeneratorSwitchOn = true;
        }

        if (roomNumber == 2000 && archipelagoGeneratorSwitchOn) //Check if looking at door and switch flag is true
        {
            if (!archipelagoGeneratorSwitchScreenRefreshed) //If screen hasnt already been refreshed
            {
                WriteMemory(361, SetKthBit(generatorByte, 5, false)); //Turn off switch in memory
                
                WriteMemory(-432, 990); //Refresh screen
                archipelagoGeneratorSwitchScreenRefreshed = true; //Set refresh flag
            }
        }
        else if (roomNumber != 2000 && archipelagoGeneratorSwitchOn) //If not looking at door and switch flag set
        {
            WriteMemory(361, SetKthBit(generatorByte, 5, true)); //Turn switch byte back on in memory
            archipelagoGeneratorSwitchScreenRefreshed = false; //Turn off refresh flag
        }
    }

    private void ArchipelagoRemoveCapturedIxupi()
    {
        int ixupiCaptured = ReadMemory(-60, 2);

        //Remove captured ixupi from game
        if (IsKthBitSet(ixupiCaptured, 0)) //Sand Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.SAND, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 1)) //Crystal Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CRYSTAL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 2)) //Metal Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.METAL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 3)) //Oil Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 4)) //Wood Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WOOD, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 5)) //Lightning Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.LIGHTNING, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 6)) //Ash Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.ASH, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 7)) //Water Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WATER, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 8)) //Cloth Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.CLOTH, 0);
        }
        if (IsKthBitSet(ixupiCaptured, 9)) //Wax Captured
        {
            WriteMemoryTwoBytes((int)IxupiLocationOffsets.WAX, 0);
        }
    }

    private void ArchipelagoUpdateWindow()
    {
        if (archipelago_Client != null)
        {
            archipelago_Client.LabelStorageDeskDrawer.Content = ConvertPotNumberToString(ReadMemory(0, 1));
            archipelago_Client.LabelStorageWorkshopDrawers.Content = ConvertPotNumberToString(ReadMemory(8, 1));
            archipelago_Client.LabelStorageLibraryCabinet.Content = ConvertPotNumberToString(ReadMemory(16, 1));
            archipelago_Client.LabelStorageLibraryStatue.Content = ConvertPotNumberToString(ReadMemory(24, 1));
            archipelago_Client.LabelStorageSlide.Content = ConvertPotNumberToString(ReadMemory(32, 1));
            archipelago_Client.LabelStorageEaglesHead.Content = ConvertPotNumberToString(ReadMemory(40, 1));
            archipelago_Client.LabelStorageEaglesNest.Content = ConvertPotNumberToString(ReadMemory(48, 1));
            archipelago_Client.LabelStorageOcean.Content = ConvertPotNumberToString(ReadMemory(56, 1));
            archipelago_Client.LabelStorageTarRiver.Content = ConvertPotNumberToString(ReadMemory(64, 1));
            archipelago_Client.LabelStorageTheater.Content = ConvertPotNumberToString(ReadMemory(72, 1));
            archipelago_Client.LabelStorageGreenhouse.Content = ConvertPotNumberToString(ReadMemory(80, 1));
            archipelago_Client.LabelStorageEgypt.Content = ConvertPotNumberToString(ReadMemory(88, 1));
            archipelago_Client.LabelStorageChineseSolitaire.Content = ConvertPotNumberToString(ReadMemory(96, 1));
            archipelago_Client.LabelStorageTikiHut.Content = ConvertPotNumberToString(ReadMemory(104, 1));
            archipelago_Client.LabelStorageLyre.Content = ConvertPotNumberToString(ReadMemory(112, 1));
            archipelago_Client.LabelStorageSkeleton.Content = ConvertPotNumberToString(ReadMemory(120, 1));
            archipelago_Client.LabelStorageAnansi.Content = ConvertPotNumberToString(ReadMemory(128, 1));
            archipelago_Client.LabelStorageJanitorCloset.Content = ConvertPotNumberToString(ReadMemory(136, 1));
            archipelago_Client.LabelStorageUFO.Content = ConvertPotNumberToString(ReadMemory(144, 1));
            archipelago_Client.LabelStorageAlchemy.Content = ConvertPotNumberToString(ReadMemory(152, 1));
            archipelago_Client.LabelStorageSkullBridge.Content = ConvertPotNumberToString(ReadMemory(160, 1));
            archipelago_Client.LabelStorageHanging.Content = ConvertPotNumberToString(ReadMemory(168, 1));
            archipelago_Client.LabelStorageClockTower.Content = ConvertPotNumberToString(ReadMemory(176, 1));
            archipelago_Client.LabelKeyOfficeElevator.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 20) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyBedroomElevator.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 21) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyThreeFloorElevator.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 22) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyWorkshop.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 23) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyLobby.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 24) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyPrehistoric.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 25) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyGreenhouse.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 26) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyOcean.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 27) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyProjector.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 28) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyGenerator.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 29) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyEgypt.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 30) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyLibrary.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 31) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyTiki.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 32) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyUFO.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 33) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyTorture.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 34) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyPuzzle.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 35) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyBedroom.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 36) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyUndergroundLake.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 37) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelKeyCrawling.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 50) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelEasierLyre.Visibility = archipelagoReceivedItems.Contains(archipelagoBaseItemID + 91) ? Visibility.Visible : Visibility.Hidden;
            archipelago_Client.LabelEasierLyre.Content = "Easier Lyre x " + (archipelagoReceivedItems?.Count(item => item == (archipelagoBaseItemID + 91)) ?? 0);
        }
    }

    private static string ConvertPotNumberToString(int potNumber)
    {
        string pieceName = "";
        switch (potNumber) //Determine which piece is being placed
        {
            case 200:
                pieceName = "Water Pot Bottom";
                break;
            case 201:
                pieceName = "Wax Pot Bottom";
                break;
            case 202:
                pieceName = "Ash Pot Bottom";
                break;
            case 203:
                pieceName = "Oil Pot Bottom";
                break;
            case 204:
                pieceName = "Cloth Pot Bottom";
                break;
            case 205:
                pieceName = "Wood Pot Bottom";
                break;
            case 206:
                pieceName = "Crystal Pot Bottom";
                break;
            case 207:
                pieceName = "Lightning Pot Bottom";
                break;
            case 208:
                pieceName = "Sand Pot Bottom";
                break;
            case 209:
                pieceName = "Metal Pot Bottom";
                break;
            case 210:
                pieceName = "Water Pot Top";
                break;
            case 211:
                pieceName = "Wax Pot Top";
                break;
            case 212:
                pieceName = "Ash Pot Top";
                break;
            case 213:
                pieceName = "Oil Pot Top";
                break;
            case 214:
                pieceName = "Cloth Pot Top";
                break;
            case 215:
                pieceName = "Wood Pot Top";
                break;
            case 216:
                pieceName = "Crystal Pot Top";
                break;
            case 217:
                pieceName = "Lightning Pot Top";
                break;
            case 218:
                pieceName = "Sand Pot Top";
                break;
            case 219:
                pieceName = "Metal Pot Top";
                break;
            case 220:
                pieceName = "Water Pot Complete";
                break;
            case 221:
                pieceName = "Wax Pot Complete";
                break;
            case 222:
                pieceName = "Ash Pot Complete";
                break;
            case 223:
                pieceName = "Oil Pot Complete";
                break;
            case 224:
                pieceName = "Cloth Pot Complete";
                break;
            case 225:
                pieceName = "Wood Pot Complete";
                break;
            case 226:
                pieceName = "Crystal Pot Complete";
                break;
            case 227:
                pieceName = "Lightning Pot Complete";
                break;
            case 228:
                pieceName = "Sand Pot Complete";
                break;
            case 229:
                pieceName = "Metal Pot Complete";
                break;
        }

        return pieceName;
    }

    private void ArchipelagoSetFlagBit(int offset, int bitNumber)
    {
        int tempValue = ReadMemory(offset, 1);
        tempValue = SetKthBit(tempValue, bitNumber, true);
        WriteMemory(offset, tempValue);
    }

    private void ArchipelagoLoadFlags()
    {
        //Get checked locations list
        List<long> LocationsChecked = archipelago_Client?.GetLocationsCheckedArchipelagoServer() ?? new();

        int ixupiCaptured = 0;
        int ixupiCapturedAmmount = 0;
        
        if(LocationsChecked.Contains(archipelagoBaseLocationID)) //Puzzle Solved Gears +169 Bit 8
        {
            ArchipelagoSetFlagBit(361, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 1)) //Puzzle Solved Stone Henge +169 Bit 7
        {                                                             //Generator Switch on +169 Bit 6
            ArchipelagoSetFlagBit(361, 6); //Stonehenge Solved
            ArchipelagoSetFlagBit(361, 5); //Generator Switch
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 2)) //Puzzle Solved Workshop Drawers +179 Bit 8
        {                                                             //Drawer Open +168 Bit 8  
            ArchipelagoSetFlagBit(377, 7); // Puzzle Solved
            ArchipelagoSetFlagBit(360, 7); // Drawer Open
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 3)) //Puzzle Solved Library Statue +170 Bit 8
        {
            ArchipelagoSetFlagBit(368, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 4)) //Puzzle Solved Theater Door +16C Bit 4
        {
            ArchipelagoSetFlagBit(364, 3);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 5)) //Puzzle Solved Geoffrey Door +16C Bit 2
        {
            ArchipelagoSetFlagBit(364, 1);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 6)) //Puzzle Solved Clock Chains +17C Bit 6
        {
            ArchipelagoSetFlagBit(380, 5);  //Puzle Solved
            WriteMemoryTwoBytes(1708, 530); //Set clock tower time
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 7)) //Puzzle Solved Atlantist +168 Bit 6
        {
            ArchipelagoSetFlagBit(360, 5);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 8)) //Puzzle Solved Organ +168 Bit 7
        {
            ArchipelagoSetFlagBit(360, 6);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 9)) //Puzzle Solved Maze Door +16C Bit 1
        {
            ArchipelagoSetFlagBit(364, 0);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 10)) //Puzzle Solved Columns of RA +16D Bit 7
        {
            ArchipelagoSetFlagBit(365, 6);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 11)) //Puzzle Solved Burial Door +16D Bit 6
        {
            ArchipelagoSetFlagBit(365, 5);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 12)) //Puzzle Solved Chinese Solitaire +17D Bit 5
        {                                                              //Drawer open +16D Bit 3
            ArchipelagoSetFlagBit(381, 4); //Puzzle Solved
            ArchipelagoSetFlagBit(365, 2); //Drawer Open
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 13)) //Puzzle Solved Tiki Drums +16D Bit 2
        {
            ArchipelagoSetFlagBit(365, 1);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 14)) //Puzzle Solved Lyre +16D Bit 1
        {
            ArchipelagoSetFlagBit(365, 0);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 15)) //Puzzle Solved Red Door +16C Bit 8
        {
            ArchipelagoSetFlagBit(364, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 16)) //Puzzle Solved Fortune Teller Door +16C Bit 6
        {
            ArchipelagoSetFlagBit(364, 5);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 17)) //Puzzle Solved Alchemy +174 Bit 6
        {                                                              //Box Opened +17D Bit 3
            ArchipelagoSetFlagBit(372, 5); //Puzzle Solved
            ArchipelagoSetFlagBit(381, 2); //Box Open
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 18)) //Puzzle Solved UFO Symbols +179 Bit 4
        {
            ArchipelagoSetFlagBit(377, 3);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 19)) //Puzzle Solved Anansi Musicbox +17C Bit 8
        {                                                              //Song set on jukebox +179 Bit 6
            //Check obtained already
            ArchipelagoSetFlagBit(380, 7); //Music Box Open
            ArchipelagoSetFlagBit(377, 5); //Jukebox Set
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 20)) //Puzzle Solved Gallows +17D Bit 7
        {
            ArchipelagoSetFlagBit(381, 6);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 21)) //Puzzle Solved Mastermind +179 Bit 7
        {
            ArchipelagoSetFlagBit(377, 6);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 22)) //Puzzle Solved Marble Flipper +168 Bit 5
        {
            ArchipelagoSetFlagBit(360, 4);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 23)) //Flashback Memory Obtained Beth's Ghost +16C Bit 3
        {
            ArchipelagoSetFlagBit(364, 2);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 24)) //Flashback Memory Obtained Merrick's Ghost +16C Bit 5
        {
            ArchipelagoSetFlagBit(364, 4);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 25)) //Flashback Memory Obtained Windlenot's Ghost +169 Bit 3
        {
            ArchipelagoSetFlagBit(361, 2);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 26)) //Flashback Memory Obtained Ancient Astrology +170 Bit 2
        {
            ArchipelagoSetFlagBit(368, 1);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 27)) //Flashback Memory Obtained Scrapbook +170 Bit 1
        {
            ArchipelagoSetFlagBit(368, 0);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 28)) //Flashback Memory Obtained Museum Brochure +175 Bit 8
        {
            ArchipelagoSetFlagBit(373, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 29)) //Flashback Memory Obtained In Search of the Unexplained +178 Bit 6
        {
            ArchipelagoSetFlagBit(376, 5);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 30)) //Flashback Memory Obtained Egyptian Hieroglyphics Explained +169 Bit 4
        {
            ArchipelagoSetFlagBit(361, 3);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 31)) //Flashback Memory Obtained South American Pictographs +175 Bit 7
        {
            ArchipelagoSetFlagBit(373, 6);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 32)) //Flashback Memory Obtained Mythology of the Stars +175 Bit 6
        {
            ArchipelagoSetFlagBit(373, 5);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 33)) //Flashback Memory Obtained Black Book +175 Bit 5
        {
            ArchipelagoSetFlagBit(373, 4);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 34)) //Flashback Memory Obtained Theater Movie +175 Bit 4
        {                                                              //Theater Curtain Open flag +168 Bit 3
            ArchipelagoSetFlagBit(373, 3); //Flashback
            ArchipelagoSetFlagBit(360, 2); //Curtain Open
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 35)) //Flashback Memory Obtained Museum Blueprints +175 Bit 3
        {
            ArchipelagoSetFlagBit(373, 2);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 36)) //Flashback Memory Obtained Beth's Address Book +175 Bit 2
        {
            ArchipelagoSetFlagBit(373, 1);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 37)) //Flashback Memory Obtained Merick's Notebook +175 Bit 1
        {
            ArchipelagoSetFlagBit(373, 0);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 38)) //Flashback Memory Obtained Professor Windlenot's Diary +174 Bit 8
        {
            ArchipelagoSetFlagBit(372, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 39)) //Ixupi Captured Water
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 7, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 40)) //Ixupi Captured Wax
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 9, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 41)) //Ixupi Captured Ash
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 6, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 42)) //Ixupi Captured Oil
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 3, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 43)) //Ixupi Captured Cloth
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 8, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 44)) //Ixupi Captured Wood
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 4, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 45)) //Ixupi Captured Crystal
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 1, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 46)) //Ixupi Captured Sand
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 0, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 47)) //Ixupi Captured Metal
        {
            ixupiCaptured = SetKthBit(ixupiCaptured, 2, true);
            ixupiCapturedAmmount += 1;
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 48)) //Final Riddle: Final Riddle: Fortune Teller +179 Bit 3
        {
            ArchipelagoSetFlagBit(377, 2);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 49)) //Final Riddle: Final Riddle: Planets Aligned +179 Bit 2
        {
            ArchipelagoSetFlagBit(377, 1);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 51)) //Final Riddle: Final Riddle: Beth's Body Page 17 +178 Bit 8
        {
            ArchipelagoSetFlagBit(376, 7);
        }
        if (LocationsChecked.Contains(archipelagoBaseLocationID + 52)) //Final Riddle: Guillotine Dropped +178 Bit 7
        {
            ArchipelagoSetFlagBit(376, 6);
        }



        //Set ixupi captured
        WriteMemory(-60, ixupiCaptured);

        //Set ixupi captured ammount in memory, and local variable
        WriteMemory(1712, ixupiCapturedAmmount);
        numberIxupiCaptured = ixupiCapturedAmmount;

        //Remove Captured Ixupi
        ArchipelagoRemoveCapturedIxupi();
    }

    private void ArchipelagoPlacePieces()
    {
        new Thread(() =>
        {
            int ixupiCaptured = ReadMemory(-60, 2);

            for (int i = 0; i < 20; i++)
            {
                if (archipelagoPiecePlaced[i] == false && (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + i) ?? true))
                {
                    //Check if ixupi is captured, if so dont place it
                    if (!((i == 0 || i == 10) && IsKthBitSet(ixupiCaptured, 7)) && //Water isnt captured
                    !((i == 1 || i == 11) && IsKthBitSet(ixupiCaptured, 9)) &&      //Wax isnt captured
                    !((i == 2 || i == 12) && IsKthBitSet(ixupiCaptured, 6)) &&      //Ash isnt captured
                    !((i == 3 || i == 13) && IsKthBitSet(ixupiCaptured, 3)) &&      //Oil isnt captured
                    !((i == 4 || i == 14) && IsKthBitSet(ixupiCaptured, 8)) &&      //Cloth isnt captured
                    !((i == 5 || i == 15) && IsKthBitSet(ixupiCaptured, 4)) &&      //Wood isnt captured
                    !((i == 6 || i == 16) && IsKthBitSet(ixupiCaptured, 1)) &&      //Crystal isnt captured
                    !((i == 7 || i == 17) && IsKthBitSet(ixupiCaptured, 5)) &&      //Lightning isnt captured
                    !((i == 8 || i == 18) && IsKthBitSet(ixupiCaptured, 0)) &&      //Earth isnt captured
                    !((i == 9 || i == 19) && IsKthBitSet(ixupiCaptured, 2))         //Metal isnt captured
                    )
                    {
                        ArchipelagoFindWhereToPlace(200 + i);
                    }

                    archipelagoPiecePlaced[i] = true;
                }
            }
        }).Start();
    }

    private void ArchipelagoFindWhereToPlace(int piece)
    {
        string pieceName = "";
        string locationName = "";
        int locationValue = 0;

        switch (piece) //Determine which piece is being placed
        {
            case 200:
                pieceName = "Water Pot Bottom";
                break;
            case 201:
                pieceName = "Wax Pot Bottom";
                break;
            case 202:
                pieceName = "Ash Pot Bottom";
                break;
            case 203:
                pieceName = "Oil Pot Bottom";
                break;
            case 204:
                pieceName = "Cloth Pot Bottom";
                break;
            case 205:
                pieceName = "Wood Pot Bottom";
                break;
            case 206:
                pieceName = "Crystal Pot Bottom";
                break;
            case 207:
                pieceName = "Lightning Pot Bottom";
                break;
            case 208:
                pieceName = "Sand Pot Bottom";
                break;
            case 209:
                pieceName = "Metal Pot Bottom";
                break;
            case 210:
                pieceName = "Water Pot Top";
                break;
            case 211:
                pieceName = "Wax Pot Top";
                break;
            case 212:
                pieceName = "Ash Pot Top";
                break;
            case 213:
                pieceName = "Oil Pot Top";
                break;
            case 214:
                pieceName = "Cloth Pot Top";
                break;
            case 215:
                pieceName = "Wood Pot Top";
                break;
            case 216:
                pieceName = "Crystal Pot Top";
                break;
            case 217:
                pieceName = "Lightning Pot Top";
                break;
            case 218:
                pieceName = "Sand Pot Top";
                break;
            case 219:
                pieceName = "Metal Pot Top";
                break;
            case 220: //If a full pot was already in the location, then just use the top piece
                pieceName = "Water Pot Top";
                break;
            case 221:
                pieceName = "Wax Pot Top";
                break;
            case 222:
                pieceName = "Ash Pot Top";
                break;
            case 223:
                pieceName = "Oil Pot Top";
                break;
            case 224:
                pieceName = "Cloth Pot Top";
                break;
            case 225:
                pieceName = "Wood Pot Top";
                break;
            case 226:
                pieceName = "Crystal Pot Top";
                break;
            case 227:
                pieceName = "Lightning Pot Top";
                break;
            case 228:
                pieceName = "Sand Pot Top";
                break;
            case 229:
                pieceName = "Metal Pot Top";
                break;
        }

        if (archipelago_Client != null)
        {
            //Figure out the matching Location
            for (int i = 0; i < archipelago_Client.storagePlacementsArray.GetLength(0); i++)
            {
                if (archipelago_Client.storagePlacementsArray[i, 1] == pieceName)
                {
                    locationName = archipelago_Client.storagePlacementsArray[i, 0];
                }
            }
        }

        string test = "Workshop Drawers";
        //Now that we have the location name, turn that into location value
        switch (locationName)
        {
            case "Desk Drawer":
                locationValue = 0;
                break;
            case "Workshop Drawers":
                locationValue = 1;
                break;
            case "Library Cabinet":
                locationValue = 2;
                break;
            case "Library Statue":
                locationValue = 3;
                break;
            case "Slide":
                locationValue = 4;
                break;
            case "Eagles Head":
                locationValue = 5;
                break;
            case "Eagles Nest":
                locationValue = 6;
                break;
            case "Ocean":
                locationValue = 7;
                break;
            case "Tar River":
                locationValue = 8;
                break;
            case "Theater":
                locationValue = 9;
                break;
            case "Greenhouse":
                locationValue = 10;
                break;
            case "Egypt":
                locationValue = 11;
                break;
            case "Chinese Solitaire":
                locationValue = 12;
                break;
            case "Tiki Hut":
                locationValue = 13;
                break;
            case "Lyre":
                locationValue = 14;
                break;
            case "Skeleton":
                locationValue = 15;
                break;
            case "Anansi":
                locationValue = 16;
                break;
            case "Janitor Closet":
                locationValue = 17;
                break;
            case "UFO":
                locationValue = 18;
                break;
            case "Alchemy":
                locationValue = 19;
                break;
            case "Skull Bridge":
                locationValue = 20;
                break;
            case "Hanging":
                locationValue = 21;
                break;
            case "Clock Tower":
                locationValue = 22;
                break;
        }

        //Place piece
        //First check if there a piece already located in the location. If so place the piece instead in its location
        if(ReadMemory(locationValue * 8, 1) == 0) //Not taken, place piece
        {
            WriteMemory(locationValue * 8, piece);
        }
        else //Taken
        {
            int pieceAlreadyHere = ReadMemory(locationValue * 8, 1);
            WriteMemory(locationValue * 8, piece);
            ArchipelagoFindWhereToPlace(pieceAlreadyHere);
        }
    }

    private void ArchipelagoSendChecks()
    {
        if(IsKthBitSet(ReadMemory(361, 1), 7)) //Puzzle Solved Gears +169 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID);
        }
        if (IsKthBitSet(ReadMemory(361, 1), 6)) //Puzzle Solved Stone Henge +169 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 1);
        }
        if (IsKthBitSet(ReadMemory(377, 1), 7)) //Puzzle Solved Workshop Drawers +179 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 2);
        }
        if (IsKthBitSet(ReadMemory(368, 1), 7)) //Puzzle Solved Library Statue +170 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 3);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 3)) //Puzzle Solved Theater Door +16C Bit 4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 4);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 1)) //Puzzle Solved Geoffrey Door +16C Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 5);
        }
        if (IsKthBitSet(ReadMemory(380, 1), 5)) //Puzzle Solved Clock Chains +17C Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 6);
        }
        if (IsKthBitSet(ReadMemory(360, 1), 5)) //Puzzle Solved Atlantist +168 Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 7);
        }
        if (IsKthBitSet(ReadMemory(360, 1), 6)) //Puzzle Solved Organ +168 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 8);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 0)) //Puzzle Solved Maze Door +16C Bit 1
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 9);
        }
        if (IsKthBitSet(ReadMemory(365, 1), 6)) //Puzzle Solved Columns of RA +16D Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 10);
        }
        if (IsKthBitSet(ReadMemory(365, 1), 5)) //Puzzle Solved Burial Door +16D Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 11);
        }
        if (IsKthBitSet(ReadMemory(381, 1), 4)) //Puzzle Solved Chinese Solitaire +17D Bit 5
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 12);
        }
        if (IsKthBitSet(ReadMemory(365, 1), 1)) //Puzzle Solved Tiki Drums +16D Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 13);
        }
        if (IsKthBitSet(ReadMemory(365, 1), 0)) //Puzzle Solved Lyre +16D Bit 1
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 14);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 7)) //Puzzle Solved Red Door +16C Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 15);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 5)) //Puzzle Solved Fortune Teller Door +16C Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 16);
        }
        if (IsKthBitSet(ReadMemory(372, 1), 5)) //Puzzle Solved Alchemy +174 Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 17);
        }
        if (IsKthBitSet(ReadMemory(377, 1), 3)) //Puzzle Solved UFO Symbols +179 Bit 4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 18);
        }
        if (IsKthBitSet(ReadMemory(380, 1), 7)) //Puzzle Solved Anansi Musicbox +17C Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 19);
        }
        if (IsKthBitSet(ReadMemory(381, 1), 6)) //Puzzle Solved Gallows +17D Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 20);
        }
        if (IsKthBitSet(ReadMemory(377, 1), 6)) //Puzzle Solved Mastermind +179 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 21);
        }
        if (IsKthBitSet(ReadMemory(360, 1), 4)) //Puzzle Solved Marble Flipper +168 Bit 5
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 22);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 2)) //Flashback Memory Obtained Beth's Ghost +16C Bit 3
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 23);
        }
        if (IsKthBitSet(ReadMemory(364, 1), 4)) //Flashback Memory Obtained Merrick's Ghost +16C Bit 5
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 24);
        }
        if (IsKthBitSet(ReadMemory(361, 1), 2)) //Flashback Memory Obtained Windlenot's Ghost +169 Bit 3
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 25);
        }
        if (IsKthBitSet(ReadMemory(368, 1), 1)) //Flashback Memory Obtained Ancient Astrology +170 Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 26);
        }
        if (IsKthBitSet(ReadMemory(368, 1), 0)) //Flashback Memory Obtained Scrapbook +170 Bit 1
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 27);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 7)) //Flashback Memory Obtained Museum Brochure +175 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 28);
        }
        if (IsKthBitSet(ReadMemory(376, 1), 5)) //Flashback Memory Obtained In Search of the Unexplained +178 Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 29);
        }
        if (IsKthBitSet(ReadMemory(361, 1), 3)) //Flashback Memory Obtained Egyptian Hieroglyphics Explained +169 Bit 4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 30);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 6)) //Flashback Memory Obtained South American Pictographs +175 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 31);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 5)) //Flashback Memory Obtained Mythology of the Stars +175 Bit 6
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 32);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 4)) //Flashback Memory Obtained Black Book +175 Bit 5
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 33);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 3)) //Flashback Memory Obtained Theater Movie +175 Bit 4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 34);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 2)) //Flashback Memory Obtained Museum Blueprints +175 Bit 3
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 35);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 1)) //Flashback Memory Obtained Beth's Address Book +175 Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 36);
        }
        if (IsKthBitSet(ReadMemory(373, 1), 0)) //Flashback Memory Obtained Merick's Notebook +175 Bit 1
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 37);
        }
        if (IsKthBitSet(ReadMemory(372, 1), 7)) //Flashback Memory Obtained Professor Windlenot's Diary +174 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 38);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 7)) //Ixupi Captured Water -3B Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 39);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 9)) //Ixupi Captured Wax -3B Bit 10
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 40);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 6)) //Ixupi Captured Ash -3B Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 41);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 3)) //Ixupi Captured Oil -3B Bit 4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 42);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 8)) //Ixupi Captured Cloth -3B Bit 9
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 43);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 4)) //Ixupi Captured Wood -3B Bit 5
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 44);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 1)) //Ixupi Captured Crystal -3B Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 45);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 0)) //Ixupi Captured Sand -3B Bit 1
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 46);
        }
        if (IsKthBitSet(ReadMemory(-60, 2), 2)) //Ixupi Captured Metal -3B Bit 3
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 47);
        }
        if (IsKthBitSet(ReadMemory(377, 1), 2)) //Final Riddle: Fortune Teller +179 Bit 3
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 48);
        }
        if (IsKthBitSet(ReadMemory(377, 1), 1)) //Final Riddle: Planets Aligned +179 Bit 2
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 49);
        }
        if (archipelagoCheckStoneTablet) //Final Riddle: Norse God Stone Message, no bit so if on the screen send the check
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 50);
        }
        if (IsKthBitSet(ReadMemory(376, 1), 7)) //Final Riddle: Beth's Body Page 17 +178 Bit 8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 51);
        }
        if (IsKthBitSet(ReadMemory(376, 1), 6)) //Final Riddle: Guillotine Dropped +178 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 52); 
        }
        if (IsKthBitSet(ReadMemory(369, 1), 6)) //Puzzle Hint Found: Combo Lock in Mailbox +171 Bit 7
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 53);
        }
        if (ReadMemory(1244, 2) == 0) //Puzzle Hint Found: Orange Symbol +4DC
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 54);
        }
        if (ReadMemory(1248, 2) == 0) //Puzzle Hint Found: Silver Symbol +4E0
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 55);
        }
        if (ReadMemory(1252, 2) == 0) //Puzzle Hint Found: Green Symbol +4E4
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 56);
        }
        if (ReadMemory(1256, 2) == 0) //Puzzle Hint Found: White Symbol +4E8
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 57);
        }
        if (ReadMemory(1260, 2) == 0) //Puzzle Hint Found: Brown Symbol +4EC
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 58);
        }
        if (ReadMemory(1264, 2) == 0) //Puzzle Hint Found: Tan Symbol +4F0
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 59);
        }
        if (archipelagoCheckBasilisk) //Puzzle Hint Found: Basilisk Bone Fragments
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 60);
        }
        if (ReadMemory(1276, 2) == 0) //Puzzle Hint Found: Atlantist Map +4FC
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 61);
        }
        if (archipelagoCheckSirenSong) //Puzzle Hint Found: Sirens Song Heard
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 62);
        }
        if (archipelagoCheckEgyptianSphinx) //Puzzle Hint Found: Egyptian Sphinx Heard
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 63);
        }
        if (archipelagoCheckGallowsPlaque) //Puzzle Hint Found: Gallows Information Plaque
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 64);
        }
        if (ReadMemory(1176, 2) == 0) //Puzzle Hint Found: Mastermind Information Plaque
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 65);
        }
        if (archipelagoCheckGeoffreyWriting) //Puzzle Hint Found: Geoffrey Elevator Writing
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 66);
        }
        if (ReadMemory(1384, 2) == 0) //Puzzle Hint Found: RamTaBoBa (Security Camera) +568
        {
            archipelago_Client?.SendCheck(archipelagoBaseLocationID + 67);
        }
        
    }

    private void ArchipelagoModifyScripts()
    {
        if (scriptsLocated == false && processHandle != UIntPtr.Zero)
        {
            //Locate scripts
            LocateAllScripts();
        }

        if (scriptsLocated)
        {
            if (roomNumber == 2330) //Underground Lake Room Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(2330, 350, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 37) ?? false);
                }
            }
            else if (roomNumber == 4630) //Office Elevator
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(4630, 160, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 20) ?? false);
                }
            }
            else if (roomNumber == 6030) //Lobby door and crawl space to bedroom elevator
            {
                if (scriptAlreadyModified == false)
                {
                    bool flag6030 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 24) ?? false; //Door
                    ArchipelagoScriptRemoveCode(6030, 626, 137, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 629, 142, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 632, 137, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 635, 42, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 637, 197, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 640, 42, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 642, 197, flag6030);
                    ArchipelagoScriptRemoveCode(6030, 645, 143, flag6030);

                    ArchipelagoScriptRemoveCode(6030, 609, 142, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false); //crawl space
                }
            }
            else if (roomNumber == 6260) //Workshop Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(6260, 344, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 23) ?? false);
                }
            }
            else if (roomNumber == 8030) //Library Door Library Side
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(8030, 326, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 31) ?? false);

                }
            }
            else if (roomNumber == 9470) //Library Door Lobby Side
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(9470, 408, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 31) ?? false);
                }
            }
            else if (roomNumber == 9570) //Egypt Door From Lobby Side
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(9570, 274, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 30) ?? false);
                }
            }
            else if (roomNumber == 9630) //Tar River Crawl Space, Lobby side
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(9630, 200, 142, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false);
                }
            }
            else if (roomNumber == 15260) //Tar River Crawl Space, Tar River side
            {
                if (scriptAlreadyModified == false)
                {
                    bool flag15260 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false;
                    ArchipelagoScriptRemoveCode(15260, 92, 49, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 94, 142, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 97, 65, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 99, 135, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 102, 65, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 104, 30, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 106, 204, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 109, 30, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 111, 204, flag15260);
                    ArchipelagoScriptRemoveCode(15260, 114, 135, flag15260);
                }
            }
            else if (roomNumber == 20040) //Egypt Door From Egypt Side
            {
                if (scriptAlreadyModified == false)
                {
                    //Normal door method doesnt work, so polygon is set to 0 at all coordinates
                    bool flag9570 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 30) ?? false;
                    ArchipelagoScriptRemoveCode(20040, 468, 79, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 470, 18, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 472, 183, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 475, 18, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 477, 182, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 480, 134, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 488, 79, flag9570);
                    ArchipelagoScriptRemoveCode(20040, 490, 19, flag9570);
                }
            }
            else if (roomNumber == 9590) //Prehistoric Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(9590, 250, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 25) ?? false);
                }
            }
            else if (roomNumber == 10101) //Three Floor Elevator - Blue Maze Bottom
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(10101, 160, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false);
                }
            }
            else if (roomNumber == 10290) //Generator Door
            {
                if (scriptAlreadyModified == false)
                {
                    if(archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 29) ?? false)
                    {
                        ArchipelagoScriptRemoveCode(10290, 152, 51, true);
                        ArchipelagoScriptRemoveCode(10290, 153, 56, true);
                    }
                    else
                    {
                        ArchipelagoScriptRemoveCode(10290, 152, 0, true);
                        ArchipelagoScriptRemoveCode(10290, 153, 72, true);
                    }
                }
            }
            else if (roomNumber == 11120) //Ocean Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(11120, 374, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 27) ?? false);
                }
            }
            else if (roomNumber == 11320) //Greenhouse Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(11320, 225, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 26) ?? false);
                }
            }

            else if (roomNumber == 18230) //Projector Room Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(18230, 126, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 28) ?? false);
                }
            }
            else if (roomNumber == 18240) //Theater Back Hallways Crawlspace
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(18240, 132, 142, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false); //crawl space
                }
            }
            else if (roomNumber == 20150) //Egypt Crawlspace from Egypt Side
            {
                if (scriptAlreadyModified == false)
                {
                    //polygon is set to 0 at all coordinates
                    bool flag20150 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false;
                    ArchipelagoScriptRemoveCode(20150, 158, 73, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 160, 51, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 162, 173, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 165, 51, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 167, 171, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 170, 125, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 172, 171, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 175, 126, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 177, 77, flag20150);
                    ArchipelagoScriptRemoveCode(20150, 179, 126, flag20150);
                }
            }
            else if (roomNumber == 23590) //Lyre easy
            {
                if (scriptAlreadyModified == false)
                {
                    int easierLyreCount = Math.Min(archipelagoReceivedItems?.Count(item => item == (archipelagoBaseItemID + 91)) ?? 0, 9) ;
                    ArchipelagoScriptRemoveCode(23590, 2158, 12 - easierLyreCount, true); //Works slightly different each round completed is +1 with a +2 offset. So 2 rounds required is 4.
                }
            }
            else if (roomNumber == 27023) //Egypt Crawlspace from Blue Hallways Side
            {
                if (scriptAlreadyModified == false)
                {
                    //polygon is set to 0 at all coordinates
                    bool flag27023 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 50) ?? false;
                    ArchipelagoScriptRemoveCode(27023, 138, 50, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 140, 21, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 142, 63, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 144, 132, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 147, 194, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 150, 135, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 153, 205, flag27023);
                    ArchipelagoScriptRemoveCode(27023, 156, 20, flag27023);
                }
            }
            else if (roomNumber == 21440) //Tiki Door
            {
                if (scriptAlreadyModified == false)
                {
                    //Normal door method doesnt work, so polygon is set to 0 at all coordinates
                    bool flag21440 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 32) ?? false;
                    ArchipelagoScriptRemoveCode(21440, 335, 80, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 337, 16, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 339, 183, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 342, 16, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 344, 182, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 347, 136, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 350, 81, flag21440);
                    ArchipelagoScriptRemoveCode(21440, 352, 136, flag21440);
                }
            }
            else if (roomNumber == 27211) //Three Floor Elevator - Blue Maze Bottom
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(27211, 160, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false);
                }
            }
            else if (roomNumber == 29450) //UFO Door, UFO Side
            {
                if (scriptAlreadyModified == false)
                {
                    if (archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 33) ?? false)
                    {
                        ArchipelagoScriptRemoveCode(29450, 122, 56, true);
                    }
                    else
                    {
                        ArchipelagoScriptRemoveCode(29450, 122, 72, true);
                    }
                    
                }
            }
            else if (roomNumber == 30010) //UFO Door, Inventions Side
            {
                if (scriptAlreadyModified == false)
                {
                    //Had issues modifiying the script the normal way, so used the door open flag instead
                    int currentValue = ReadMemory(368, 1);
                    currentValue = SetKthBit(currentValue, 4, !archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 33) ?? true); //Set this to false when key obtained
                    WriteMemory(368, currentValue);
                    //Reload the screen
                    WriteMemory(-432, 990);

                    scriptAlreadyModified = true;
                }
            }
            else if (roomNumber == 30430) //Torture Room Door
            {
                if (scriptAlreadyModified == false)
                {
                    bool flag30430 = archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 34) ?? false;
                    ArchipelagoScriptRemoveCode(30430, 172, 97, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 174, 32, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 176, 162, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 179, 32, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 181, 162, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 184, 142, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 187, 96, flag30430);
                    ArchipelagoScriptRemoveCode(30430, 189, 142, flag30430);
                }
            }
            else if (roomNumber == 32450) //Puzzle Room Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(32450, 258, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 35) ?? false);
                }
            }
            else if (roomNumber == 33500) //Three Floor Elevator - Blue Maze Top
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(33500, 176, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false);
                    ArchipelagoScriptRemoveCode(33500, 190, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 22) ?? false);
                }
            }
            else if (roomNumber == 37300) //Bedroom Door
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(37300, 205, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 36) ?? false);
                }
            }
            else if (roomNumber == 38130) //Bedroom Elevator
            {
                if (scriptAlreadyModified == false)
                {
                    ArchipelagoScriptRemoveCode(38130, 160, 179, archipelagoReceivedItems?.Contains(archipelagoBaseItemID + 21) ?? false);
                }
            }
            else
            {
                scriptAlreadyModified = false;
            }
        }
    }

    private void ArchipelagoScriptRemoveCode(int scriptNumber, int offset, int valueToWriteWhenPassable, bool keyOrCrawlingObtained)
    {
        //Grab the location script
        UIntPtr? loadedScriptAddress = LoadedScriptAddress(scriptNumber);

        if (loadedScriptAddress.HasValue)
        {
            //Write changes to the script
            if (keyOrCrawlingObtained)
            {
                WriteMemoryAnyAdress(loadedScriptAddress.Value, offset, valueToWriteWhenPassable); //b3, 179 in decimal
            }
            else
            {
                WriteMemoryAnyAdress(loadedScriptAddress.Value, offset, 0);
            }

            //Force a script reload by setting the previous room again
            WriteMemory(-432, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(-432, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(-432, roomNumberPrevious);
            Thread.Sleep(10);
            WriteMemory(-432, roomNumberPrevious);

            scriptAlreadyModified = true;
        }
    }
    
    private void GetRoomNumber()
    {
        //Monitor Room Number
        if (MyAddress != UIntPtr.Zero && processHandle != UIntPtr.Zero) //Throws an exception if not checked in release mode.
        {
            int tempRoomNumber = ReadMemory(-424, 2);

            if (tempRoomNumber != roomNumber)
            {
                roomNumberPrevious = roomNumber;
                roomNumber = tempRoomNumber;
                liveSplit?.RoomChange(roomNumberPrevious, roomNumber);
            }
            Dispatcher.Invoke(() =>
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

    private void CheckOil()
    {
        // Make sure if oil is captured it stays captured
        if (!currentlyTeleportingPlayer)
        {
            int ixupiCaptureRead = ReadMemory(-60, 2);
            int oilLocation = ReadMemory((int)IxupiLocationOffsets.OIL, 2);

            if (IsKthBitSet(ixupiCaptureRead, (int)Ixupi.OIL) && oilLocation != 0)
            {
                WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, 0);
            }
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
                currentlyTeleportingPlayer = true;

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

                currentlyTeleportingPlayer = false;
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
        if (roomNumber == 32076 && roomNumberPrevious != 32076)
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
        int oilLocation = ReadMemory((int)IxupiLocationOffsets.OIL, 2); //Record where tar currently is
        WriteMemory((int)IxupiLocationOffsets.OIL, 11000); //Move Oil to Plants
        WriteMemory(-424, 11170); //Move Player to Plants
        WriteMemory(-432, 11180); //Set Player Previous Room to trigger oil nearby sound
        Thread.Sleep(30);
        WriteMemoryTwoBytes((int)IxupiLocationOffsets.OIL, oilLocation);

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
            PostMessage((UIntPtr)(long)shiversProcess?.MainWindowHandle, WM_LBUTTON, 1, MakeLParam(580, 320));
            PostMessage((UIntPtr)(long)shiversProcess?.MainWindowHandle, WM_LBUTTON, 0, MakeLParam(580, 320));
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

    private void LoadInScriptList()
    {
        //Clear completeScriptList if not empty
        completeScriptList.Clear();

        //Load in the list of script numbers
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = "Shivers_Randomizer.resources.ScriptList.txt";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int number = int.Parse(line);
                completeScriptList.Add(number);
            }
        }

        archipelagoCompleteScriptList = completeScriptList.ToList(); //Used in archipelago to determine invalid room numbers loaded from data storage
    }

    private void LocateAllScripts()
    {
        //Load in a fresh set of scripts and clear out any old found scripts
        LoadInScriptList();
        scriptsFound.Clear();

        //Locate Scripts
        //This should find most of the scripts
        LocateScript(4280);
        LocateScript(9170);
        LocateScript(13349);
        LocateScript(31520);

        //If any left then search specifically
        int iterations = 0;
        while (completeScriptList.Count > 5 && iterations < 10)
        {
            //Select randomly so that its not always getting stuck not able to find the first script in the list
            int randomIndex = rng.Next(0, completeScriptList.Count);
            LocateScript(completeScriptList[randomIndex]);
            iterations += 1;
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

        UIntPtr testAddress = scanner.AobScan2(processHandle, toFind);

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

    public UIntPtr? LoadedScriptAddress(int scriptBeingFound)
    {
        uint bytesRead = 0;
        byte[] buffer = new byte[8];
        Tuple<int, UIntPtr>? script = scriptsFound.FirstOrDefault(t => t.Item1 == scriptBeingFound);
        if (script == null)
        {
            return null;
        }

        ReadProcessMemory(processHandle, (ulong)script.Item2 - 32, buffer, (ulong)buffer.Length, ref bytesRead);

        ulong addressValue = BitConverter.ToUInt64(buffer, 0);
        UIntPtr addressPtr = new(addressValue);

        return addressPtr;
    }
}
