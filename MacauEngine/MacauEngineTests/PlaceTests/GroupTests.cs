using MacauEngine.Models;
using MacauEngine.Models.Enums;
using MacauEngine.Validators;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MacauEngineTests.PlaceTests
{
    [TestFixture]
    public class GroupTests
    {
        [Test]
        public void CanPlacePair()
        {
            var cardB = new Card(Suit.Club, Number.Five);
            var cards = new List<Card>()
            {
                new Card(Suit.Club, Number.Six), // bottomest
                new Card(Suit.Spade, Number.Six) // top
            };
            var validator = new PlaceValidator(cards, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }
        [Test]
        public void CanPlaceTriple()
        {
            var cardB = new Card(Suit.Club, Number.Five);
            var cards = new List<Card>()
            {
                new Card(Suit.Club, Number.Six), // bottomest
                new Card(Suit.Diamond, Number.Six),
                new Card(Suit.Spade, Number.Six) // top
            };
            var validator = new PlaceValidator(cards, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }
        [Test]
        public void CanPlaceQuad()
        {
            var cardB = new Card(Suit.Club, Number.Five);
            var cards = new List<Card>()
            {
                new Card(Suit.Club, Number.Six), // bottomest
                new Card(Suit.Diamond, Number.Six),
                new Card(Suit.Heart, Number.Six),
                new Card(Suit.Spade, Number.Six) // top
            };
            var validator = new PlaceValidator(cards, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }
        [Test]
        public void FailPlaceMismatch()
        {
            var cardB = new Card(Suit.Club, Number.Five);
            var cards = new List<Card>()
            {
                new Card(Suit.Club, Number.Six), // bottomest
                new Card(Suit.Club, Number.Eight) // top
            };
            var validator = new PlaceValidator(cards, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, result.ErrorReason);
        }
    }
}
