﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CustomRPC
{
    // https://www.codeproject.com/Articles/32908/C-Single-Instance-App-With-the-Ability-To-Restore
    static public class WinApi
    {
        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
    }

    static class Program
    {
        public static Mutex AppMutex;

        public static int WM_SHOWFIRSTINSTANCE;
        public static int WM_IMPORTPRESET;

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            string mutexName = "CustomRP dev";
#else
            string mutexName = "CustomRP";
#endif
            WM_SHOWFIRSTINSTANCE = WinApi.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|" + mutexName);
            WM_IMPORTPRESET = WinApi.RegisterWindowMessage("WM_IMPORTPRESET|" + mutexName);

            AppMutex = new Mutex(true, mutexName, out bool createdNew);

            var settings = Properties.Settings.Default;

            if (!createdNew)
            {
                WinApi.PostMessage(new IntPtr(0xffff), WM_SHOWFIRSTINSTANCE, IntPtr.Zero, IntPtr.Zero);

                if (args.Length > 0)
                {
                    try
                    {
                        File.Copy(args[0], Path.GetTempPath() + "preset.crp", true);
                        WinApi.PostMessage(new IntPtr(0xffff), WM_IMPORTPRESET, IntPtr.Zero, IntPtr.Zero);
                    }
                    catch
                    {
                        // Do nothing
                    }
                }

                return;
            }

            string culture = "auto";

            try
            {
                // In case the settings file goes corrupt, trying to read any property will throw an exception.
                culture = settings.language;
            }
            catch (System.Configuration.ConfigurationErrorsException e)
            {
                var result = MessageBox.Show(Strings.errorCorruptSettings, Strings.error, MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    // I can't just do settings.Reset() since it will throw the same exception, so instead I have to delete the config file.
                    string filename = ((System.Configuration.ConfigurationErrorsException)e.InnerException).Filename;
                    System.IO.File.Delete(filename);
                    AppMutex.Close();
                    Application.Restart(); 
                }
                return;
            }

            if (culture != "auto") CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(culture);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            IntPtr _ = new MainForm(args.Length > 0 ? args[0] : "none").Handle; // Terrible, yet allows to fully initialize the form without showing it first

            Application.Run();

            GC.KeepAlive(AppMutex);
        }
    }
}