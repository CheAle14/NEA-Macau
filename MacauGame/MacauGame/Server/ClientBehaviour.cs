using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MacauGame.Server
{
    public class ClientBehaviour : WebSocketBehavior
    {
        public ClientBehaviour(MacauServer s)
        {
            Server = s;
        }
        public MacauServer Server { get; set; }
        protected override void OnClose(CloseEventArgs e)
        {
            Log.Error($"Closed: {e.Code} {e.Reason}");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Log.Error($"{e.Message}\r\nException: {e.Exception}");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Log.Debug(e.Data);
        }

        protected override void OnOpen()
        {
            Log.Debug($"Opened new connection");

        }
    }
}
