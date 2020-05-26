using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MacauGame
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Log.Info($"Started");
            var srv = new Server.MacauServer();
            while(true) { }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error($"UnhandledEx", (Exception)e.ExceptionObject);
            if (e.IsTerminating)
                Log.Error("UnhandledEx", "We must now exit");
        }
    }
}
