using MacauEngine.Models;
using MacauEngine.Validators;
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

        public bool VotedToStart = false;

        public bool ForceUserAnyway = false;

        public bool IsCurrentPlayer => Server.CurrentWaitingOn != null && Server.CurrentWaitingOn.Id == Id;

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

        public static bool lockGlobal(Action someAction)
        {
            MacauGame.Log.Info("Attempting to get global lock.");
            GLOBAL.WaitOne();
            MacauGame.Log.Info("Achieved global lock");
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
                MacauGame.Log.Info("Releaseing global lock");
                GLOBAL.Release();
            }
        }

        public void Send(Packet packet)
        {
            Log.Trace("Send: " + packet.ToString());
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

        private bool ErrorIfNotCurrent(Packet ping)
        {
            if (IsCurrentPlayer)
                return false;
            Send(ping.Reply(PacketId.Error,
                JValue.FromObject($"It is not your turn, expecting: {Server.CurrentWaitingOn.Name}")));
            return true;
        }

        public void SendWaitingOn()
        {
            var jobj = new JObject();
            jobj["miss"] = Player.MissingGoes;
            var packet = new Packet(PacketId.WaitingOnYou, jobj);
            Send(packet.ToString());
        }

        void brodcastWaitingOn()
        {
            var pck = new Packet(PacketId.WaitingOn, JValue.FromObject(Server.CurrentWaitingOn.Id));
            Sessions.Broadcast(pck.ToString());

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
            } else if (packet.Id == PacketId.PlaceCards)
            {
                if (ErrorIfNotCurrent(packet))
                    return;
                var jsonArray = packet.Content.ToArray();
                var requested = jsonArray.Select(x => new Card((JObject)x));
                
                if(Player.MissingGoes > 0)
                {
                    if(Player.MultiTurnSkip)
                    {
                        Send(packet.Reply(PacketId.Error, JValue.FromObject("You are missing multiple turns: you must skip")));
                        return;
                    }
                }
                var validator = new PlaceValidator(requested, Server.Table.ShowingCards.Last());
                var result = validator.Validate();
                if(!result.IsSuccess)
                {
                    Send(packet.Reply(PacketId.Error, JValue.FromObject(result.ErrorReason)));
                    return;
                }
                // Now we've validated the move, we need to actually update the player's hand.
                // So we'll remove any cards that appear to be the same as the ones we're trying to place.

                lockGlobal(() =>
                { // Code within this function has exclusive control.

                    // However first, we must check that they actually have all the cards:
                    var found = new List<Card>();
                    foreach (var placing in requested)
                    {
                        var same = Player.Hand.FirstOrDefault(x => x.Equals(placing)); // Note: this is why AceSuit must be ignored
                        if (same == null)
                        {
                            Send(packet.Reply(PacketId.Error, JValue.FromObject($"You do not have the {placing}")));
                            return;
                        }
                        found.Add(placing);
                    }
                    foreach (var x in found) Player.Hand.Remove(x);
                    bool isActive = Server.Table.ShowingCards.Last().IsActive;
                    foreach (var card in found)
                    {
                        if (card.IsPickupCard || card.Value == MacauEngine.Models.Enums.Number.Four)
                            card.IsActive = isActive = true; // allows us to save value too.
                        if (isActive && card.Value == MacauEngine.Models.Enums.Number.King)
                            card.IsActive = isActive = true;
                        if (card.Value == MacauEngine.Models.Enums.Number.Seven)
                        {
                            card.IsActive = isActive = false;
                            Server.Table.ShowingCards.ForEach(y => y.IsActive = false);
                        }
                        Server.Table.PlaceCard(card);
                    }
                    Send(packet.Reply(PacketId.Success, JValue.CreateNull()));
                    var jarray = new JArray();
                    foreach (var x in found)
                        jarray.Add(x.ToJson());
                    var bulk = new Packet(PacketId.NewCardsPlaced, jarray);
                    foreach (var player in Server.OrderedPlayers)
                    {
                        player.Send(bulk);
                    }
                    if (Player.Hand.Count == 0)
                    {
                        //Server.OrderedPlayers.Remove(this);
                        Log.Info($"{Name} has finished");
                    }
                    Server.MoveNextPlayer();
                    brodcastWaitingOn();
                    //Server.CurrentWaitingOn.SendWaitingOn();
                });
            } else if (packet.Id == PacketId.VoteStartGame)
            {
                if(Server.GameStarted)
                {
                    Send(packet.Reply(PacketId.Error, JValue.FromObject("Game has already been started")));
                    return;
                }
                lockGlobal(() =>
                {
                    VotedToStart = true;
                    int count = Server.Players.Values.Count(x => x.VotedToStart);
                    Log.Info($"{Name} has voted to start: {count}/{Server.Players.Count}");
                    if(count >= Server.Players.Count)
                    {
                        Server.StartGame();
                    }
                });
            } else if (packet.Id == PacketId.IndicateSkipsTurn)
            {
                if (ErrorIfNotCurrent(packet))
                    return;
                var activeFours = Server.Table.ShowingCards.Count(x => x.IsActive && x.Value == MacauEngine.Models.Enums.Number.Four);
                if(activeFours > 0)
                {
                    Player.MissingGoes += activeFours;
                    Server.Table.ShowingCards.ForEach(x => x.IsActive = false);
                } else if(Player.MissingGoes <= 0)
                {
                    Send(packet.Reply(PacketId.Error, JValue.FromObject("You are not missing any turns to skip - perhaps you meant to pickup?")));
                    return;
                }
                Player.MissingGoes--;
                Player.MultiTurnSkip = Player.MissingGoes > 0;
                Send(packet.Reply(PacketId.Success, JValue.CreateNull()));
                string msg = $"{Name} is skipped, ";
                if (Player.MissingGoes == 0)
                    msg += "they may go next turn";
                else
                    msg += $"they have {Player.MissingGoes} more goes to miss";
                var pong = new Packet(PacketId.Message, JValue.FromObject(msg));
                foreach (var player in Server.OrderedPlayers)
                {
                    player.Send(pong.ToString());
                    player.Send(new Packet(PacketId.ClearActive, JValue.CreateNull()));
                }
                Server.MoveNextPlayer();
                brodcastWaitingOn();
                //Server.CurrentWaitingOn.SendWaitingOn();
            } else if(packet.Id == PacketId.IndicatePickupCard)
            {
                if (ErrorIfNotCurrent(packet))
                    return;
                if(Player.MissingGoes > 0)
                {
                    Send(packet.Reply(PacketId.Error, JValue.FromObject("You are missing turns - perhaps you meant to skip?")));
                    return;
                }
                var activeFours = Server.Table.ShowingCards.Count(x => x.IsActive && x.Value == MacauEngine.Models.Enums.Number.Four);
                if(activeFours > 0)
                {
                    Send(packet.Reply(PacketId.Error, JValue.FromObject("You cannot pickup cards when threatened by a Four")));
                    return;
                }
                var topCard = Server.Table.ShowingCards.Last();
                if(topCard.Value == MacauEngine.Models.Enums.Number.King && topCard.IsActive)
                {

                }
                int pickups = 0;
                foreach(var card in Server.Table.ShowingCards)
                {
                    if(card.IsActive)
                    {
                        card.IsActive = false;
                        pickups += card.PickupValue;
                    }
                }
                if(pickups == 0)
                { // they picking up because they have nothing they (want) to place
                    pickups = 1;
                }
                var pickedUp = new JArray();
                while (pickups > 0)
                {
                    var card = Server.Table.DrawCard();
                    if (card != null) // may be null if deck is empty 
                    {
                        Player.Hand.Add(card);
                        pickedUp.Add(card.ToJson());
                    }
                    pickups--;
                }
                Send(packet.Reply(PacketId.BulkPickupCards, pickedUp));
                foreach(var player in Server.OrderedPlayers)
                {
                    player.Send(new Packet(PacketId.ClearActive, JValue.CreateNull()));
                }
                Server.MoveNextPlayer();
                brodcastWaitingOn();
                //Server.CurrentWaitingOn.SendWaitingOn();
            } else if (packet.Id == PacketId.GetGameInfo)
            {
                var jArray = new JArray();
                foreach (var player in Server.OrderedPlayers)
                    jArray.Add(player.Player.ToJson());
                var jobj = new JObject();
                jobj["players"] = jArray;
                Send(new Packet(PacketId.ProvideGameInfo, jobj));
            }
            else
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
            try
            {
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                Context.WebSocket.Close(CloseStatusCode.Abnormal, ex.Message);
            }
        }

        protected override void OnOpen()
        {
            Log.Debug($"Opened new connection");
            lockGlobal(() =>
            {
                Log.Debug("Entered global lock; handling new connection...");
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
                this.Player.Order = Server.Players.Count;
                Server.OrderedPlayers = Server.Players.Values.OrderBy(x => x.Player.Order).ToList();
                Log.Info($"New Player has joined: {Name}, {Id}");
                var newPlayer = new Packet(PacketId.NewPlayerJoined, this.Player.ToJson());
                foreach(var p in Server.Players.Values)
                {
                    if(p.Player.Id == hwid)
                    {

                    } else
                    {
                        p.Send(newPlayer);
                    }
                }
            });
        }
    }
}
