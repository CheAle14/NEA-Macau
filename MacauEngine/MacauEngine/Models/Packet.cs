using Newtonsoft.Json.Linq;

namespace MacauEngine.Models
{
    public class Packet
    {
        public Packet(PacketId id, JToken value, int sequence)
        {
            if(sequence == 0)
                sequence = new JTokenEqualityComparer().GetHashCode(value);
            Id = id;
            Content = value;
            Sequence = sequence;
            Response = null;
        }
        public Packet(PacketId id, JToken value) : this(id, value, 0) { }
        public Packet(JObject obj)
        {
            Id = obj["id"].ToObject<PacketId>();
            Content = obj["content"];
            Sequence = obj["seq"].ToObject<int>();
            var maybe = obj["res"];
            if (maybe != null)
                Response = maybe.ToObject<int>();
        }

        public Packet Reply(PacketId id, JToken value)
        {
            return new Packet(id, value, 0)
            {
                Response = this.Sequence
            };
        }

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

        public override string ToString()
        {
            return ToJson().ToString(Newtonsoft.Json.Formatting.None);
        }

        public PacketId Id { get; set; }
        public JToken Content { get; set; }

        public int Sequence { get; set; }
        public int? Response { get; set; }
    }

    public enum PacketId
    {
        None = 0,

        #region Common Codes
        UnknownCode,
        Error,
        Success,
        Message,
        Disconnect,
        #endregion

        #region Client -> Server Codes
        GetPlayerInfo,
        GetGameInfo,

        PlaceCards,
        SkipOrGiveupTurn,

        IndicateSkipsTurn,
        IndicatePickupCard,

        VoteStartGame,
        #endregion

        #region Server -> Client Codes
        ProvidePlayerInfo,
        ProvideGameInfo,
        BulkPickupCards,
        WaitingOnYou,
        NewCardsPlaced,
        #endregion
    }
}
