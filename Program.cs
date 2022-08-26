﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using RyzenTuner.Common.Container;
using RyzenTuner.UI;

namespace RyzenTuner
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // TODO：测试，切换到英文
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            if (Process.GetProcessesByName("RyzenTuner").Length > 1)
            {
                throw new Exception(Properties.Strings.TextExceptionOnlyOneProgramIsAllowedToRun);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            _handleUnhandledException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs arg)
        {
            var e = (Exception)arg.ExceptionObject;
            _handleUnhandledException(e);
        }

        private static void _handleUnhandledException(Exception ex)
        {
            MessageBox.Show(ex.Message, Properties.Strings.TextExceptionTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            AppContainer.Logger().LogException(ex);

            Application.Exit();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}