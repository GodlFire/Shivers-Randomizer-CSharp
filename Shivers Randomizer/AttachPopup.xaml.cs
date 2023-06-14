using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static Shivers_Randomizer.utils.AppHelpers;

namespace Shivers_Randomizer;

/// <summary>
/// Interaction logic for AttachPopup.xaml
/// </summary>
public partial class AttachPopup : Window
{
    private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
    private readonly App app;
    private UIntPtr processHandle;
    private UIntPtr MyAddress;

    public AttachPopup(App app)
    {
        InitializeComponent();
        this.app = app;
        app.MyAddress = UIntPtr.Zero;
        app.processHandle = UIntPtr.Zero;
        app.AddressLocated = null;
        app.shiversProcess = null;
        GetProcessList();
    }

    private void Button_GetProcessList_Click(object sender, RoutedEventArgs e)
    {
        GetProcessList();
    }

    private void Button_Attach_Click(object sender, RoutedEventArgs e)
    {
        AttachToProcess();
    }

    private void ListBox_Selection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            button_Attach.IsEnabled = true;
        }
        else if (e.RemovedItems.Count > 0)
        {
            button_Attach.IsEnabled = false;
        }
    }

    private void GetProcessList()
    {
        listBox_Process_List.Items.Clear();
        Process[] processCollection = Process.GetProcessesByName("scummvm")
            .Where(p => p.MainWindowTitle.Contains("Shivers", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (processCollection.Length == 1)
        {
            listBox_Process_List.Items.Add($"Process ID: {processCollection[0].Id} | Process Name: {processCollection[0].MainWindowTitle}");
            listBox_Process_List.SelectedIndex = 0;
            listBox_Process_List.Focus();
            AttachToProcess();
        }
        else if (processCollection.Length > 1)
        {
            foreach (Process p in processCollection)
            {
                listBox_Process_List.Items.Add($"Process ID: {p.Id} | Process Name: {p.MainWindowTitle}");
            }

            listBox_Process_List.SelectedIndex = 0;
            listBox_Process_List.Focus();
            button_Attach.IsEnabled = true;

            if (!IsActive)
            {
                ShowDialog();
            }
        }
        else
        {
            app.AddressLocated = false;
        }
    }

    private void AttachToProcess()
    {
        // ********In release mode there is an infinite loop produced somehow but not in debug mode*********
        // Grab Process ID from selected process
        string? idString = listBox_Process_List.SelectedItem?.ToString();

        if (idString != null)
        {
            Process process = Process.GetProcessById(Convert.ToInt32(idString[12..idString.IndexOf(" | ")]));

            // Obtain a process Handle
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)process.Id);

            // Signature to scan for
            byte[] toFind = new byte[] { 0xD4, 0x00, 0x00, 0x00, 0xC8, 0x1B }; //D4 00 00 00 C8 1B

            // Scan for Signature
            MyAddress = AobScan(processHandle, toFind);

            if (MyAddress != UIntPtr.Zero)
            {
                label_Feedback.Content = $"Shivers Detected! 🙂 {MyAddress.ToUInt64():X}";

                app.MyAddress = MyAddress;
                app.processHandle = processHandle;

                app.AddressLocated = true;

                app.shiversProcess = process;

                Close();
            }
            else
            {
                label_Feedback.Content = "Was unable to connect to Shivers, Did you select shivers?";
                app.AddressLocated = false;
            }
        }
        else
        {
            label_Feedback.Content = "No process selected";
            app.AddressLocated = false;
        }
    }

    private UIntPtr AobScan(UIntPtr processHandle, byte[] pattern)
    {
        List<MEMORY_BASIC_INFORMATION64> memReg = MemInfo(processHandle);
        for (int i = 0; i < memReg.Count; i++)
        {
            byte[] buff = new byte[memReg[i].RegionSize];
            uint refzero = 0;
            ReadProcessMemory(processHandle, memReg[i].BaseAddress, buff, memReg[i].RegionSize, ref refzero);

            UIntPtr result = Scan(memReg, buff, pattern, i);
            if (result != UIntPtr.Zero)
            {
                return new UIntPtr(memReg[i].BaseAddress + result.ToUInt64());
            }
        }

        return UIntPtr.Zero;
    }

    private UIntPtr Scan(List<MEMORY_BASIC_INFORMATION64> memReg, byte[] sIn, byte[] sFor, int memRegionI)
    {
        int pool = 0;
        UIntPtr tempResult;
        int end = sFor.Length - 1;
        int[] sBytes = GetScanBytes(sFor);

        while (pool <= sIn.Length - sFor.Length)
        {
            for (int i = end; sIn[pool + i] == sFor[i]; i--)
            {
                if (i == 0)
                {
                    // If a signiture is found, check at that addess - 0x7C. There is a byte constantly changing. If it is constantly changing then we have found
                    // The correct signature, if not find the next matching signature
                    tempResult = new UIntPtr((uint)pool);
                    uint bytesRead = 0;
                    uint bytesRead2 = 0;
                    byte[] buffer = new byte[1];
                    byte[] buffer2 = new byte[1];
                    ReadProcessMemory(processHandle, memReg[memRegionI].BaseAddress + tempResult.ToUInt64() - 0x7C, buffer, 1, ref bytesRead);
                    Thread.Sleep(50);
                    ReadProcessMemory(processHandle, memReg[memRegionI].BaseAddress + tempResult.ToUInt64() - 0x7C, buffer2, Convert.ToUInt64(buffer2.Length), ref bytesRead2);
                    if (buffer[0] != buffer2[0])
                    {
                        return new UIntPtr((uint)pool);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            pool += sBytes[sIn[pool + end]];
        }

        return UIntPtr.Zero;
    }
}
