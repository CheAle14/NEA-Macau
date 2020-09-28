using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using Newtonsoft.Json;
using Squirrel;

[assembly: System.Reflection.AssemblyMetadata("SquirrelAwareVersion", "1")]
namespace MacauGame
{
    internal class Program
    {
        public static string AppDataFolderName = "CheAle14-Macau";
        public static string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);
        public static string GAME_TYPE => AppDataFolderName.ToLower();
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
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#if USING_MLAPI
            MLAPI.MasterList.Log = (msg) =>
            {
                Log.Info("MSL", msg);
                return Task.CompletedTask;
            };
#endif
            RND = new Random();
#if !DEBUG
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
        }

        private static void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        static async Task<UpdateManager> getMgr()
        {
            return new UpdateManager("https://masterlist.uk.ms/nea");
        }

        static async Task Main()
        {
            using (var updater = await getMgr())
            {
                Log.Info(updater.ApplicationName ?? "unknown app name");
                SquirrelAwareApp.HandleEvents(onInitialInstall, onAppUpdate, onAppObsoleted, onAppUninstall, onFirstRun);
                //var updater = await mgr;
                var update = await updater.UpdateApp();
                if(update == null)
                {
                    Log.Info("Running latest version");
                } else
                {
                    Log.Info($"Running version {(update?.Version?.ToString() ?? "error happened")}");
                }
            }
        }

        static void createShortCut(Version v)
        {
            if(!GetLocalIPAddress().StartsWith("10."))
            {
                using (var mgr = getMgr().Result)
                    mgr.CreateShortcutForThisExe();
                return;
            }
            WshShell shell = new WshShell();
            string shortcutAddress = @"D:\\MacauGame.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Shortcut to launch Macau game";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                + @"\CheAle14\Update.exe";
            shortcut.TargetPath = path;
            shortcut.WorkingDirectory = Path.GetDirectoryName(path) + $"\\app-{v.Major}.{v.Minor}.{v.Build}";
            shortcut.Arguments = "--processStart MacauGame.exe";
            shortcut.Save();
        }
        static void removeShotCut(Version v)
        {
            if (!GetLocalIPAddress().StartsWith("10."))
            {
                using (var mgr = getMgr().Result)
                    mgr.RemoveShortcutForThisExe();
                return;
            }
            if (System.IO.File.Exists("D:\\MacauGame.lnk"))
                System.IO.File.Delete("D:\\MacauGame.lnk");

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
}
