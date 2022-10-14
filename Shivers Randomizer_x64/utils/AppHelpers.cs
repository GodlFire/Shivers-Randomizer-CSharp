using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Shivers_Randomizer_x64.utils;

internal static class AppHelpers
{
    [DllImport("user32.dll")] public static extern bool GetWindowRect(UIntPtr hwnd, ref RectSpecial rectangle);
    [DllImport("user32.dll")] public static extern bool PostMessage(UIntPtr hWnd, uint Msg, int wParam, int lParam);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern bool IsIconic(UIntPtr hWnd);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] public static extern bool IsWindow(UIntPtr hWnd);

    [DllImport("KERNEL32.DLL")] public static extern UIntPtr OpenProcess(uint access, bool inheritHandler, uint processId);
    [DllImport("KERNEL32.DLL")] public static extern int VirtualQueryEx(UIntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, int dwLength);
    [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool ReadProcessMemory(UIntPtr process, ulong address, byte[] buffer, ulong size, ref uint read);
    [DllImport("KERNEL32.DLL", SetLastError = true)] public static extern bool WriteProcessMemory(UIntPtr process, ulong address, byte[] buffer, uint size, ref uint written);

    public static int MakeLParam(int x, int y) => y << 16 | x & 0xFFFF;

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public int AllocationProtect;
        public int __alignment1;
        public ulong RegionSize;
        public int State;
        public int Protect;
        public int Type;
        public int __alignment2;
    }

    public struct RectSpecial
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }
}