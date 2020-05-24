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
    public class FourPlaceTests
    {
        [TestCase(Suit.Club)]
        [TestCase(Suit.Diamond)]
        [TestCase(Suit.Heart)]
        [TestCase(Suit.Spade)]
        public void CanStackFours(Suit suit)
        {
            var cardB = new Card(Suit.Club, Number.Four) { IsActive = true };
            var cardT = new Card(suit, Number.Four);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(true, result.IsSuccess, result.ErrorReason);
        }

        [TestCase(Number.Two)] // attack card
        [TestCase(Number.Six)] // normal card
        [TestCase(Number.Seven)] // defense
        [TestCase(Number.King)] // defense
        [TestCase(Number.Joker)] // global card
        [TestCase(Number.Ace)] // global card
        public void MustPlaceFourOnActiveFour(Number number)
        {
            var cardB = new Card(Suit.Club, Number.Four) { IsActive = true };
            var cardT = new Card(Suit.Club, number);
            var validator = new PlaceValidator(new List<Card>() { cardT }, cardB);
            var result = validator.Validate();
            Assert.AreEqual(false, result.IsSuccess, result.ErrorReason);
        }
    
        
    }
}
