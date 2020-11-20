using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacauEngine.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MacauGame
{
    internal class Program
    {
        public static string AppDataFolderName = "CheAle14-Macau";
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);
        public static string GAME_TYPE => AppDataFolderName.ToLower();
        public static Version VERSION { get; set; } = new Version(0, 0, 0);
        public static Random RND { get; private set; }

        #region Config & Saved Preferences
        public static Options Configuration { get; set; }

        public static void SaveConfig()
        {
            var json = JsonConvert.SerializeObject(Configuration);
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
            var path = Path.Combine(AppDataPath, "config.json");
            System.IO.File.WriteAllText(path, json);
        }

        public static void LoadConfig()
        {
            Configuration = new Options();
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
            var path = Path.Combine(AppDataPath, "config.json");
            try
            {
                var json = System.IO.File.ReadAllText(path);
                var obj = JsonConvert.DeserializeObject<Options>(json);
                Configuration = obj;
            } catch (Exception ex)
            {
                Log.Error("Config", ex);
            }
        }
        #endregion

        static void Main(string[] args)
        {
#if DEBUG
            NativeMethods.AllocConsole();
            Console.WriteLine("Debug Console");
#endif
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#if USING_MLAPI
            MLAPI.MasterList.Log = (msg) =>
            {
                Log.Info("MSL", msg);
                return Task.CompletedTask;
            };
#endif
            RND = new Random();
            LoadConfig();
            var menu = new Menu();
            menu.FormClosing += Menu_FormClosing;
            Application.Run(menu);
#if DEBUG
            NativeMethods.FreeConsole();
#endif
        }

        public static string FormatPrefix(int n)
        {
            switch(n)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                default:
                    return $"{n}th";
            }
        }

        private static void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error($"UnhandledEx", (Exception)e.ExceptionObject);
            if (e.IsTerminating)
                Log.Error("UnhandledEx", "We must now exit");
            MessageBox.Show(((Exception)e.ExceptionObject).ToString());
        }

        /// <summary>
        /// From https://stackoverflow.com/a/6803109
        /// </summary>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }

    internal static class NativeMethods
    {
        // http://msdn.microsoft.com/en-us/library/ms681944(VS.85).aspx
        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns>nonzero if the function succeeds; otherwise, zero.</returns>
        /// <remarks>
        /// A process can be associated with only one console,
        /// so the function fails if the calling process already has a console.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();

        // http://msdn.microsoft.com/en-us/library/ms683150(VS.85).aspx
        /// <summary>
        /// Detaches the calling process from its console.
        /// </summary>
        /// <returns>nonzero if the function succeeds; otherwise, zero.</returns>
        /// <remarks>
        /// If the calling process is not already attached to a console,
        /// the error code returned is ERROR_INVALID_PARAMETER (87).
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int FreeConsole();
    }
}
