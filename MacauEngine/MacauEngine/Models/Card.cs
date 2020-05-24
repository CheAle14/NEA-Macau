using MacauEngine.Models.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Models
{
    /// <summary>
    /// Represents a common playing card
    /// </summary>
    public class Card : MacauObject, IEquatable<Card>
    {
        /// <summary>
        /// Constructs a card instance from a networked set of values
        /// </summary>
        /// <param name="obj"></param>
        public Card(JObject obj) : base(obj) { }
        /// <summary>
        /// Constructs a card instance from the given values
        /// </summary>
        public Card(Suit house, Number value)
        {
            House = house;
            Value = value;
        }
        /// <summary>
        /// Gets or sets the <see cref="Suit"/> of this Card
        /// </summary>
        public Suit House { get; set; }

        /// <summary>
        /// Gets or sets the suit this Ace changes to. If this is not an Ace, this is null.
        /// </summary>
        public Suit? AceSuit { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Number"/> of this Card
        /// </summary>
        public Number Value { get; set; }
        /// <summary>
        /// Gets whether this Card can force another player to pick cards up
        /// </summary>
        public bool IsPickupCard => Value == Number.Two || Value == Number.Three || Value == Number.Joker;
        /// <summary>
        /// Gets whether this Card can defend against pickup cards
        /// </summary>
        public bool IsDefenseCard => Value == Number.Seven || Value == Number.King;
        /// <summary>
        /// Gets whether this Card has some sort of special effect
        /// </summary>
        public bool IsSpecialCard => IsPickupCard || IsDefenseCard || Value == Number.Ace || Value == Number.Four;

        /// <summary>
        /// Gets or sets whether this Card is currently active
        /// </summary>
        public bool IsActive { get; set; }

        internal bool Empty => Value == Number.None || House == Suit.None;

        /// <inheritdoc/>
        public override void Update(JObject obj)
        {
            House = obj["house"].ToObject<Suit>();
            Value = obj["value"].ToObject<Number>();
            IsActive = obj["active"].ToObject<bool>();
        }

        /// <inheritdoc/>
        public override JObject ToJson()
        {
            var json = new JObject();
            json["house"] = JValue.FromObject(House);
            json["value"] = JValue.FromObject(Value);
            json["active"] = IsActive;
            return json;
        }

        /// <summary>
        /// Determines whether two card objects refer to the same playing card
        /// </summary>
        /// <param name="other">Other instance to compare with</param>
        /// <returns>True if both cards refer to the same playing card</returns>
        public bool Equals(Card other)
        {
            if (other == null)
                return false; // since we aren't null.
            return other.House == this.House && other.Value == this.Value;
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -534054243;
            hashCode = hashCode * -1521134295 + House.GetHashCode();
            hashCode = hashCode * -1521134295 + Value.GetHashCode();
            return hashCode;
        }
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is Card o)
                return Equals(o);
            return false;
        }

        public override string ToString()
        {
            return $"{House}{Value}" + (IsActive ? "(A)" : "");
        }
    }
}
