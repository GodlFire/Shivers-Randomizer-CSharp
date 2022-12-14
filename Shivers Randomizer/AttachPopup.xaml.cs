using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using static Shivers_Randomizer.utils.AppHelpers;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for AttachPopup.xaml
/// </summary>
public partial class AttachPopup : Window
{
    private Process[] processCollection = Array.Empty<Process>();

    private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    private readonly App app;
    private UIntPtr processHandle;
    private UIntPtr MyAddress;
    private List<MEMORY_BASIC_INFORMATION64> MemReg = new();

    public AttachPopup(App app)
    {
        InitializeComponent();
        this.app = app;
        GetProcessList();
    }

    private void Button_GetProcessList_Click(object sender, RoutedEventArgs e)
    {
        GetProcessList();
    }

    private void Button_Attach_Click(object sender, RoutedEventArgs e)
    {
        //********In release mode there is an infinite loop produced somehow but not in debug mode*********
        //Grab Process ID from selected process
        string? idString = listBox_Process_List.SelectedItem?.ToString();

        if (idString != null)
        {
            Process process = Process.GetProcessById(Convert.ToInt32(idString[13..idString.IndexOf(" P")]));

            //Obtain a process Handle
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)process.Id);

            //Signature to scan for
            byte[] toFind = new byte[] { 0xD4, 0x00, 0x00, 0x00, 0xC8, 0x1B }; //D4 00 00 00 C8 1B

            //Scan for Signature
            MyAddress = AobScan("scummvm", toFind);

            if (MyAddress != UIntPtr.Zero)
            {
                label_Feedback.Content = "Shivers Detected! :)" + MyAddress.ToUInt64().ToString("X");

                app.MyAddress = MyAddress;
                app.processHandle = processHandle;

                app.AddressLocated = true;
                app.EnableAttachButton = false;

                app.hwndtest = (UIntPtr)(long)process.MainWindowHandle;

                Close();
            }
            else
            {
                label_Feedback.Content = "Was unable to connect to Shivers, Did you select shivers?";
            }
        }
        else
        {
            label_Feedback.Content = "No process selected";
        }
    }

    private void GetProcessList()
    {
        processCollection = Array.Empty<Process>();
        listBox_Process_List.Items.Clear();
        processCollection = Process.GetProcessesByName("scummvm");
        foreach (Process p in processCollection)
        {
            listBox_Process_List.Items.Add("Process ID : " + p.Id + " Process Name: " + p.MainWindowTitle);
        }
    }

    public UIntPtr AobScan(string ProcessName, byte[] Pattern)
    {
        Process[] P = Process.GetProcessesByName(ProcessName);
        if (P.Length == 0)
        {
            return UIntPtr.Zero;
        }

        MemReg = new List<MEMORY_BASIC_INFORMATION64>();
        MemInfo((UIntPtr)(long)P[0].Handle);
        for (int i = 0; i < MemReg.Count; i++)
        {
            byte[] buff = new byte[MemReg[i].RegionSize];
            uint refzero = 0;
            ReadProcessMemory((UIntPtr)(long)P[0].Handle, MemReg[i].BaseAddress, buff, MemReg[i].RegionSize, ref refzero);

            UIntPtr Result = Scan(buff, Pattern, i);
            if (Result != UIntPtr.Zero)
            {
                return new UIntPtr(MemReg[i].BaseAddress + Result.ToUInt64());
            }
        }

        return UIntPtr.Zero;
    }

    public void MemInfo(UIntPtr pHandle)
    {
        UIntPtr Addy = new();
        while (true)
        {
            MEMORY_BASIC_INFORMATION64 MemInfo = new();
            int MemDump = VirtualQueryEx(pHandle, Addy, out MemInfo, Marshal.SizeOf(MemInfo));
            if (MemDump == 0)
            {
                break;
            }

            if ((MemInfo.State & 0x1000) != 0 && (MemInfo.Protect & 0x100) == 0)
            {
                MemReg.Add(MemInfo);
            }

            Addy = new UIntPtr(MemInfo.BaseAddress + MemInfo.RegionSize);
        }
    }

    public UIntPtr Scan(byte[] sIn, byte[] sFor, int memRegionI)
    {
        UIntPtr tempResult;
        int[] sBytes = new int[256];
        int Pool = 0;
        int End = sFor.Length - 1;
        for (int i = 0; i < 256; i++)
        {
            sBytes[i] = sFor.Length;
        }

        for (int i = 0; i < End; i++)
        {
            sBytes[sFor[i]] = End - i;
        }

        while (Pool <= sIn.Length - sFor.Length)
        {
            for (int i = End; sIn[Pool + i] == sFor[i]; i--)
            {
                if (i == 0)
                {
                    //If a signiture is found, check at that addess - 0x7C. There is a byte constantly changing. If it is constantly changing then we have found
                    //The correct signature, if not find the next matching signature
                    tempResult = new UIntPtr((uint)Pool);
                    uint bytesRead = 0;
                    uint bytesRead2 = 0;
                    byte[] buffer = new byte[1];
                    byte[] buffer2 = new byte[1];
                    ReadProcessMemory(processHandle, MemReg[memRegionI].BaseAddress + tempResult.ToUInt64() - 0x7C, buffer, 1, ref bytesRead);
                    System.Threading.Thread.Sleep(50);
                    ReadProcessMemory(processHandle, MemReg[memRegionI].BaseAddress + tempResult.ToUInt64() - 0x7C, buffer2, Convert.ToUInt64(buffer2.Length), ref bytesRead2);
                    if (buffer[0] != buffer2[0])
                    {
                        return new UIntPtr((uint)Pool);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Pool += sBytes[sIn[Pool + End]];
        }

        return UIntPtr.Zero;
    }
}
