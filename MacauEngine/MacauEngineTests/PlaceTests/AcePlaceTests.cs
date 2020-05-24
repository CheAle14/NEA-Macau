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
    public class AcePlaceTests
    {
        [TestCase(Number.Two)]
        [TestCase(Number.Three)]
        [TestCase(Number.Four)]
        [TestCase(Number.Five)]
        [TestCase(Number.Six)]
        [TestCase(Number.Seven)]
        [TestCase(Number.Eight)]
        [TestCase(Number.Nine)]
        [TestCase(Number.Ten)]
        [TestCase(Number.Jack)]
        [TestCase(Number.Queen)]
        [TestCase(Number.King)]
        [TestCase(Number.Ace)]
        public void CanPlaceAceOnAny(Number number)
        {
            var cardB = new Card(Suit.Spade, number);
            var cardT = new Card(Suit.Diamond, Number.Ace);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }
    
        [Test]
        public void CanPlaceOnChangedSuit()
        {
            var cardB = new Card(Suit.Club, Number.Ace)
            {
                IsActive = true,
                AceSuit = Suit.Diamond
            };
            var cardT = new Card(Suit.Diamond, Number.Four);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }

        [Test]
        public void FailPlaceOnAceSuitRatherThanChangedSuit()
        {
            // If ace is a Club, but changes to Diamond, we still can't place any Clubs down.
            var cardB = new Card(Suit.Club, Number.Ace)
            {
                IsActive = true,
                AceSuit = Suit.Diamond
            };
            var cardT = new Card(Suit.Club, Number.Jack);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, result.ErrorReason);
        }
    }
}
