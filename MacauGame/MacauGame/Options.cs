using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauGame
{
    [JsonObject(ItemRequired = Required.Always)]
    public class Options
    {
        public string Name { get; set; } = Environment.UserName;

        public string IP { get; set; } = "127.1";
    }
}
