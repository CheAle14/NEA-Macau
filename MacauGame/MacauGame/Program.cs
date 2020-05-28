using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Squirrel;

namespace MacauGame
{
    internal class Program
    {
        public static string GAME_TYPE = "cheale14Macau";
        public static Random RND { get; private set; }
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
            Application.Run(new Menu());
        }

        static async Task Main()
        {
            using (var updater = new UpdateManager(@"D:\_GitHub\NEA-Macau\MacauGame\Releases"))
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
        }
    }
}
