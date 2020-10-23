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
using Squirrel;

[assembly: System.Reflection.AssemblyMetadata("SquirrelAwareVersion", "1")]
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
            SquirrelAwareApp.HandleEvents(onInitialInstall, onAppUpdate, onAppObsoleted, onAppUninstall, onFirstRun);
#if USING_MLAPI
            MLAPI.MasterList.Log = (msg) =>
            {
                Log.Info("MSL", msg);
                return Task.CompletedTask;
            };
#endif
            RND = new Random();
#if DEBUG
            Log.Info($"Skipping version checks due to debug configuration.");
#else
            Log.Info($"Started, checking updates");
            Log.Info("Other things");
            Main().GetAwaiter().GetResult();
            Log.Info("Other things2");
#endif
            LoadConfig();
            var menu = new Menu();
            menu.FormClosing += Menu_FormClosing;
            Application.Run(menu);
#if DEBUG
            NativeMethods.FreeConsole();
#endif
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Log.Info($"Trying to find assembly '{args.Name}'");
            if(args.Name == "Interop.IWshRuntimeLibrary")
            {
                var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CheAle14");
                var folders = Directory.GetDirectories(installDir, "app-*");
                var latest = folders.OrderBy(x => x).First();
                var exeLocation = Path.Combine(latest, "Interop.IWshRuntimeLibrary.dll");
                Log.Info($"Attempting to load '{exeLocation}'");
                return Assembly.LoadFrom(exeLocation);
            }
            return null;
        }

        private static void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        static async Task<UpdateManager> getMgr()
        {
            return new UpdateManager("https://masterlist.uk.ms/nea", "MacauGame", ".");
        }

        static async Task Main()
        {
            using (var updater = await getMgr())
            {
                Log.Info(updater.ApplicationName ?? "unknown app name");
                //var updater = await mgr;
#if IN_SCHOOL
                var update = await updater.UpdateAppNoRegistry();
#else
                var update = await updater.UpdateApp();
#endif
                if (update == null)
                {
                    Log.Info("Running latest version");
                } else
                {
                    VERSION = update.Version.Version;
                    Log.Info($"Running version {(update?.Version?.ToString() ?? "error happened")}");
                    createShortCut(VERSION);
                }
            }
        }

        static string[] getShortAttempts() 
        {
            var ls = new List<string>();
            ls.Add(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            ls.Add(@"D:\MacauGame.lnk");
            ls.Add(@"H:\MacauGame.lnk");
            ls.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads"));
            return ls.ToArray();
        }


        static void createShortcutAt(Version v, string location)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                + @"\CheAle14\Update.exe";
            Log.Warn($"Creating shortcut {location} -> {path}");
            var sl = new Squirrel.Shell.ShellLink()
            {
                Target = path,
                WorkingDirectory = Path.GetDirectoryName(path) + $"\\app-{v.Major}.{v.Minor}.{v.Build}",
                Arguments = "--processStart MacauGame.exe",
                Description = "Shortcut to launch NEA Macau Game.",
            };
            sl.Save(location);
        }

        static void createShortCut(Version v)
        {
            foreach(var path in getShortAttempts())
            {
                try
                {
                    createShortcutAt(v, path);
                } catch (Exception e)
                {
                    Log.Warn("Failed to create shortcut: " + e.ToString());
                }
            }
        }
        static void removeShotCut(Version v)
        {
            foreach(var path in getShortAttempts())
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        static void onInitialInstall(Version v) => createShortCut(v);
        static void onAppUpdate(Version v) => createShortCut(v);
        static void onAppUninstall(Version v) => removeShotCut(v);
        static void onFirstRun()
        {

        }

        static void onAppObsoleted(Version v)
        {
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
