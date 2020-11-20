using MacauEngine.Models.Enums;
using Newtonsoft.Json;
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

        public bool IsRed => House == Suit.Diamond || House == Suit.Heart;
        public bool IsBlack => House == Suit.Club || House == Suit.Spade;

        public string ImageName { get
            {
                if (Value == Number.Joker)
                    return IsRed
                        ? $"R_{(int)Value}"
                        : $"B_{(int)Value}";
                return $"{House.ToString()[0]}_{(int)Value}";
            }
        }

        /// <summary>
        /// Gets the number of cards a player has to pickup if attacked by this one
        /// </summary>
        public int PickupValue {  get
            {
                switch(Value)
                {
                    case Number.Two:
                        return 2;
                    case Number.Three:
                        return 3;
                    case Number.Joker:
                        return IsRed ? 10 : 5;
                    default:
                        return 0;
                }
            } }

        internal bool Empty => Value == Number.None || House == Suit.None;

        /// <inheritdoc/>
        public override void Update(JObject obj)
        {
            House = obj["house"].ToObject<Suit>();
            Value = obj["value"].ToObject<Number>();
            IsActive = obj["active"]?.ToObject<bool>() ?? false;
            AceSuit = obj["suit"]?.ToObject<Suit?>() ?? null;
        }

        /// <inheritdoc/>
        public override JObject ToJson()
        {
            var json = new JObject();
            json["house"] = JValue.FromObject(House);
            json["value"] = JValue.FromObject(Value);
            if(AceSuit.HasValue)
                json["suit"] = JValue.FromObject(AceSuit.Value);
            if(IsActive || AceSuit.HasValue)
                json["active"] = IsActive;
            return json;
        }

        /// <summary>
        /// Determines whether two card objects refer to the same playing card
        /// </summary>
        /// <param name="other">Other instance to compare with</param>
        /// <returns>True if both cards refer to the same playing card</returns>
        public bool Equals(Card other)
        { // Note: Must not use AceSuit, unless you also change 'PlaceCards' operation in ClientBehaviour. 
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

        string baseText()
        {
            if (Value == Number.Joker)
                return (((int)House & 0b1_00) == 0) // check red
                    ? "Red Joker"
                    : "Black Joker";
            return $"{Value} of {House}s";
        }

        public override string ToString()
        {
            return baseText() + (IsActive ? " (Active)" : "");
        }
    }
}
