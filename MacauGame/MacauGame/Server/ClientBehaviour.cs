using MacauEngine.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
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
        public Player Player { get; private set; }
        public string Id { get { return Player.Id; } set { Player.Id = value; } }
        public string Name { get { return Player.Name; } set { Player.Name = value; } }

        /// <summary>
        /// Semaphore to lock important reads/writes; ensures only one client access at a time
        /// </summary>
        static Semaphore GLOBAL = new Semaphore(1, 1);
        /// <summary>
        /// Semaphore that ensures only one message from each client is performed a time.
        /// </summary>
        Semaphore LOCAL = new Semaphore(1, 1);

        bool lockLocal(Action someAction)
        {
            LOCAL.WaitOne();
            try
            {
                someAction();
                return true;
            } catch (Exception ex)
            {
                var thing = MacauGame.Log.FindLocation(1);
                Log.Warn($"Failed to complete from caller {thing}; {ex}");
                return false;
            } finally
            {
                LOCAL.Release();
            }
        }

        static bool lockGlobal(Action someAction)
        {
            GLOBAL.WaitOne();
            try
            {
                someAction();
                return true;
            }
            catch (Exception ex)
            {
                var thing = MacauGame.Log.FindLocation(1);
                MacauGame.Log.Warn(thing, ex.ToString());
                return false;
            }
            finally
            {
                GLOBAL.Release();
            }
        }

        public void Send(Packet packet)
        {
            Send(packet.ToString());
        }

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
       
        private Dictionary<int, AwaitedPacket> waitingForResponse = new Dictionary<int, AwaitedPacket>();
        public MacauServer Server { get; set; }
        protected override void OnClose(CloseEventArgs e)
        {
            Log.Error($"Closed: {e.Code} {e.Reason}");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Log.Error($"{e.Message}\r\nException: {e.Exception}");
        }

        void handlePacket(Packet packet)
        {
            if(packet.Id == PacketId.GetPlayerInfo)
            {
                JToken obj;
                var id = packet.Content.ToObject<string>();
                if(Server.Players.TryGetValue(id, out var client))
                {
                    var player = client.Player;
                    obj = player == null ? (JToken)JValue.CreateNull() : (JToken)JObject.FromObject(player);
                } else
                {
                    obj = JValue.CreateNull();
                }
                Send(packet.Reply(PacketId.ProvidePlayerInfo, obj));
            } else
            {
                Send(packet.Reply(PacketId.UnknownCode, JToken.FromObject($"Not able to handle code {packet.Id}")));
            }
        }

        void packetThread(object o)
        {
            if(o is Packet packet)
            {
                lockLocal(() => { handlePacket(packet); });
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Log.Trace(e.Data);
            var json = Newtonsoft.Json.Linq.JObject.Parse(e.Data);
            var packet = new Packet(json);
            Log.Debug($"Packet {packet.Sequence} responding to {packet.Response}; id {packet.Id}");
            if(packet.Response.HasValue)
            {
                if(waitingForResponse.TryGetValue(packet.Response.Value, out var waiting))
                {
                    waiting.Recieved = packet;
                    waiting.Holder.Set();
                    waitingForResponse.Remove(packet.Response.Value);
                } else
                {
                    Log.Warn($"Packet is attempting to release an unknown awaiter");
                }
            } else
            {
                var th = new Thread(packetThread);
                th.Start(packet);
            }
        }

        protected override void OnOpen()
        {
            Log.Debug($"Opened new connection");
            lockGlobal(() =>
            {
                var hwid = Context.QueryString["hwid"];
                var name = Context.QueryString["name"];
                if(hwid == null || name == null)
                {
                    Context.WebSocket.Close(CloseStatusCode.Abnormal, "Bad Request -- No hwid or name query paramaters");
                    return;
                }
                this.Player = new Player(hwid, name);
                if (Server.Players.ContainsKey(hwid))
                {
                    Context.WebSocket.Close(CloseStatusCode.Abnormal, "Conflict -- player already exists");
                    return;
                }
                Server.Players[hwid] = this;
                Log.Info($"New Player has joined: {Name}, {Id}");
            });
        }
    }
}
