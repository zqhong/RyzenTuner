using System;
using System.Windows.Forms;
using RyzenTuner.Common.Container;
using RyzenTuner.UI;
using RyzenTuner.Utils;

namespace RyzenTuner
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}