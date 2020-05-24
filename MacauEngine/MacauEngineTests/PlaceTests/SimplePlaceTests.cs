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
    public class SimplePlaceTests
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
        public void CanPlaceOnSameValue(Number cardNumber)
        {
            var cardB = new Card(Suit.Club, cardNumber);
            var cardT = new Card(Suit.Diamond, cardNumber);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, $"Reason: {result.ErrorReason}");
        }

        [TestCase(Suit.Diamond)]
        [TestCase(Suit.Heart)]
        [TestCase(Suit.Club)]
        [TestCase(Suit.Spade)]
        public void CanPlaceOnSameSuit(Suit suit)
        {
            var cardB = new Card(suit, Number.Six);
            var cardT = new Card(suit, Number.Eight);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, $"Reason: {result.ErrorReason}");
        }

        [Test]
        public void FailPlaceDifferentValueAndSuit()
        {
            var cardB = new Card(Suit.Club, Number.Nine);
            var cardT = new Card(Suit.Diamond, Number.Eight);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, $"Reason: {result.ErrorReason}");
        }
    }
}
