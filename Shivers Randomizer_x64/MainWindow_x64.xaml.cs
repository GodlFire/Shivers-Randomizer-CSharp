using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Shivers_Randomizer_x64;
using System.Text.RegularExpressions;
using System.Media;

namespace Shivers_Randomizer
{
    /// <summary>
    /// Interaction logic for MainWindow_x64.xaml
    /// </summary>
    public partial class MainWindow_x64 : Window
    {

        [DllImport("KERNEL32.DLL")] public static extern UIntPtr OpenProcess(uint access, bool inheritHandler, uint processId);
        [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool WriteProcessMemory(UIntPtr process, ulong address, byte[] buffer, uint size, ref uint written);
        [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool ReadProcessMemory(UIntPtr process, ulong address, byte[] buffer, ulong size, ref uint read);

        [DllImport("user32.dll")] public static extern bool GetWindowRect(UIntPtr hwnd, ref Rect rectangle);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool IsWindow(UIntPtr hWnd);

        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool IsIconic(UIntPtr hWnd);

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
        Rect ShiversWindowDimensions = new Rect();

        const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        public static UIntPtr processHandle;
        public static UIntPtr MyAddress;
        public static UIntPtr hwndtest;
        public static bool AddressLocated { get; set; }

        public static bool EnableAttachButton;


        int Seed;
        bool setSeedUsed;
        int FailureMessage;
        int ScrambleCount;
        int[] Locations;
        int roomNumber;
        int roomNumberPrevious;
        int numberIxupiCaptured;
        int numberIxupiCapturedTemp;
        int firstToTheOnlyXNumber;
        bool setBethAgain;
        bool nineIxupiEnteringBasement;
        bool finalCutsceneTriggered;

        bool settingsVanilla;
        bool settingsIncludeAsh;
        bool settingsIncludeLightning;
        bool settingsEarlyBeth;
        bool settingsExtraLocations;
        bool settingsExcludeLyre;
        bool settingsEarlyLightning;
        bool settingsRedDoor;
        bool settingsFullPots;
        bool settingsFirstToTheOnlyFive;




        public Overlay_x64 Overlay_x64 = new Overlay_x64();




        public MainWindow_x64()
        {
            InitializeComponent();

            EnableAttachButton = true;

        }





















        private void button_Attach_Click(object sender, RoutedEventArgs e)
        {
            //Display popup for attaching to shivers process
            var attachPopup_x64 = new AttachPopup_x64();
            attachPopup_x64.Show();
        }





        private void button_Scramble_Click(object sender, RoutedEventArgs e)
        {
            settingsVanilla = checkBoxVanilla.IsChecked == true;
            settingsIncludeAsh = checkBoxIncludeAsh.IsChecked == true;
            settingsIncludeLightning = checkBoxIncludeLightning.IsChecked == true;
            settingsEarlyBeth = checkBoxEarlyBeth.IsChecked == true;
            settingsExtraLocations = checkBoxExtraLocations.IsChecked == true;
            settingsExcludeLyre = checkBoxExcludeLyre.IsChecked == true;
            settingsEarlyLightning = checkBoxEarlyLightning.IsChecked == true;
            settingsRedDoor = checkBoxRedDoor.IsChecked == true;
            settingsFullPots = checkBoxFullPots.IsChecked == true;
            settingsFirstToTheOnlyFive = checkBoxFirstToTheOnlyFive.IsChecked == true;

            //Check if seed was entered
            if (txtBox_Seed.Text != "")
            {
                //check if seed is too big, if not use it
                if (!(int.TryParse(txtBox_Seed.Text, out Seed)))
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
            var rngHidden = new Random(Seed); ;
            if (!setSeedUsed)
            {
                Seed = rngHidden.Next();
            }
            var rng = new Random(Seed);

            //Set setBethAgain to false
            setBethAgain = false;


            //If early lightning then set flags for timer
            finalCutsceneTriggered = false;
            nineIxupiEnteringBasement = false;


            Scramble:
            Locations = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };

            //If Vanilla is selected then use the vanilla placement algorithm
            if (settingsVanilla)
            {
                Locations[0] = 212; //Places Ash Top in desk drawer
                Locations[4] = 217; //Places Lighting Top in slide
                Locations[10] = 202; //Places Ash bottom in Greenhouse
                vanillaPlacePiece(200, rng); //Place Water Bottom
                vanillaPlacePiece(201, rng); //Place Wax Bottom
                vanillaPlacePiece(203, rng); //Place Oil Bottom
                vanillaPlacePiece(204, rng); //Place Cloth Bottom
                vanillaPlacePiece(205, rng); //Place Wood Bottom
                vanillaPlacePiece(206, rng); //Place Crystal Bottom
                vanillaPlacePiece(207, rng); //Place Electricity Bottom
                vanillaPlacePiece(208, rng); //Place Sand Bottom
                vanillaPlacePiece(209, rng); //Place Metal Bottom
                vanillaPlacePiece(210, rng); //Place Water Top
                vanillaPlacePiece(211, rng); //Place Wax Top
                vanillaPlacePiece(213, rng); //Place Oil Top
                vanillaPlacePiece(214, rng); //Place Cloth Top
                vanillaPlacePiece(215, rng); //Place Wood Top
                vanillaPlacePiece(216, rng); //Place Crystal Top
                vanillaPlacePiece(218, rng); //Place Sand Top
                vanillaPlacePiece(219, rng); //Place Metal Top
            }
            else if (!settingsFirstToTheOnlyFive) //Normal Scramble
            {
                List<int> PiecesNeededToBePlaced = new List<int>();
                List<int> PiecesRemainingToBePlaced = new List<int>();
                int numberOfRemainingPots = 20;
                int numberOfFullPots = 0;

                //Check if ash is added to the scramble
                if (!settingsIncludeAsh)
                {
                    Locations[0] = 212; //Places Ash Top in desk drawer
                    Locations[10] = 202; //Places Ash bottom in Greenhouse
                    numberOfRemainingPots -= 2;
                }
                //Check if lighting is added to the scramble
                if (!settingsIncludeLightning)
                {
                    Locations[4] = 217; //Places Lighting Top in slide
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
                        FullPotRolled = rng.Next(220, 230);//Grab a random pot
                        if (FullPotRolled == 222 || FullPotRolled == 227)//Make sure its not ash or lightning
                        {
                            goto RollFullPot;
                        }

                        if (PiecesNeededToBePlaced.Contains(FullPotRolled) == true)//Make sure it wasnt already selected
                        {
                            goto RollFullPot;
                        }
                        PiecesNeededToBePlaced.Add(FullPotRolled);
                        numberOfRemainingPots -= 2;
                    }
                    if (rng.Next(0, 2) == 1 && settingsIncludeAsh) //Is ash completed
                    {
                        PiecesNeededToBePlaced.Add(222);
                        numberOfRemainingPots -= 2;
                    }
                    if (rng.Next(0, 2) == 1 && settingsIncludeLightning) //Is lighting completed
                    {
                        PiecesNeededToBePlaced.Add(227);
                        numberOfRemainingPots -= 2;
                    }
                }

                int pieceBeingAddedToList; //Add remaining peices to list
                while (numberOfRemainingPots != 0)
                {
                    pieceBeingAddedToList = rng.Next(0, 20) + 200;
                    //Check if piece already added to list
                    //Check if piece was ash and ash not included in scramble
                    //Check if piece was lighting top and lightning not included in scramble
                    if ((PiecesNeededToBePlaced.Contains(pieceBeingAddedToList)) ||
                        ((pieceBeingAddedToList == 202 || pieceBeingAddedToList == 212) && !settingsIncludeAsh) ||
                        ((pieceBeingAddedToList == 217) && !settingsIncludeLightning))
                    {
                        continue;
                    }
                    //Check if completed pieces are used and the base pieces are rolled
                    if ((pieceBeingAddedToList < 210 && PiecesNeededToBePlaced.Contains(pieceBeingAddedToList + 20)) || (pieceBeingAddedToList > 209 && PiecesNeededToBePlaced.Contains(pieceBeingAddedToList + 10)))
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
                    if (!settingsExtraLocations && (RandomLocation == 2 || RandomLocation == 6 || RandomLocation == 13)) //Check if extra locations are used
                    {
                        continue;
                    }
                    if (settingsExcludeLyre && settingsExtraLocations && numberOfFullPots == 0 && RandomLocation == 14)//Check if lyre excluded
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
                //Check if cloth behind cloth
                //Check if oil behind oil
                //Check if cloth behind oil AND oil behind cloth
                if ((Locations[8] == 203 || Locations[8] == 213 || Locations[8] == 223) ||
                    (Locations[17] == 204 || Locations[17] == 214 || Locations[17] == 224) ||
                    ((Locations[17] == 203 || Locations[17] == 213 || Locations[17] == 223) && (Locations[8] == 204 || Locations[8] == 214 || Locations[8] == 224)))
                {
                    goto Scramble;
                }
            }
            else if (settingsFirstToTheOnlyFive) //First to the only X
            {
                List<int> PiecesNeededToBePlaced = new List<int>();
                List<int> PiecesRemainingToBePlaced = new List<int>();

                //Get number of sets
                firstToTheOnlyXNumber = Int32.Parse(txtBox_FirstToTheOnlyX.Text);
                int numberOfRemainingPots = 2 * firstToTheOnlyXNumber;

                //Check for invalid numbers
                if(numberOfRemainingPots == 0) //No Sets
                {
                    FailureMessage = 2;
                    goto Failure;
                }
                else if(numberOfRemainingPots == 2 && !settingsIncludeAsh && !settingsIncludeLightning) //1 set but didnt not include ash or lightning in scramble
                {
                    FailureMessage = 3;
                    goto Failure;
                }

                //If 1 set and either IncludeAsh/IncludeLighting is false then force the other. Else roll randomly from all available pots
                if (numberOfRemainingPots == 2 && (settingsIncludeAsh | settingsIncludeLightning))
                {
                    if(!settingsIncludeAsh)//Force lightning
                    {
                        PiecesNeededToBePlaced.Add(207);
                        Locations[4] = 217; //Places Lighting Top in slide
                    }
                    else if(!settingsIncludeLightning)//Force Ash
                    {
                        Locations[0] = 212; //Places Ash Top in desk drawer
                        Locations[10] = 202; //Places Ash bottom in Greenhouse
                    }
                }
                else
                {
                    string[] SetsAvailable =  new string[] {"Water", "Wax", "Ash", "Oil", "Cloth", "Wood", "Crystal", "Lightning", "Sand", "Metal"};

                    //Determine which sets will be included in the scramble
                    //First check if lighting/ash are included in the scramble. if not force them
                    if (!settingsIncludeAsh)
                    {
                        Locations[0] = 212; //Places Ash Top in desk drawer
                        Locations[10] = 202; //Places Ash bottom in Greenhouse
                        numberOfRemainingPots -= 2;
                        SetsAvailable[2] = "";
                    }
                    if(!settingsIncludeLightning)
                    {
                        PiecesNeededToBePlaced.Add(207);
                        Locations[4] = 217; //Places Lighting Top in slide
                        numberOfRemainingPots -= 2;
                        SetsAvailable[7] = "";
                    }

                    //Next select from the remaining sets available
                    while(numberOfRemainingPots > 0)
                    {
                        int setSelected = 0;
                        //Pick a set
                        setSelected = rng.Next(0, 10);
                        switch (setSelected)
                        {
                            case 0: //Water
                                if (SetsAvailable.Any(s => s.Contains("Water") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(220);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(200);
                                        PiecesNeededToBePlaced.Add(210);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[0] = "";
                                }
                                    break;
                            case 1: //Wax
                                if (SetsAvailable.Any(s => s.Contains("Wax") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(221);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(201);
                                        PiecesNeededToBePlaced.Add(211);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[1] = "";
                                }
                                break;
                            case 2: //Ash
                                if (SetsAvailable.Any(s => s.Contains("Ash") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(222);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(202);
                                        PiecesNeededToBePlaced.Add(212);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[2] = "";
                                }
                                break;
                            case 3: //Oil
                                if (SetsAvailable.Any(s => s.Contains("Oil") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(223);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(203);
                                        PiecesNeededToBePlaced.Add(213);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[3] = "";
                                }
                                break;
                            case 4: //Cloth
                                if (SetsAvailable.Any(s => s.Contains("Cloth") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(224);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(204);
                                        PiecesNeededToBePlaced.Add(214);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[4] = "";
                                }
                                break;
                            case 5: //Wood
                                if (SetsAvailable.Any(s => s.Contains("Wood") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(225);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(205);
                                        PiecesNeededToBePlaced.Add(215);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[5] = "";
                                }
                                break;
                            case 6: //Crystal
                                if (SetsAvailable.Any(s => s.Contains("Crystal") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(226);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(206);
                                        PiecesNeededToBePlaced.Add(216);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[6] = "";
                                }
                                break;
                            case 7: //Lightning
                                if (SetsAvailable.Any(s => s.Contains("Lightning") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(227);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(207);
                                        PiecesNeededToBePlaced.Add(217);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[7] = "";
                                }
                                break;
                            case 8: //Sand
                                if (SetsAvailable.Any(s => s.Contains("Sand") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(228);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(208);
                                        PiecesNeededToBePlaced.Add(218);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[8] = "";
                                }
                                break;
                            case 9: //Metal
                                if (SetsAvailable.Any(s => s.Contains("Metal") == true))
                                {
                                    //Check/roll for full pot
                                    if (settingsFullPots && rng.Next(0, 2) == 1)
                                    {
                                        PiecesNeededToBePlaced.Add(229);
                                    }
                                    else
                                    {
                                        PiecesNeededToBePlaced.Add(209);
                                        PiecesNeededToBePlaced.Add(219);
                                    }

                                    numberOfRemainingPots -= 2;
                                    SetsAvailable[9] = "";
                                }
                                break;
                        }
                    }
                    int RandomLocation;
                    PiecesRemainingToBePlaced = new List<int>(PiecesNeededToBePlaced);
                    while (PiecesRemainingToBePlaced.Count > 0)
                    {
                        RandomLocation = rng.Next(0, 23);
                        if (!settingsExtraLocations && (RandomLocation == 2 || RandomLocation == 6 || RandomLocation == 13)) //Check if extra locations are used
                        {
                            continue;
                        }
                        if (settingsExcludeLyre && RandomLocation == 14)//Check if lyre excluded
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
                    //Check if cloth behind cloth
                    //Check if oil behind oil
                    //Check if cloth behind oil AND oil behind cloth
                    //Check if a piece behind cloth with no cloth pot available
                    //Check if a piece behind oil with no oil pot available
                    if ((Locations[8] == 203 || Locations[8] == 213 || Locations[8] == 223) ||
                        (Locations[17] == 204 || Locations[17] == 214 || Locations[17] == 224) ||
                        ((Locations[17] == 203 || Locations[17] == 213 || Locations[17] == 223) && (Locations[8] == 204 || Locations[8] == 214 || Locations[8] == 224)) ||
                        (Locations[8] != 0 && !Locations.Contains(203) && !Locations.Contains(213) && !Locations.Contains(223)) ||
                        (Locations[17] != 0 && !Locations.Contains(204) && !Locations.Contains(214) && !Locations.Contains(224)))
                    {
                        goto Scramble;
                    }
                }
            }

            //Place pieces in memory
            PlacePieces();


            //Set bytes for mailbox/red door/beth. Only mailbox is set if vanilla shuffle is selected
            //This is now obsolete. If the room number isnt 922 then the scramble button isnt enabled. Thus if the randomizer didnt work the scramble button would never enable
            //WriteProcessMemory(processHandle, (ulong)MyAddress + 369, BitConverter.GetBytes(84), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Mailbox 
            uint bytesWritten = 0;
            if (!settingsVanilla)
            {
                if (settingsRedDoor)
                {
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 364, BitConverter.GetBytes(128), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Red door
                }
                if (settingsEarlyBeth)
                {
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 381, BitConverter.GetBytes(128), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Beth
                }
            }

            //Set ixupi captured number
            if(settingsFirstToTheOnlyFive)
            {
                WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(10 - firstToTheOnlyXNumber), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
            }
            else//Set to 0 if not running First to The Only X
            {
                WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(0), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
            }


            ScrambleCount += 1;
            label_ScrambleFeedback.Content = "Scramble Number: " + ScrambleCount;



            //Set info for overlay
            Overlay_x64.SetInfo(Seed, setSeedUsed, settingsVanilla, settingsIncludeAsh, settingsIncludeLightning, settingsEarlyBeth, settingsExtraLocations, 
                settingsExcludeLyre, settingsEarlyLightning, settingsRedDoor, settingsFullPots, settingsFirstToTheOnlyFive);

            //Set Seed info and flagset info
            label_Seed.Content = "Seed: " + Seed;
            label_Flagset.Content = "Flagset: " + Overlay_x64.flagset;


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

        public void PlacePieces()
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
            21 = Hanging / Gallows
            22 = Clock
            */

            uint bytesWritten = 0;
            WriteProcessMemory(processHandle, (ulong)MyAddress + 0, BitConverter.GetBytes(Locations[0]), (uint)BitConverter.GetBytes(Locations[0]).Length, ref bytesWritten); //Desk Drawer
            WriteProcessMemory(processHandle, (ulong)MyAddress + 8, BitConverter.GetBytes(Locations[1]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);  //Workshop
            WriteProcessMemory(processHandle, (ulong)MyAddress + 16, BitConverter.GetBytes(Locations[2]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Library Cupboard
            WriteProcessMemory(processHandle, (ulong)MyAddress + 24, BitConverter.GetBytes(Locations[3]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Library Statue
            WriteProcessMemory(processHandle, (ulong)MyAddress + 32, BitConverter.GetBytes(Locations[4]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Slide
            WriteProcessMemory(processHandle, (ulong)MyAddress + 40, BitConverter.GetBytes(Locations[5]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Eagle
            WriteProcessMemory(processHandle, (ulong)MyAddress + 48, BitConverter.GetBytes(Locations[6]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Eagles Nest
            WriteProcessMemory(processHandle, (ulong)MyAddress + 56, BitConverter.GetBytes(Locations[7]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Ocean
            WriteProcessMemory(processHandle, (ulong)MyAddress + 64, BitConverter.GetBytes(Locations[8]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Tar River
            WriteProcessMemory(processHandle, (ulong)MyAddress + 72, BitConverter.GetBytes(Locations[9]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Theater
            WriteProcessMemory(processHandle, (ulong)MyAddress + 80, BitConverter.GetBytes(Locations[10]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Green House / Plant Room
            WriteProcessMemory(processHandle, (ulong)MyAddress + 88, BitConverter.GetBytes(Locations[11]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Egypt
            WriteProcessMemory(processHandle, (ulong)MyAddress + 96, BitConverter.GetBytes(Locations[12]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Chinese Solitaire
            WriteProcessMemory(processHandle, (ulong)MyAddress + 104, BitConverter.GetBytes(Locations[13]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Tiki Hut
            WriteProcessMemory(processHandle, (ulong)MyAddress + 112, BitConverter.GetBytes(Locations[14]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Lyre
            WriteProcessMemory(processHandle, (ulong)MyAddress + 120, BitConverter.GetBytes(Locations[15]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Skeleton
            WriteProcessMemory(processHandle, (ulong)MyAddress + 128, BitConverter.GetBytes(Locations[16]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Anansi
            WriteProcessMemory(processHandle, (ulong)MyAddress + 136, BitConverter.GetBytes(Locations[17]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Janitor Closet
            WriteProcessMemory(processHandle, (ulong)MyAddress + 144, BitConverter.GetBytes(Locations[18]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //UFO
            WriteProcessMemory(processHandle, (ulong)MyAddress + 152, BitConverter.GetBytes(Locations[19]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Alchemy
            WriteProcessMemory(processHandle, (ulong)MyAddress + 160, BitConverter.GetBytes(Locations[20]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Puzzle Room
            WriteProcessMemory(processHandle, (ulong)MyAddress + 168, BitConverter.GetBytes(Locations[21]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Hanging / Gallows
            WriteProcessMemory(processHandle, (ulong)MyAddress + 176, BitConverter.GetBytes(Locations[22]), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Clock Tower
        }

        public void DispatcherTimer()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {

            GetWindowRect(hwndtest, ref ShiversWindowDimensions);
            Overlay_x64.xCoord = ShiversWindowDimensions.Left;
            Overlay_x64.yCoord = ShiversWindowDimensions.Top;
            Overlay_x64.isMinimized = IsIconic(hwndtest);


            //Check if a window exists, if not hide the overlay
            if (!IsWindow(hwndtest))
            {
                Overlay_x64.Hide();
            }
            else
            {
                Overlay_x64.Show();
            }


            uint bytesRead = 0;
            uint bytesWritten = 0;
            byte[] buffer = new byte[2];
            int tempRoomNumber;

            //Monitor Room Number
            if(MyAddress != (UIntPtr)0x0 && processHandle != (UIntPtr)0x0) //Throws an exception if not checked in release mode.
            {
                ReadProcessMemory(processHandle, (ulong)MyAddress - 424, buffer, (ulong)buffer.Length, ref bytesRead);
                tempRoomNumber = buffer[0] + (buffer[1] << 8);
                if (tempRoomNumber != roomNumber)
                {
                    roomNumberPrevious = roomNumber;
                    roomNumber = tempRoomNumber;
                }
                label_roomPrev.Content = roomNumberPrevious;
                label_room.Content = roomNumber;
            }
                

            



            //If room number is 910 or 922 update the status text. If room number is not 922 disable the scramble button.
            if (roomNumber == 910 || roomNumber == 922)
            {
                label_ShiversDetected.Content = "Shivers Detected! :)";
                if (roomNumber == 922)
                {
                    button_Scramble.IsEnabled = true;
                }
                else
                {
                    button_Scramble.IsEnabled = false;
                }
            }

            //If early lightning is enabled, check capture number to allow capturing lightning. 
            if (settingsEarlyLightning && !settingsVanilla)
            {
                ReadProcessMemory(processHandle, (ulong)MyAddress + 1712, buffer, (ulong)buffer.Length, ref bytesRead);
                numberIxupiCaptured = buffer[0];

                //Entering Basement, Set Ixupi Number to 9 to spawn lightning
                if (roomNumber == 10290 && numberIxupiCaptured != 9)
                {
                    //Store Ixupi number temporarily
                    numberIxupiCapturedTemp = numberIxupiCaptured;
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(9), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                }


                //In basement, set Ixupi number to 0 to not trigger end cutscene
                if (roomNumber == 39010 && roomNumberPrevious == 10290)
                {
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(0), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                }
                //Exiting basement
                //Lightning Caught
                ReadProcessMemory(processHandle, (ulong)MyAddress + 1712, buffer, (ulong)buffer.Length, ref bytesRead);
                numberIxupiCaptured = buffer[0];
                if (roomNumber == 10300 && roomNumberPrevious == 39030 && numberIxupiCaptured == 1)
                {
                    //If Lightning is not the first ixupi caught then increment Ixupi counter, if he is then dont do anything.
                    if (numberIxupiCapturedTemp != 0)
                    {
                        WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(numberIxupiCapturedTemp + 1), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                    }
                }
                //Lightning not caught
                else if (roomNumber == 10300 && roomNumberPrevious == 39030 && numberIxupiCaptured == 0)
                {
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(numberIxupiCapturedTemp), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                    numberIxupiCapturedTemp = 0;
                }
                //Never entered basement
                else if ((roomNumber == 10310 && roomNumberPrevious == 10290) || (roomNumber == 10280 && roomNumberPrevious == 10290))
                {
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(numberIxupiCapturedTemp), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                    ReadProcessMemory(processHandle, (ulong)MyAddress + 1712, buffer, (ulong)buffer.Length, ref bytesRead);
                    numberIxupiCaptured = buffer[0];
                }
                //Entered basement with 9 ixupi already
                if (roomNumber != 10290 && numberIxupiCaptured == 9)
                {
                    nineIxupiEnteringBasement = true;
                }
                //If 10 Ixupi Caught then trigger final cutscene
                ReadProcessMemory(processHandle, (ulong)MyAddress + 1712, buffer, (ulong)buffer.Length, ref bytesRead);
                numberIxupiCaptured = buffer[0];
                if ((numberIxupiCaptured == 10 && finalCutsceneTriggered == false) || ((numberIxupiCaptured == 1) && nineIxupiEnteringBasement && finalCutsceneTriggered == false))
                {
                    //If moved properly to final cutscene, disable the trigger for final cutscene
                    finalCutsceneTriggered = true;
                    WriteProcessMemory(processHandle, (ulong)MyAddress - 424, BitConverter.GetBytes(935), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                }

                //If early beth is not enabled and ixupi count was 9 entering the basement, set the beth flag again
                if ((roomNumber != 10290 && numberIxupiCaptured == 9 && !settingsEarlyBeth) | setBethAgain == true)
                {
                    setBethAgain = true;
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 381, BitConverter.GetBytes(128), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten); //Beth
                    WriteProcessMemory(processHandle, (ulong)MyAddress + 1712, BitConverter.GetBytes(9), (uint)BitConverter.GetBytes(211).Length, ref bytesWritten);
                }

                label_ixupidNumber.Content = numberIxupiCaptured;

            }
        }

        





        void vanillaPlacePiece(int potPiece, Random rng)
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
                    locationRand = 0;

                //Check if piece is cloth and location is janitors closest
                if (potPiece == 204 || potPiece == 214)
                {
                    if (locationRand == 17)
                    {
                        locationRand += 1;
                        continue;
                    }
                }
                //Checking oil is in the bathroom or tar river
                if (potPiece == 203 || potPiece == 213)
                {
                    if (locationRand == 8 || locationRand == 17)
                    {
                        locationRand += 1;
                        continue;
                    }
                }
                //For extra locations, is disabled in vanilla
                if (1 == 1 && (locationRand == 2 || locationRand == 6 || locationRand == 13))
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




        private void checkBoxVanilla_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxVanilla.IsChecked == true)
            {
                checkBoxIncludeAsh.IsEnabled = false;
                checkBoxIncludeLightning.IsEnabled = false;
                checkBoxEarlyBeth.IsEnabled = false;
                checkBoxExtraLocations.IsEnabled = false;
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxEarlyLightning.IsEnabled = false;
                checkBoxRedDoor.IsEnabled = false;
                checkBoxFullPots.IsEnabled = false;
                checkBoxFirstToTheOnlyFive.IsEnabled = false;
            }
            else
            {
                checkBoxIncludeAsh.IsEnabled = true;
                checkBoxIncludeLightning.IsEnabled = true;
                checkBoxExtraLocations.IsEnabled = true;
                checkBoxExcludeLyre.IsEnabled = true;
                checkBoxEarlyLightning.IsEnabled = true;
                checkBoxRedDoor.IsEnabled = true;
                checkBoxFullPots.IsEnabled = true;
                checkBoxFirstToTheOnlyFive.IsEnabled = true;
            }
        }

        private void checkBoxExtraLocations_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxExtraLocations.IsChecked == true || checkBoxFullPots.IsChecked == true)
            {
                checkBoxExcludeLyre.IsEnabled = true;
            }
            else
            {
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxExcludeLyre.IsChecked = false;
            }
        }
        private void checkBoxFullPots_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxFullPots.IsChecked == true || checkBoxExtraLocations.IsChecked == true)
            {
                checkBoxExcludeLyre.IsEnabled = true;
            }
            else
            {
                checkBoxExcludeLyre.IsEnabled = false;
                checkBoxExcludeLyre.IsChecked = false;
            }
        }
        private void checkBoxIncludeLightning_Click(object sender, RoutedEventArgs e)
        {
            //If lighting is included in scramble and no early lighting capture allowed early beth must be enabled. If you dont you cant get 9 captures to open beth if there is a non lighting piece in slide
            if (checkBoxIncludeLightning.IsChecked == true && checkBoxEarlyLightning.IsChecked == false)
            {
                checkBoxEarlyBeth.IsEnabled = false;
                checkBoxEarlyBeth.IsChecked = true;
            }
            else
            {
                checkBoxEarlyBeth.IsEnabled = true;
            }
            //If lightningt is not included in scramble no point to have early lightning capture enabled
            if (checkBoxIncludeLightning.IsChecked == false)
            {
                checkBoxEarlyLightning.IsEnabled = false;
                checkBoxEarlyLightning.IsChecked = false;
            }
            else
            {
                checkBoxEarlyLightning.IsEnabled = true;
            }
        }

        private void checkBoxEarlyLightning_Click(object sender, RoutedEventArgs e)
        {
            //If lighting is included in scramble and no early lighting capture allowed early beth must be enabled. If you dont you cant get 9 captures to open beth if there is a non lighting piece in slide
            if (checkBoxIncludeLightning.IsChecked == true && checkBoxEarlyLightning.IsChecked == false)
            {
                checkBoxEarlyBeth.IsEnabled = false;
                checkBoxEarlyBeth.IsChecked = true;
            }
            else
            {
                checkBoxEarlyBeth.IsEnabled = true;
            }
        }












        private void button_Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Welcome to Shivers Randomizer 2.3\n\nHow to use:\n1. Launch Shivers\n2. Attach process to shivers \n3. " +
                "Press New Game (In Shivers)\n4. Change Settings as desired\n5. Press scramble\n\n The scramble button will only enable on the registry page.\n\n If you load a game or restart shivers the randomizer must also be restarted.");
        }

        //Allows only numbers in the seed box input
        private void NumbersValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }





        private void button_Write_Click(object sender, RoutedEventArgs e)
        {
            uint bytesWritten = 0;
            byte[] buffer = BitConverter.GetBytes(Convert.ToInt32(txtBox_WriteValue.Text));


            WriteProcessMemory(processHandle, (ulong)MyAddress, buffer, (uint)buffer.Length, ref bytesWritten);
        }

        private void button_Read_Click(object sender, RoutedEventArgs e)
        {
            uint bytesRead = 0;
            byte[] buffer = new byte[1];



            ReadProcessMemory(processHandle, (ulong)MyAddress, buffer, (ulong)buffer.Length, ref bytesRead);

            label_Value.Content = buffer[0];
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            DispatcherTimer();
            
            var rng = new Random();
            if (rng.Next() % 100 == 0)
            {
                SoundPlayer player = new SoundPlayer(Shivers_Randomizer_x64.Properties.Resources.Siren);
                player.Load();
                player.Play();
            }
            
            
        }
    }
}

