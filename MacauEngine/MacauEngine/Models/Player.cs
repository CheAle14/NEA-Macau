using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Models
{
    public class Player : MacauObject
    {
        public Player(JObject obj) : base(obj) {  }
        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }
        public string Id { get; set; }
        public string Name { get; set; }

        public int Order { get; set; }

        public int MissingGoes { get; set; }
        public bool MultiTurnSkip { get; set; }

        public List<Card> Hand { get; set; }

        public bool Finished => Hand != null && Hand.Count == 0;

        public JObject ToJson(bool includeHand)
        {
            var json = new JObject();
            json["id"] = Id;
            json["name"] = Name;
            json["order"] = Order;
            if (includeHand)
                json["hand"] = new JArray(Hand);
            else
                json["hand"] = new JArray(new List<Card>());
            if (MissingGoes > 0)
                json["miss"] = MissingGoes;
            return json;
        }
        public override JObject ToJson() => ToJson(false);

        public override void Update(JObject json)
        {
            Id = json["id"].ToObject<string>();
            Name = json["name"].ToObject<string>();
            Hand = json["hand"].ToObject<List<Card>>();
            if (json.TryGetValue("miss", out var tkn))
                MissingGoes = tkn.ToObject<int>();
        }

        public override bool Equals(object obj)
        {
            if (obj is Player o)
                return Equals(o);
            return false;
        }

        public bool Equals(Player other)
        {
            if (other == null)
                return false;
            return other.Id == Id && other.Name == other.Name;
        }
    }
}
