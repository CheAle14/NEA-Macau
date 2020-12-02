using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Models
{
    /// <summary>
    /// Represents the data of a generic player
    /// </summary>
    public class Player : MacauObject
    {
        /// <summary>
        /// Instantiates the player instance from JSON
        /// </summary>
        /// <param name="obj">The JSON content to be loaded, passed to the Update function</param>
        public Player(JObject obj) : base(obj) {  }

        /// <summary>
        /// Instantiates the player instance using the provided information
        /// </summary>
        /// <param name="id">The unique hardware ID of the player</param>
        /// <param name="name">The non-unique name of the player</param>
        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets or sets the unique hardware ID of the player
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the display name of the player
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the position of the player in relation to the other players is, where 0 is first
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the number of turns the player is currently missing
        /// </summary>
        public int MissingGoes { get; set; }
        /// <summary>
        /// Gets or sets whether the player is missing multiple turns
        /// </summary>
        public bool MultiTurnSkip { get; set; }

        /// <summary>
        /// Gets or sets the list containing the cards the player has within their hand
        /// </summary>
        public List<Card> Hand { get; set; }

        /// <summary>
        /// Gets or sets whether the player has voted to start the game
        /// </summary>
        public bool VotedToStart { get; set; }

        /// <summary>
        /// Gets whether the player is considered Finished due to no longer having any cards
        /// </summary>
        public bool Finished => Hand != null && Hand.Count == 0;

        /// <summary>
        /// Counter used to increment the position a player ended in
        /// </summary>
        public static int _position = 0;

        /// <summary>
        /// Gets or sets the position a player ended in, or null if not finished
        /// </summary>
        public int? FinishedPosition { get; set; }

        /// <summary>
        /// Converts this instance into its JSON representation
        /// </summary>
        /// <param name="includeHand">If true, include the cards of the player; if false, an empty list</param>
        public JObject ToJson(bool includeHand)
        {
            var json = new JObject();
            json["id"] = Id;
            json["name"] = Name;
            json["order"] = Order;
            if (includeHand)
                json["hand"] = Hand == null 
                    ? new JArray(new List<Card>()) 
                    : new JArray(Hand.Select(x => x.ToJson()));
            else
                json["hand"] = null;
            if (MissingGoes > 0)
                json["miss"] = MissingGoes;
            json["start"] = VotedToStart;
            return json;
        }
        /// <summary>
        /// Converts this instance into its JSON representation, with the Hand as an empty list
        /// </summary>
        /// <returns>JSON representation without the hand</returns>
        public override JObject ToJson() => ToJson(false);

        /// <summary>
        /// Sets the instances values from the JSON object
        /// </summary>
        /// <param name="json">JSON content from which to load values from</param>
        public override void Update(JObject json)
        {
            Id = json["id"].ToObject<string>();
            Name = json["name"].ToObject<string>();
            var hand = json["hand"];
            if(hand is JArray array)
            {
                Hand = array.Select(x => new Card((JObject)x)).ToList();
            } else
            {
                Hand = null;
            }
            if (json.TryGetValue("miss", out var tkn))
                MissingGoes = tkn.ToObject<int>();
        }

        /// <summary>
        /// Returns true if the compared object is equal to this instance
        /// </summary>
        /// <param name="obj">The object to compare to</param>
        public override bool Equals(object obj)
        {
            if (obj is Player o)
                return Equals(o);
            return false;
        }

        /// <summary>
        /// Determines equality with another player, based only on the Id and Name
        /// </summary>
        /// <param name="other">Other player with which to compare</param>
        public bool Equals(Player other)
        {
            if (other == null)
                return false;
            return other.Id == Id && other.Name == other.Name;
        }
    }
}
