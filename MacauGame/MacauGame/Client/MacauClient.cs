using MacauEngine.Models;
#if USING_MLAPI
using MLAPI.Classes.Client;
#endif
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;

namespace MacauGame.Client
{
    public partial class MacauClient : Form
    {
        public string SELF_HWID { get; private set; }
        public MacauClient()
        {
            InitializeComponent();
        }

        private Dictionary<int, AwaitedPacket> waitingForResponse = new Dictionary<int, AwaitedPacket>();

        WebSocket WS_Client { get; set; }
        public GameClient Game { get; set; }

        private void MacauClient_Load(object sender, EventArgs e)
        {
            SELF_HWID = UHWID.UHWIDEngine.AdvancedUid;
            this.Text = "Fetching servers...";
            txtName.Text = Program.Configuration.Name;
            txtIP.Text = Program.Configuration.IP;
            new Thread(updateML).Start();
        }

        void updateML()
        {
#if USING_MLAPI
            List<ServerInfo> servers;
            try
            {
                servers = MLAPI.MasterList.GetServers(Program.GAME_TYPE, false).Result;
                this.Invoke(new Action(() =>
                {
                    foundServers(servers);
                }));
            }
            catch (Exception ex)
            {
                Log.Error("GetML", ex);
                this.Invoke(new Action(() =>
                {
                    this.Name = "Could not fetch masterlist";
                }));
            }
#else
            this.Invoke(new Action(() =>
            {
                this.Name = "Masterlist is disabled.";
            }));
#endif
        }

#if USING_MLAPI
        void foundServers(List<MLAPI.Classes.Client.ServerInfo> servers)
        {
            foreach(var srv in servers)
            {
                var row = new object[] { srv.Name, $"{srv.Players.Count}", srv.ExternalIP, srv.InternalIP };
                dgvMasterlist.Rows.Add(row);
            }
            this.Text = $"Found {servers.Count} online servers";
        }
#endif
        private void txtIP_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtIP.ReadOnly = !txtIP.ReadOnly;
        }

        private void dgvMasterlist_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return; // clicked somewhere on the table, but not in a cell
            var row = dgvMasterlist.Rows[e.RowIndex];
            var cell = row.Cells[e.ColumnIndex];
            if(cell is DataGridViewButtonCell btn)
            {
                // btn.Value is the IP address they clicked on
                txtIP.Tag = btn.Value;
                txtIP.Text = btn.Value.ToString();
                btnConnect.PerformClick();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if(WS_Client != null)
            {
                MessageBox.Show("You are already trying to connect to a server");
                return;
            }
            if(string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("You must enter a name");
                return;
            }
            if(txtName.Text.Length < 3 || txtName.Text.Length > 16)
            {
                MessageBox.Show("Name must be between 3 and 16 characters long");
                return;
            }
            if(string.IsNullOrWhiteSpace(txtIP.Text))
            {
                MessageBox.Show("You must select an IP from the masterlist, or double-click for manual entry");
                return;
            }
            if(!(IPAddress.TryParse(txtIP.Text, out var ipad))) 
            {
                MessageBox.Show("Could not parse given IP as an actual IP address");
                return;
            }
#if DEBUG
            SELF_HWID = UHWID.UHWIDEngine.AdvancedUid + txtName.Text;
#endif
            waitingForResponse = new Dictionary<int, AwaitedPacket>();
            Program.Configuration.Name = txtName.Text;
            Program.Configuration.IP = txtIP.Text;
            string ipStr;
            if(ipad.AddressFamily == AddressFamily.InterNetwork)
            { // IPv4 address, can be used directly
                ipStr = ipad.ToString();
            } else
            { // assume IPv6 address, must be wrapped in square brackets.
                ipStr = "[" + ipad.ToString() + "]";
            }
            WS_Client = new WebSocket($"ws://{ipStr}:{Server.MacauServer.PORT}/" +
                $"?name={Uri.EscapeDataString(txtName.Text)}&hwid={SELF_HWID}");
            Log.Info($"Connecting: {WS_Client.Url}");
            WS_Client.OnOpen += WS_Client_OnOpen;
            WS_Client.OnMessage += WS_Client_OnMessage;
            WS_Client.OnError += WS_Client_OnError;
            WS_Client.OnClose += WS_Client_OnClose;
            new Thread(wsOpen).Start();
        }

