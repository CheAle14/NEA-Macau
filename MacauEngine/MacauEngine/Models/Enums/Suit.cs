using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Models.Enums
{
    /// <summary>
    /// Represents the House/Suit of a playing card
    /// </summary>
    public enum Suit
    {
        /// <summary>
        /// Default value, no suit set
        /// </summary>
        None =    0b0_000,
        /// <summary>
        /// Represents the Red Diamonds
        /// </summary>
        Diamond = 0b0_01,
        /// <summary>
        /// Represents Red Hearts
        /// </summary>
        Heart =   0b0_10,
        /// <summary>
        /// Represents Black Spades
        /// </summary>
        Spade =   0b1_01,
        /// <summary>
        /// Represents Black Clubs
        /// </summary>
        Club =    0b1_10,
    }
}
