using MacauEngine.Models;
using MacauEngine.Models.Enums;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MacauEngineTests.JSONTests
{
    [TestFixture]
    public class CardJsonTests : BaseJsonTest
    {
        /// <summary>
        /// Ensures that the ToJson() function populates the JObject correctly
        /// </summary>
        [Test]
        public override void EncodingPopulatesCorrectly()
        {
            var card = new Card(Suit.Club, Number.Five);
            var jsonValue = card.ToJson();
            Assert.NotNull(jsonValue);
            var suitValue = jsonValue["house"];
            Assert.NotNull(suitValue);
            Assert.AreEqual(Suit.Club, suitValue.ToObject<Suit>());
            var numberValue = jsonValue["value"];
            Assert.NotNull(numberValue);
            Assert.AreEqual(Number.Five, numberValue.ToObject<Number>());
        }

        [Test]
        public override void DecodingPopulatesCorrectly()
        {
            var card = new Card(Suit.Diamond, Number.Ace);
            var jsonValue = card.ToJson();

            var secondCard = new Card(jsonValue);
            bool equal = card.Equals(secondCard);
            Assert.AreEqual(true, equal);
        }

        [Test]
        public override void DecodesPopulatesStringCorrectly()
        {
            var card = new Card(Suit.Club, Number.Joker) { IsActive = true };
            var jsonValue = card.ToJson();

            var jsonString = jsonValue.ToString();

            var secondJson = JObject.Parse(jsonString);

            var secondCard = new Card(secondJson);
            bool equal = card.Equals(secondCard);
            Assert.AreEqual(true, equal);
        }
    }
}
