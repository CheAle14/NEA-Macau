using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Squirrel;

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
            File.WriteAllText(path, json);
        }

        public static void LoadConfig()
        {
            Configuration = new Options();
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
            var path = Path.Combine(AppDataPath, "config.json");
            try
            {
                var json = File.ReadAllText(path);
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
            MLAPI.MasterList.Log = (msg) =>
            {
                Log.Info("MSL", msg);
                return Task.CompletedTask;
            };
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
        }

        private static void Menu_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        static async Task Main()
        {
            using (var updater = await UpdateManager.GitHubUpdateManager("https://github.com/CheAle14/NEA-Macau"))
            {
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

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error($"UnhandledEx", (Exception)e.ExceptionObject);
            if (e.IsTerminating)
                Log.Error("UnhandledEx", "We must now exit");
            MessageBox.Show(((Exception)e.ExceptionObject).ToString());
        }
    }
}
