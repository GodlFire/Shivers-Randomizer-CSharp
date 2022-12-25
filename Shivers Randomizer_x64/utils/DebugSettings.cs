using System.Windows;

namespace Shivers_Randomizer.utils;

internal static class DebugSettings
{
    public static Visibility Visibility
    {
#if DEBUG
        get { return Visibility.Visible; }
#else
        get { return Visibility.Collapsed; }
#endif
    }

    public static double MainWindowSize
    {
#if DEBUG
        get { return 960; }
#else
        get { return 740; }
#endif
    }
}
