using MacauEngine.Models;
using MacauEngine.Models.Enums;
using MacauEngine.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauEngine.Validators
{
    /// <summary>
    /// Determines whether the given set of cards can be placed on the current table card
    /// </summary>
    public class PlaceValidator
    {
        List<Card> PlacingDown;
        Card AlreadyOnTop;

        /// <summary>
        /// Instiantiates an instance with the provided cards to be placed, and the top table card
        /// </summary>
        /// <param name="placing">An enumerable of the cards to be placed</param>
        /// <param name="currentTableCard">The top table card</param>
        public PlaceValidator(IEnumerable<Card> placing, Card currentTableCard)
        {
            PlacingDown = placing.ToList();
            AlreadyOnTop = currentTableCard;
        }

        ValidationResult validate_GroupPlace()
        {
            if (PlacingDown.Count == 1)
                return ValidationResult.FromSuccess();
            // Can only place down groups of all the same *Value*
            // eg, three Twos, two Queens etc.
            var values = PlacingDown.Select(x => x.Value).Distinct();
            return values.Count() == 1
                ? ValidationResult.FromSuccess()
                : ValidationResult.FromError($"Cannot place {values.Count()} different card-types at once; must place all the same: eg three Twos");
        }

        /// <summary>
        /// Gets the result of the validation
        /// </summary>
        /// <returns></returns>
        public ValidationResult Validate()
        {
            var allCards = new List<Card>();
            allCards.Add(AlreadyOnTop);
            allCards.AddRange(PlacingDown);
            if (PlacingDown.Count <= 0)
                return ValidationResult.FromError($"Must place at least one card");
            var group = validate_GroupPlace();
            if (!group.IsSuccess)
                return group;
            for(int bottomIndex = 0; bottomIndex + 1 < allCards.Count; bottomIndex += 2)
            {
                int topIndex = bottomIndex + 1;
                var bottom = allCards[bottomIndex];
                var top = allCards[topIndex];
                var test = new CanPlaceOn(top, bottom);
                var result = test.Check();
                if (!result.IsSuccess)
                    return result;
            }
            return ValidationResult.FromSuccess();
        }
    }


    internal class CanPlaceOn : BaseValidator
    {
        Card _top;
        Card _bottom;
        public CanPlaceOn(Card top, Card bottom)
        {
            _top = top;
            _bottom = bottom;
        }

        bool canPlaceHouse(Suit house)
        {
            if (_top.Value == Number.Ace || _top.Value == Number.Joker)
                return true;
            return _top.House == house;
        }

        internal override ValidationResult Check()
        {
            if (_top == null || _top.Empty)
                return ValidationResult.FromError("Top card was null");
            if (_bottom == null || _bottom.Empty)
                return ValidationResult.FromError("Bottom card was null");
            if(_bottom.IsActive)
            {
                if(_bottom.Value == Number.Four)
                {
                    return _top.Value == Number.Four
                        ? ValidationResult.FromSuccess()
                        : ValidationResult.FromError("A Four is active; only another Four can be placed");
                }
                if(_bottom.IsPickupCard)
                {
                    return _top.IsDefenseCard || _top.IsPickupCard
                        ? ValidationResult.FromSuccess()
                        : ValidationResult.FromError($"A pickup card is active; only a defense or pickup card can be placed");
                }
                if(_bottom.IsDefenseCard)
                { // sevens wont be active, so this must be a king, so active cards are off the table.
                    return _top.IsDefenseCard
                        ? ValidationResult.FromSuccess()
                        : ValidationResult.FromError($"{_bottom} requires a defensive card, rather than {_top}");
                }
            }
            if (_top.Value == Number.Joker || _top.Value == Number.Ace)
                return ValidationResult.FromSuccess();
            if(_bottom.Value == Number.Ace)
            {
                if(_bottom.AceSuit.HasValue)
                {
                    return canPlaceHouse(_bottom.AceSuit.Value)
                        ? ValidationResult.FromSuccess()
                        : ValidationResult.FromError($"{_bottom} changes suit to {_bottom.AceSuit.Value}, where {_top} cannot be placed on that suit");
                }
                // ace is being placed in bulk so doesnt have a set suit, which means the player *probably* wants the top's suit to be selected.
                // hence, we implicitely allow it:
                return ValidationResult.FromSuccess();
            }
            if (_bottom.Value == Number.Joker && _bottom.IsActive == false)
                return ValidationResult.FromSuccess();
            return (_bottom.Value == _top.Value || _bottom.House == _top.House)
                ? ValidationResult.FromSuccess()
                : ValidationResult.FromError($"{_top} cannot be placed on {_bottom}");
        }
    }
}
