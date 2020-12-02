using Newtonsoft.Json.Linq;
using System;

namespace MacauEngine.Models
{
    /// <summary>
    /// Represents a single unit of data sent from and/or to the client and/or server
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Instanstiates a new instance with the specified Id, JSON content and sequence reference
        /// </summary>
        public Packet(PacketId id, JToken value, int sequence)
        {
            if(sequence == 0)
                sequence = 
                    new JTokenEqualityComparer().GetHashCode(value)
                    * id.GetHashCode();
            Id = id;
            Content = value;
            Sequence = sequence;
            Response = null;
        }
        /// <summary>
        /// Instantiates a new instance with the specified Id, JSON value and a sequence of 0
        /// </summary>
        public Packet(PacketId id, JToken value) : this(id, value, 0) { }
        /// <summary>
        /// Instantiates a new instance by loading values from the provided JSON object
        /// </summary>
        public Packet(JObject obj)
        {
            Id = obj["id"].ToObject<PacketId>();
            Content = obj["content"];
            Sequence = obj["seq"].ToObject<int>();
            var maybe = obj["res"];
            if (maybe != null)
                Response = maybe.ToObject<int>();
        }

        /// <summary>
        /// Creates a new response packet to this instance
        /// </summary>
        /// <param name="id">The Id for which the response packet will have</param>
        /// <param name="value">The JSON content of the response packet</param>
        /// <returns>A packet, whose Response value is equal to this instances Sequence</returns>
        public Packet Reply(PacketId id, JToken value)
        {
            return new Packet(id, value, 0)
            {
                Response = this.Sequence
            };
        }

        /// <summary>
        /// Returns this packet as a JSON respresentation
        /// </summary>
        public JObject ToJson()
        {
            var jobj = new JObject();
            jobj["id"] = Id.ToString();
            jobj["content"] = Content;
            jobj["seq"] = Sequence;
            if(Response.HasValue)
                jobj["res"] = Response.Value;
            return jobj;
        }

        /// <summary>
        /// Returns the JSON representation of this packet properly indented
        /// </summary>
        public override string ToString()
        {
            return ToJson().ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Gets or sets the Id of this packet, which indicates its purpose
        /// </summary>
        public PacketId Id { get; set; }
        /// <summary>
        /// The optional payload of this packet, can be null through JValue.CreateNull()
        /// </summary>
        public JToken Content { get; set; }
        /// <summary>
        /// The quasi-unique sequence value to allow packets to response to this one
        /// </summary>
        public int Sequence { get; set; }
        /// <summary>
        /// The optional Sequence value of the packet this instance is responding to
        /// </summary>
        public int? Response { get; set; }
    }

    /// <summary>
    /// Specifies the purpose of the packet
    /// </summary>
    public enum PacketId
    {
        /// <summary>
        /// The packet has no purpose (unused)
        /// </summary>
        None = 0,

        #region Common Codes
        /// <summary>
        /// Hypothetical error packet, indicating client sent an unknown packet
        /// </summary>
        UnknownCode,
        /// <summary>
        /// Response packet.
        /// Indicates the packet's Content should be displayed as an error message
        /// </summary>
        Error,
        /// <summary>
        /// Response packet.
        /// Indicates the operation for the prior packet was sucessfully performed
        /// </summary>
        Success,
        /// <summary>
        /// Indicates the packet's Content should be displayed as a generic message
        /// </summary>
        Message,
        /// <summary>
        /// Indicates the receiver should close the WebSocket connection, with the Content as the reason why
        /// </summary>
        Disconnect,
        #endregion

        #region Client -> Server Codes
        /// <summary>
        /// The client requires intelligence on an unknown player
        /// Content is the ID of the player
        /// See: <see cref="ProvidePlayerInfo"/>
        /// </summary>
        GetPlayerInfo,
        /// <summary>
        /// The client requires information on the state of the current game
        /// Content is null
        /// See: <see cref="ProvideGameInfo"/>
        /// </summary>
        GetGameInfo,

        /// <summary>
        /// The client requests that the specified cards be placed
        /// Content is an array of <see cref="Card"/>
        /// </summary>
        PlaceCards,

        [Obsolete("Unknown", true)]
        SkipOrGiveupTurn,

        /// <summary>
        /// The client requests that they skip their turn, due to threat of Four(s)
        /// </summary>
        IndicateSkipsTurn,
        /// <summary>
        /// The client requests that they pickup a <see cref="Card"/>, or multiple cards if attack cards are placed
        /// </summary>
        IndicatePickupCard,

        /// <summary>
        /// The client requests that a vote to start the game be recorded in their name
        /// </summary>
        VoteStartGame,
        #endregion

        #region Server -> Client Codes
        /// <summary>
        /// The server provides the information of the requested player.
        /// Content: the Player
        /// See: <see cref="GetPlayerInfo"/>
        /// </summary>
        ProvidePlayerInfo,
        /// <summary>
        /// The server provides the information of the game, such as whose turn it is.
        /// Content: the Game.
        /// See: <see cref="GetGameInfo"/>
        /// </summary>
        ProvideGameInfo,
        /// <summary>
        /// The server indicates the specified player has voted to start, so the clients can update the uI
        /// Content: ID of the player who voted
        /// See: <see cref="VoteStartGame"/>
        /// </summary>
        PlayerHasVotedStart,
        /// <summary>
        /// The server indicates that the clients should set all table cards to Active = false.
        /// </summary>
        ClearActive,
        /// <summary>
        /// The server indicates the client has picked up the specified cards to their Hand
        /// Content: array of <see cref="Card"/>s
        /// </summary>
        BulkPickupCards,
        /// <summary>
        /// The server indicates whose turn it currently is
        /// Content: id of player the server is waiting to go
        /// </summary>
        WaitingOn,
        /// <summary>
        /// The server indicates the specified cards have been placed onto the table
        /// Content: array of <see cref="Card"/>s
        /// </summary>
        NewCardsPlaced,
        /// <summary>
        /// The server provides the information of a new player
        /// Content: <see cref="Player"/> object, without Hand info
        /// </summary>
        NewPlayerJoined,
        /// <summary>
        /// The server indicates a <see cref="Player"/> has no more cards remaining, 
        ///   and optionally information about whether the game has ended.
        /// Content: 
        ///     - ID of player
        ///     - Position that player finished in (eg, 1[st] 2[nd] etc)
        ///     - Whether the game has ended
        /// </summary>
        PlayerFinished,
        #endregion
    }
}
