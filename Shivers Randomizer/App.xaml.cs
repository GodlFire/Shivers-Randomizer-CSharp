using Shivers_Randomizer;
using Shivers_Randomizer.room_randomizer;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public bool AddressLocated;
    public bool EnableAttachButton;

    public int Seed;
    public bool setSeedUsed;
    private Random rng;
    public int FailureMessage;
    public int ScrambleCount;
    public List<int> Locations = new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    public int roomNumber;
    public int roomNumberPrevious;
    public int numberIxupiCaptured;
    public int numberIxupiCapturedTemp;
    public int firstToTheOnlyXNumber;
    public bool finalCutsceneTriggered;
    private bool elevatorUndergroundSolved;
    private bool elevatorBedroomSolved;
    private bool elevatorThreeFloorSolved;

    public bool settingsVanilla;
    public bool settingsIncludeAsh;
    public bool settingsIncludeLightning;
    public bool settingsEarlyBeth;
    public bool settingsExtraLocations;
    public bool settingsExcludeLyre;
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

    public App()
    {
        mainWindow = new MainWindow(this);
        overlay = new Overlay(this);
        rng = new Random();
        mainWindow.Show();
    }

    public void Scramble()
    {
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
                FailureMessage = 1;
                goto Failure;
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
                FailureMessage = 2;
                goto Failure;
            }
            else if (numberOfRemainingPots == 2 && !settingsIncludeAsh && !settingsIncludeLightning) //1 set but didnt not include ash or lightning in scramble
            {
                FailureMessage = 3;
                goto Failure;
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

        //Set bytes for mailbox/red door/beth. Only mailbox is set if vanilla shuffle is selected
        //This is now obsolete. If the room number isnt 922 then the scramble button isnt enabled. Thus if the randomizer didnt work the scramble button would never enable
        //writeMemory(369, 84); //Mailbox 
        if (!settingsVanilla)
        {
            if (settingsRedDoor)
            {
                //WriteMemory(364, 144); --OLD
                SetKthBitMemoryOneByte(364, 7, true);
            }
            else
            {
                //WriteMemory(364, 0); --Old
                SetKthBitMemoryOneByte(364, 7, false);
            }
            if (settingsEarlyBeth)
            {
                //WriteMemory(381, 128); --OLD
                SetKthBitMemoryOneByte(381, 7, true);
            }
            else
            {
                //WriteMemory(381, 0); --OLD
                SetKthBitMemoryOneByte(381, 7, false);
            }
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
            //Sets slide in lobby to get to tar ON
            //WriteMemory(368, 64); --Old
            SetKthBitMemoryOneByte(368, 6, true);

            roomTransitions = new RoomRandomizer(this, rng).RandomizeMap();
        }
        else
        {
            //Sets slide in lobby to get to tar OFF
            //WriteMemory(368, 0); --Old
            SetKthBitMemoryOneByte(368, 6, false);
        }

        ScrambleCount += 1;
        mainWindow.label_ScrambleFeedback.Content = "Scramble Number: " + ScrambleCount;

        //Set info for overlay
        overlay.SetInfo();

        //Set Seed info and flagset info
        if (setSeedUsed)
        {
            mainWindow.label_Seed.Content = "Set Seed: " + Seed;
        } else
        {
            mainWindow.label_Seed.Content = "Seed: " + Seed;
        }
        mainWindow.label_Flagset.Content = "Flagset: " + overlay.flagset;


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

                //Send starting flagset to server
                multiplayer_Client.sendServerFlagset(overlay.flagset);

                //Send starting seed
                multiplayer_Client.sendServerSeed(Seed);

                //Reenable scramble button
                disableScrambleButton = false;

                currentlyRunningThreadOne = false;
            }).Start();
        }


    Failure:
        switch (FailureMessage)
        {
            case 1:
                MessageBox.Show("Seed was not less then 2,147,483,647. Please try again with a smaller number");
                FailureMessage = 0;
                break;
            case 2:
                MessageBox.Show("Number of Ixupi must be greater than 0");
                FailureMessage = 0;
                break;
            case 3:
                MessageBox.Show("If selecting 1 pot set you must include either lighting or ash into the scramble");
                FailureMessage = 0;
                break;
            case 4:
                MessageBox.Show("");
                FailureMessage = 0;
                break;
        }
    }

    private void WaitServerResponse()
    {
        while (multiplayer_Client?.serverResponded == false)
        {
            Thread.Sleep(100);
        }
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

    private int syncCounter = 0;
    private void Timer_Tick(object? sender, EventArgs e)
    {
        syncCounter += 1;
        GetWindowRect(hwndtest, ref ShiversWindowDimensions);
        overlay.Left = ShiversWindowDimensions.Left;
        overlay.Top = ShiversWindowDimensions.Top + (int)SystemParameters.WindowCaptionHeight;
        overlay.labelOverlay.Foreground = IsIconic(hwndtest) ? overlay.brushTransparent : overlay.brushLime;

        if (Seed == 0)
        {
            overlay.labelOverlay.Content = "Not yet randomized";
        }

        //Check if a window exists, if not hide the overlay
        if (!IsWindow(hwndtest))
        {
            overlay.Hide();
        }
        else
        {
            overlay.Show();
        }

        int tempRoomNumber;

        //Monitor Room Number
        if (MyAddress != (UIntPtr)0x0 && processHandle != (UIntPtr)0x0) //Throws an exception if not checked in release mode.
        {
            tempRoomNumber = ReadMemory(-424, 2);

            if (tempRoomNumber != roomNumber)
            {
                roomNumberPrevious = roomNumber;
                roomNumber = tempRoomNumber;
            }
            mainWindow.label_roomPrev.Content = roomNumberPrevious;
            mainWindow.label_room.Content = roomNumber;
        }

        //If room number is 910 or 922 update the status text. If room number is not 922 disable the scramble button.
        if (roomNumber == 910 || roomNumber == 922)
        {
            mainWindow.label_ShiversDetected.Content = "Shivers Detected! :)";
            mainWindow.button_Scramble.IsEnabled = roomNumber == 922;
        }

        //Early lightning
        if (settingsEarlyLightning && !settingsVanilla)
        {
            EarlyLightning();
        }

        //Room Shuffle
        if (settingsRoomShuffle)
        {
            RoomShuffle();
        }

        //Elevators Stay Solved
        if (settingsElevatorsStaySolved)
        {
            //Check if an elevator has been solved
            if (ReadMemory(912, 1) == 1)
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
            if (IsKthBitSet(currentElevatorState,1) != true)
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

        /*
        bool runThreadIfAvailable = false;
        if (syncCounter > 1)
        {
            runThreadIfAvailable = true;
            syncCounter -= 1;
        }
        */
        mainWindow.label_syncCounter.Content = syncCounter;

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

                    for (int i = 1; i < 11; i++)
                    {
                        if(IsKthBitSet(ixupiCaptureRead, i) && multiplayerIxupi[i] == false) //Check if ixupi at specific bit is now set, and if its not set in multiplayerIxupi list
                        {
                            multiplayerIxupi[i] = true;
                            multiplayer_Client.sendServerIxupiCaptured(ixupiCaptureRead);
                        }
                    }
                    

                    //Check if server has requested a ixupi sync
                    if(multiplayer_Client.syncIxupi && multiplayer_Client.ixupiCapture != ixupiCaptureRead)
                    {
                        //Set the ixupi captured
                        WriteMemory(-60, multiplayer_Client.ixupiCapture);

                        //Redraw pots on the inventory bar by setting previous room to the name select
                        WriteMemory(-432, 922);

                        //Reset sync flag
                        multiplayer_Client.syncIxupi = false;
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
            roomNumber == 24380 || //Anansi
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

            //Respawn Ixupi
            RespawnIxupi(transition.NewTo);

            //Check if merrick flashback already aquired
            bool merrickAquired = IsKthBitSet(ReadMemory(364, 1), 4);

            //Stop Audio to prevent soft locks
            StopAudio(transition.NewTo);

            //Restore Merrick flashback to original state
            if(!merrickAquired)
            {
                SetKthBitMemoryOneByte(364, 4, false);
            }
        }
    }

    private void RespawnIxupi(int destinationRoom)
    {
        int rngRoll;

        if(destinationRoom is 9020 or 9450 or 9680 or 9600 or 9560 or 9620 or 25010) //Water Lobby/Toilet
        {
            if (ReadMemory(180, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory(180, 9000); //Fountain
                }
                else
                {
                    WriteMemory(180, 25000); //Toilet
                }
            }
        }

        if(destinationRoom is 8000 or 8250 or 24750 or 24330) //Wax Library/Anansi
        {
            if (ReadMemory(188, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory(188, 8000); //Library
                }
                else if (rngRoll == 1)
                {
                    WriteMemory(188, 22000); //Tiki
                }
                else
                {
                    WriteMemory(188, 24000); //Anansi
                }
            }
        }

        if(destinationRoom is 6400 or 6270 or 6020 or 38100) //Ash Office
        {
            if (ReadMemory(196, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory(196, 6000); //Office
                }
                else
                {
                    WriteMemory(196, 21000); //Burial
                }
            }
                
        }

        if(destinationRoom is 11240 or 11100 or 11020) //Oil Prehistoric
        {
            if (ReadMemory(204, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory(204, 11000); //Animals
                }
                else
                {
                    WriteMemory(204, 14000); //Tar River
                }
            }
        }

        if(destinationRoom is 7010 or 24280 or 24180) //Wood Workshop/Pegasus
        {
            if (ReadMemory(220, 2) != 0)
            {
                rngRoll = rng.Next(0, 4);
                if (rngRoll == 0)
                {
                    WriteMemory(220, 7000); //Workshop
                }
                else if (rngRoll == 1)
                {
                    WriteMemory(220, 23000); //Gods Room
                }
                else if (rngRoll == 2)
                {
                    WriteMemory(220, 24000); //Pegasus
                }
                else
                {
                    WriteMemory(220, 36000); //Back Hallways
                }
            }
        }

        if(destinationRoom is 12230 or 12010) //Crystal Ocean
        {
            if (ReadMemory(228, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory(228, 9000); //Lobby
                }
                else
                {
                    WriteMemory(228, 12000); //Ocean
                }
            }
        }

        if(destinationRoom is 12230 or 12010 or 19040) //Sand Ocean/Plants
        {
            if (ReadMemory(244, 2) != 0)
            {
                rngRoll = rng.Next(0, 2);
                if (rngRoll == 0)
                {
                    WriteMemory(244, 12000); //Ocean
                }
                else
                {
                    WriteMemory(244, 19000); //Plants
                }
            }
        }

        if(destinationRoom is 17010 or 37010) //Metal Projector Room/Bedroom
        {
            if (ReadMemory(252, 2) != 0)
            {
                rngRoll = rng.Next(0, 3);
                if (rngRoll == 0)
                {
                    WriteMemory(252, 11000); //Prehistoric
                }
                else if (rngRoll == 1)
                {
                    WriteMemory(252, 17000); //Projector Room
                }
                else
                {
                    WriteMemory(252, 37000); //Bedroom
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
    public static bool IsKthBitSet(int n, int k)
    {
        if ((n & (1 << k)) > 0)
        {
            return true;
        } 
        else
        {
            return false;
        }

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
}
