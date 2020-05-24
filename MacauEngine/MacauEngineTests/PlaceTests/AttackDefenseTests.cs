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
    public class AttackDefenseTests
    {
        [TestCase(Number.Two)]
        [TestCase(Number.Three)]
        [TestCase(Number.Joker)]
        public void CanStackAttackCards(Number cardNumber)
        {
            // This test will simply check the attack cards stack properly.
            // We don't care about suit-checking yet.
            var cardB = new Card(Suit.Diamond, Number.Two)
            {
                IsActive = true,
            };
            var cardT = new Card(Suit.Diamond, cardNumber);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, $"{cardNumber}: {result.ErrorReason}");
        }
    
        [Test]
        public void CanJokerPlaceAnySuit()
        {
            var cardB = new Card(Suit.Spade, Number.Five);
            var cardT = new Card(Suit.Heart, Number.Joker);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, $"{result.ErrorReason}");
        }

        [TestCase(Number.Two)]
        [TestCase(Number.Three)]
        public void FailPlaceWrongSuit(Number cardNumber)
        {
            // Placing a two needs a three to check against, otherwise we're not checking suits
            // since a two can place on a two through its Value.
            var cardB = new Card(Suit.Diamond, cardNumber == Number.Two ? Number.Three : Number.Two);
            var cardT = new Card(Suit.Club, cardNumber);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, $"{cardNumber}: {result.ErrorReason}");
        }
    
        [TestCase(Number.Two)]
        [TestCase(Number.Three)]
        [TestCase(Number.Joker)]
        public void FailPlaceAttackOnDefense(Number cardNumber)
        {
            var cardB = new Card(Suit.Diamond, Number.King)
            {
                IsActive = true
            };
            var cardT = new Card(Suit.Heart, cardNumber);
            var validator = new PlaceValidator(new List<Card>(){ cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, $"{result.ErrorReason}");
        }
    
        [TestCase(Number.Seven, Number.Two)]
        [TestCase(Number.Seven, Number.Three)]
        [TestCase(Number.Seven, Number.Joker)]
        [TestCase(Number.King, Number.Two)]
        [TestCase(Number.King, Number.Three)]
        [TestCase(Number.King, Number.Joker)]
        public void CanPlaceDefenseAnyAttack(Number defense, Number attack)
        {
            var cardB = new Card(Suit.Heart, attack) { IsActive = true };
            var cardT = new Card(Suit.Spade, defense);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }
    
        [TestCase(Number.Four)]
        [TestCase(Number.Five)]
        [TestCase(Number.Six)]
        [TestCase(Number.Eight)]
        [TestCase(Number.Nine)]
        [TestCase(Number.Ten)]
        [TestCase(Number.Jack)]
        [TestCase(Number.Queen)]
        [TestCase(Number.Ace)]
        public void MustPlaceSpecialOnAttack(Number number)
        {
            var cardB = new Card(Suit.Club, Number.Two) { IsActive = true };
            var cardT = new Card(Suit.Diamond, number);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, result.ErrorReason);
        }

        [TestCase(Number.Four)]
        [TestCase(Number.Five)]
        [TestCase(Number.Six)]
        [TestCase(Number.Eight)]
        [TestCase(Number.Nine)]
        [TestCase(Number.Ten)]
        [TestCase(Number.Jack)]
        [TestCase(Number.Queen)]
        [TestCase(Number.Ace)]
        public void MustPlaceSpecialOnDefense(Number number)
        {
            var cardB = new Card(Suit.Heart, Number.King) { IsActive = true };
            var cardT = new Card(Suit.Diamond, number);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, result.ErrorReason);
        }

    }
}
