using MacauEngine.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MacauEngineTests.JSONTests
{
    [TestFixture]
    public class PlayerJsonTests : BaseJsonTest
    {
        [Test]
        public override void DecodesPopulatesStringCorrectly()
        {
            var firstPlayer = new Player("asdasdasdqweqwe", "somenameasdad123123123\\@");
            var json = firstPlayer.ToJson();

            var str = json.ToString();

            var secondJson = JObject.Parse(str);

            var secondPlayer = new Player(secondJson);
            bool equals = firstPlayer.Equals(secondPlayer);
            Assert.AreEqual(true, equals);
        }

        [Test]
        public override void DecodingPopulatesCorrectly()
        {
            var firstPlayer = new Player("asdasdasdqweqwe", "somenameasdad123123123\\@");
            var json = firstPlayer.ToJson();

            var secondPlayer = new Player(json);
            bool equals = firstPlayer.Equals(secondPlayer);
            Assert.AreEqual(true, equals);
        }

        [Test]
        public override void EncodingPopulatesCorrectly()
        {
            string id = "someHWIDid";
            string name = "CraigFromCraigAndDave";
            var player = new Player(id, name);
            var json = player.ToJson();
            Assert.AreEqual(id, json["id"].ToString());
            Assert.AreEqual(name, json["name"].ToString());
        }
    }
}
