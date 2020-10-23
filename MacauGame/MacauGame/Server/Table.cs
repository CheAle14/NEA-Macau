using MacauEngine.Models;
using MacauEngine.Models.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauGame.Server
{
    public class Table
    {
        public List<Card> Deck { get; set; }
        public List<Card> ShowingCards { get; set; }
        
        public void PlaceCard(Card c)
        {
            ShowingCards.Add(c);
            // Whether the card IsActive is handled in ClientBehaviour before this function is called.
        }

        List<Card> getOrderedDeck()
        {
            var nonShuffled = new List<Card>();
            foreach (Suit house in Enum.GetValues(typeof(Suit)))
            {
                if (house == Suit.None)
                    continue;
                foreach (Number value in Enum.GetValues(typeof(Number)))
                {
                    /*if (value == Number.None)
                        continue;
                    if (value == Number.Joker && house == Suit.Club)
                        continue;
                    if (value == Number.Joker && house == Suit.Diamond)
                        continue;
                    if(house == Suit.Club || house == Suit.Spade)
                    {
                        nonShuffled.Add(new Card(house, Number.King));
                    } else if (house == Suit.Heart || house == Suit.Diamond)
                    {
                        nonShuffled.Add(new Card(house, Number.Joker));
                    }*/
                    nonShuffled.Add(new Card(house, value));
                }
            }
            return nonShuffled;
        }

        public Table(int numDecks = 1)
        {
            Deck = new List<Card>();
            ShowingCards = new List<Card>();
            List<Card> nonShuffled;
            if(numDecks == 1)
            {
                nonShuffled = getOrderedDeck();
            } else
            {
                nonShuffled = new List<Card>();
                for (int i = 0; i < numDecks; i++)
                    nonShuffled.AddRange(getOrderedDeck());
            }

            while(nonShuffled.Count > 0)
            {
                int index = Program.RND.Next(0, nonShuffled.Count);
                var card = nonShuffled[index];
                nonShuffled.RemoveAt(index);
                Deck.Add(card);
            }
        }

        void FlipDeck()
        {
            // Showing has bottom at 0, top at end.
            // Deck as top at 0, bottom at end.
            // Hence, we need to remove from 0 and insert into end.

            // For now, we'll just flip one card at a time, whenever needed.
            // In future, I may attempt to bulk-flip as many as we should
            var card = ShowingCards[0];
            ShowingCards.RemoveAt(0);
            Deck.Add(card);
        }

        public Card DrawCard()
        {
            if (Deck.Count == 0)
                FlipDeck();
            if (Deck.Count == 0)
                return null;
            var drawnCard = Deck[0];
            Deck.RemoveAt(0);
            return drawnCard;
        }

    }
}
