using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Shivers_Randomizer_x64.utils;
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
        get { return 835; }
#else
        get { return 650; }
#endif
    }
}
