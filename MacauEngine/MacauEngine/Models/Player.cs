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

        public List<Card> Hand { get; set; }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["id"] = Id;
            json["name"] = Name;
            json["hand"] = new JArray(Hand);
            return json;
        }

        public override void Update(JObject json)
        {
            Id = json["id"].ToObject<string>();
            Name = json["name"].ToObject<string>();
            Hand = json["hand"].ToObject<List<Card>>();
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
