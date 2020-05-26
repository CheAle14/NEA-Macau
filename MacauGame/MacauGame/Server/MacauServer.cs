using MacauEngine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace MacauGame.Server
{
    public class MacauServer
    {
        public Dictionary<int, Player> Players { get; set; }
        public WebSocketServer WsServer { get; set; }
        const int PORT = 26007;
        public MacauServer()
        {
            Players = new Dictionary<int, Player>();
            WsServer = new WebSocketServer(PORT);
            WsServer.Log.Level = WebSocketSharp.LogLevel.Trace;
            WsServer.Log.Output = (data, filePath) =>
            {
                var msg = new Log.LogMessage(Log.FormatStackFrame(data.Caller), (Log.LogSeverity)((int)data.Level), data.Message, null);
                Log.LogMsg(msg);
            };
            WsServer.AddWebSocketService<ClientBehaviour>("/", () =>
            {
                return new ClientBehaviour(this);
            });
            WsServer.Start();
        }
    }
}
