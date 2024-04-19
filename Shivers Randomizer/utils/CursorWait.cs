using System;
using System.IO;
using System.Windows.Input;

namespace Shivers_Randomizer.utils;
internal class CursorWait : IDisposable
{
    private readonly Cursor? saved = null;
    private readonly Cursor waitCursor = new(new MemoryStream(Properties.Resources.ShiversBusy));

    public CursorWait()
    {
        saved = Mouse.OverrideCursor;
        Mouse.OverrideCursor = waitCursor;
    }

    public void Dispose()
    {
        Mouse.OverrideCursor = saved;
    }
}