        void wsOpen()
        {
            try
            {
                WS_Client.Connect();
            }
            catch (Exception ex)
            {
                Log.Error("WSClient", ex);
                MessageBox.Show($"Could not connect to {WS_Client.Url.Host}: {ex.Message}", "No Connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WS_Client = null;
            }
        }

        private void WS_Client_OnClose(object sender, CloseEventArgs e)
        {
            Log.Info($"Closed: {e.Code} {e.Reason}");
            MessageBox.Show($"Connection with server lost or closed - {e.Code} {e.Reason}");
            if (Game != null && this.InvokeRequired)
                this.Invoke(new Action(() => Game?.Close()));
            Game = null;
            WS_Client = null;
        }

        private void WS_Client_OnError(object sender, ErrorEventArgs e)
        {
            Log.Info($"Error: {e.Message}\r\n   {e.Exception}");
        }

        private void WS_Client_OnOpen(object sender, EventArgs e)
        {
            Log.Info("Opened WS");
            var act = new Action(() =>
            {
                Game = new GameClient(this);
                Game.FormClosing += Game_FormClosing;
                Game.Visible = true;
                Game.Show();
                MacauGame.Menu.Instance.Hide();
                this.Hide();
                Send(new Packet(PacketId.GetGameInfo, JValue.CreateNull()));
            });
            if (this.InvokeRequired)
                this.Invoke(act);
            else
                act();
        }

        private void Game_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Send(new Packet(PacketId.Error, JValue.FromObject($"Game form closed {e.CloseReason}")));
            } catch (Exception ex)
            {
                Log.Error("GameClosing", ex);
            }
            try
            {
                WS_Client.Close(CloseStatusCode.Abnormal);
            } catch (Exception ex)
            {
                Log.Error("GameClosing", ex);
            }
            this.Show();
        }

        private void MacauClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                WS_Client.Send(new Packet(PacketId.Disconnect, JValue.FromObject(e.CloseReason.ToString())).ToString());
            } catch
            {
            }
            try
            {
                WS_Client.Close(CloseStatusCode.Abnormal, e.CloseReason.ToString());
            } catch { }
        }

        private void MacauClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            MacauGame.Menu.Instance.Client = null;
            MacauGame.Menu.Instance.Show();
        }

        public void Send(Packet p) => WS_Client.Send(p.ToString());
        public Packet GetResponse(Packet packet, int timeout = 30000)
        {
            var awaiter = new AwaitedPacket()
            {
                Sent = packet,
                Holder = new ManualResetEventSlim()
            };
            waitingForResponse[packet.Sequence] = awaiter;
            Send(packet);
            awaiter.Holder.Wait(timeout);
            return awaiter.Recieved;
        }
        public void GetResponse(Packet packet, Action<Packet> callback, int timeout = 30000)
        {
            var awaiter = new AsyncAwaitedPacket()
            {
                Sent = packet,
                Holder = new ManualResetEventSlim(),
                Callback = callback,
                Timeout = timeout
            };
            waitingForResponse[packet.Sequence] = awaiter;
            var th = new Thread(responseCallbackThread);
            th.Start(awaiter);
            Send(packet);
        }

        private void responseCallbackThread(object o)
        {
            if(o is AsyncAwaitedPacket awaiter)
            {
                awaiter.Holder.Wait(awaiter.Timeout + 250); // account for thread starting
                awaiter.Callback.Invoke(awaiter.Recieved);
            }
        }

        Player GetPlayer(string id)
        {
            var packet = new Packet(PacketId.GetPlayerInfo, JValue.FromObject(id));
            var response = GetResponse(packet);
            if (response == null)
                return new Player(id, "[Could not fetch info]");
            return new Player((JObject)response.Content);
        }

        void handlePacket(Packet packet)
        {
            if(Game == null)
            {
                Log.Error("HandlePacket", "Cannot handle packet since Game form is null.");
            } else
            {
                Game.Invoke(new Action(() =>
                {
                    Game.HandleGamePacket(packet);
                }));
            }
        }

        Semaphore LOCK = new Semaphore(1, 1);
        void packetThread(object o)
        {
            if (!(o is Packet packet))
                return;
            if (packet == null)
                return;
            LOCK.WaitOne();
            try
            {
                handlePacket(packet);
            } finally
            {
                LOCK.Release();
            }
        }

        private void WS_Client_OnMessage(object sender, MessageEventArgs e)
        {
            Log.Trace(e.Data);
            try
            {
                var json = Newtonsoft.Json.Linq.JObject.Parse(e.Data);
                var packet = new Packet(json);
                Log.Debug($"Packet {packet.Sequence} responding to {packet.Response}; id {packet.Id}");
                if (packet.Response.HasValue)
                {
                    if (waitingForResponse.TryGetValue(packet.Response.Value, out var waiting))
                    {
                        waiting.Recieved = packet;
                        waiting.Holder.Set();
                        waitingForResponse.Remove(packet.Response.Value);
                    }
                    else
                    {
                        Log.Warn($"Packet is attempting to release an unknown awaiter");
                    }
                }
                else
                {
                    var th = new Thread(packetThread);
                    th.Start(packet);
                }
            } catch (Exception ex)
            {
                Log.Error("ClientRec", ex);
                WS_Client.Close(CloseStatusCode.Abnormal, ex.Message);
            }
        }
    }
}
