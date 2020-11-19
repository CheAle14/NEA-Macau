using MacauEngine.Models;
using MacauEngine.Models.Enums;
#if USING_MLAPI
using MLAPI;
using MLAPI.Classes.Server;
#endif
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                var act = new Action(() =>
                {
                    rtbConsole.AppendText(msg.ToString() + "\r\n", GetColor(msg.Severity));
                });
                if (this.InvokeRequired)
                    this.Invoke(act);
                else
                    act();
            }, "S");
        }

        System.Drawing.Color GetColor(Log.LogSeverity sev)
        {
            switch (sev)
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

#if USING_MLAPI
        public RestServer MLServer { get; private set; }
#endif

        public Table Table { get; set; }
        public WebSocketServer WsServer { get; set; }
        public const int PORT = 26007;
        public bool GameStarted { get; set; }

        public void StartGame()
        {
            // Function is already called in lockGlobal context.
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

            // We need to ensure all players know one another.
            // And also know one another's order
            int orderCount = 0;
            var orderArray = new JArray();
            foreach (var player in OrderedPlayers)
            {
                player.Player.Order = orderCount++;
                orderArray.Add(player.Player.ToJson());
            }
            foreach (var player in OrderedPlayers)
            {
                player.Player.Order = orderCount++;
                player.Player.Hand = new List<Card>();
                for (int i = 0; i < 5; i++)
                {
                    player.Player.Hand.Add(Table.DrawCard());
                }
                player.SendGameInfo(false);
                Thread.Sleep(500);
            }
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

        bool nextPlayerPredicate(Player player)
        {
            return player.Finished == false;
        }

        void MoveNextWithMisses(int direction)
        {
            do
            {
                PreviousWaitingOn = CurrentWaitingOn;
                CurrentWaitingOn = getNextMatch(CurrentWaitingOn, x => !x.Finished, direction);
                if (CurrentWaitingOn == null)
                    break;
                if(CurrentWaitingOn.Player.MissingGoes > 0)
                {
                    CurrentWaitingOn.Player.MissingGoes--;
                    var msg = new Packet(PacketId.Message, JValue.FromObject($"{CurrentWaitingOn.Player.Name} misses their turn; remaining: {CurrentWaitingOn.Player.MissingGoes}"));
                    foreach (var player in Players)
                        player.Value.Send(msg);
                    // TODO: miss turns must be >0 in order for loop to occur again
                }
            } while (CurrentWaitingOn.Player.MissingGoes > 0);
        }

        public void MoveNextPlayer()
        {
            // From writeup - direction of next player is determined by whether the number of active kings is even or odd.
            // If odd, then -1
            // If even, then +1]
            var top = Table.ShowingCards.Last();
            if (top.Value == MacauEngine.Models.Enums.Number.King)
            {
                var howManyKings = Table.ShowingCards.Count(x => x.Value == Number.King && x.IsActive);
                var direction = howManyKings % 2 == 0 ? 1 : -1;

                MoveNextWithMisses(direction);
            }
            else
            {
                MoveNextWithMisses(1);
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

        static string getIp()
        {
            using (var hc = new HttpClient())
            {
                var resp = hc.GetAsync("https://icanhazip.com").Result;
                var text = resp.Content.ReadAsStringAsync().Result;
                return text.Trim();
            }
        }

        void masterlist()
        {
#if USING_MLAPI
            Log.Info("Reaching out to ML...");
            try
            {
                MLServer = MasterList.GetOrCreate(x =>
                {
                    x.Game = Program.GAME_TYPE;
                    x.InternalIP = IPAddress.Parse(Program.GetLocalIPAddress());
                    x.ExternalIP = IPAddress.Parse(getIp());
                    x.Name = "This one - " + Environment.UserName;
                    x.Port = PORT;
                    x.IsPortForward = true;
                }).Result;
                MLServer.PingOnline().GetAwaiter().GetResult();
                Log.Info("Server on masterlist");
            }
            catch (Exception ex)
            {
                Log.Error("MasterlistStart", ex);
                Log.Info("Server not hosted on ML.");
                Log.Warn("Masterlist is disabled; IP of server is: " + Program.GetLocalIPAddress());
            }
#else
            Log.Warn("Masterlist is disabled; IP of server is: " + Program.GetLocalIPAddress());
#endif
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
