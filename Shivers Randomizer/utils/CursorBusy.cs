using System;
using System.IO;
using System.Windows.Input;
using static Shivers_Randomizer.utils.AppHelpers;

namespace Shivers_Randomizer.utils;
internal class CursorBusy : IDisposable
{
    private readonly Cursor? saved = null;
    private readonly Cursor busyCursor = new(new MemoryStream(Properties.Resources.ShiversBusy));
    private readonly UIntPtr? hWnd = null;

    public CursorBusy(UIntPtr? windowToDisable = null)
    {
        hWnd = windowToDisable;
        BlockInput(true);
        if (hWnd.HasValue)
        {
            EnableWindow(hWnd.Value, false);
        }
        
        saved = Mouse.OverrideCursor;
        Mouse.OverrideCursor = busyCursor;
    }

    public void Dispose()
    {
        Mouse.OverrideCursor = saved;
        BlockInput(false);
        if (hWnd.HasValue)
        {
            EnableWindow(hWnd.GetValueOrDefault(), true);
        }
    }
}
