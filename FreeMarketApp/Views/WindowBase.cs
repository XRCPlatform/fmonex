using Avalonia.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FreeMarketApp.Views
{
    public class WindowBase : Window
    {
        public void FixWindowCenterPosition()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    var screen = Screens.ScreenFromPoint(Position);
                    if (screen.WorkingArea.IsEmpty)
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual;
                    }
                }
                catch (Exception)
                {

                    //silence please
                }
            }            
        }
    }
}
