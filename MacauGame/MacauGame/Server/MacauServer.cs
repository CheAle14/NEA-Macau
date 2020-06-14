using MacauEngine.Models;
using MLAPI;
using MLAPI.Classes.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp.Server;

namespace MacauGame.Server
{
    public partial class MacauServer : Form
    {
        public string Server_HWID => UHWID.UHWIDEngine.AdvancedUid;
        string logId;
        public MacauServer()
        {
            InitializeComponent();
            logId = Log.Register(msg =>
            {
                if (this == null || this.IsDisposed || this.Disposing)
                    return;
                this.Invoke(new Action(() =>
                {
                    rtbConsole.AppendText(msg.ToString() + "\r\n", GetColor(msg.Severity));
                }));
            }, "S");
        }

        System.Drawing.Color GetColor(Log.LogSeverity sev)
        {
            switch(sev)
            {
                case Log.LogSeverity.Trace:
                    return Color.DarkGray;
                case Log.LogSeverity.Debug:
                    return Color.Gray;
                case Log.LogSeverity.Info:
                    return Color.White;
                case Log.LogSeverity.Warning:
                    return Color.Yellow;
                case Log.LogSeverity.Error:
                    return Color.Red;
                case Log.LogSeverity.Fatal:
                    return Color.DarkRed;
                default:
                    return Color.Magenta;
            }
        }

        public Dictionary<string, ClientBehaviour> Players { get; set; }

        public List<ClientBehaviour> OrderedPlayers { get; set; }
        public ClientBehaviour PreviousWaitingOn { get; set; }
        public ClientBehaviour CurrentWaitingOn { get; set; }

        public Table Table { get; set; }
        public WebSocketServer WsServer { get; set; }
        public const int PORT = 26007;
        public bool GameStarted { get; set; }

        public void StartGame()
        {
            if (GameStarted)
                return;
            GameStarted = true;
            Table = new Table();
            var topCard = Table.DrawCard();
            if (topCard.Value == MacauEngine.Models.Enums.Number.Ace)
                topCard.AceSuit = topCard.House;
            Table.ShowingCards.Add(topCard);
            OrderedPlayers = Players.Select(x => x.Value).OrderBy(x => x.Player.Order).ToList();
            Log.Info($"Starting game with {OrderedPlayers.Count}; table: {Table.ShowingCards[0]}");
            CurrentWaitingOn = OrderedPlayers[0];
            var placedPacket = new Packet(PacketId.NewCardsPlaced, new JArray() { Table.ShowingCards[0].ToJson() });

            // We need to ensure all players know one another.
            // And also know one another's order
            int orderCount = 0;
            var orderArray = new JArray();
            foreach(var player in OrderedPlayers)
            {
                player.Player.Order = orderCount++;
                orderArray.Add(player.Player.ToJson());
            }
            var orderObject = new JObject();
            orderObject["players"] = orderArray;
            var orderPacket = new Packet(PacketId.ProvideGameInfo, orderObject);
            foreach (var player in OrderedPlayers)
            {
                player.Player.Order = orderCount++;
                player.Player.Hand = new List<Card>();
                var jarray = new JArray();
                for (int i = 0; i < 5; i++)
                {
                    player.Player.Hand.Add(Table.DrawCard());
                    jarray.Add(player.Player.Hand[i].ToJson());
                }
                player.Send(orderPacket);
                Thread.Sleep(500); // probably not needed, but we'll throw it in just in case.
                var packet = new Packet(PacketId.BulkPickupCards, jarray);
                player.Send(packet);
                Thread.Sleep(500); 
                player.Send(placedPacket);
                Thread.Sleep(500);
            }
            CurrentWaitingOn.Send(new Packet(PacketId.WaitingOnYou, JValue.CreateNull()));
            Log.Info($"Finished starting game, waiting on action from {CurrentWaitingOn.Name}");
        }

        ClientBehaviour getNextMatch(ClientBehaviour current, Func<Player, bool> predicate, int direction = 1)
        {
            int index = OrderedPlayers.IndexOf(current);
            int next = index + direction;
            do
            {
                if (next < 0)
                    next = OrderedPlayers.Count - 1;
                else if (next >= OrderedPlayers.Count)
                    next = 0;

                var possible = OrderedPlayers[next];
                if (predicate(possible.Player))
                    return possible;
                next += direction;

            } while (true);
        }

        public void MoveNextPlayer()
        {
            var top = Table.ShowingCards.Last();
            if (top.Value == MacauEngine.Models.Enums.Number.King)
            { // difficulty with Kings is that 
                // they can be turned back to a person who is no longer playing.
                int indexOfLastPlacer = OrderedPlayers.IndexOf(PreviousWaitingOn);
                int indexOfCurrent = OrderedPlayers.IndexOf(CurrentWaitingOn);
                int difference = indexOfCurrent - indexOfLastPlacer;
                // eg, if LastPlacer as at index 0, and current at 1.
                //      then the diff would be 1, which is positive.
                //      hence we are ahead of them, so need to flip and look backwards.
                // else, if LastPlacer is at index 1, and current is at 0
                //      then the diff is -1, negative
                //      hence we are behind, so need to look forwards
                // the direction we look is the inverse sign of whatever the difference is (but only 1)
                int direction = difference > 0 ? -1 : 1;
                var nextResponder = getNextMatch(CurrentWaitingOn, x => !x.Finished, direction);
                PreviousWaitingOn = CurrentWaitingOn;
                CurrentWaitingOn = nextResponder;
            }
            else
            {
                PreviousWaitingOn = CurrentWaitingOn;
                CurrentWaitingOn = getNextMatch(CurrentWaitingOn, x => !x.Finished);
            }
        }

        private void MacauServer_Load(object sender, EventArgs e)
        {
            this.Text = Server_HWID;
            Players = new Dictionary<string, ClientBehaviour>();
            WsServer = new WebSocketServer(IPAddress.Any, PORT);
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
            Log.Info("Server started");
            new Thread(masterlist).Start();
        }

        void masterlist()
        {
            Log.Info("Reaching out to ML...");
            try
            {
                var rest = MasterList.GetOrCreate(x =>
                {
                    x.Game = Program.GAME_TYPE;
                    x.InternalIP = IPAddress.Parse("192.168.1.2");
                    x.Name = "Test Server 4";
                    x.Port = PORT;
                    x.IsPortForward = true;
                }).Result;
                rest.PingOnline().GetAwaiter().GetResult();
                Log.Info("Server on masterlist");
            }
            catch (Exception ex)
            {
                Log.Error("MasterlistStart", ex);
                Log.Info("Server not hosted on ML.");
            }
        }

        private void MacauServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.UnRegister(logId);
            try
            {
                WsServer.Stop(WebSocketSharp.CloseStatusCode.Abnormal, "Form Closed");
            } catch
            {
            }
        }

        private void MacauServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            MacauGame.Menu.Instance.Server = null;
            MacauGame.Menu.Instance.Show();
        }
    }
}
