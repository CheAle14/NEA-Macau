using MacauEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MacauGame
{
    public class AwaitedPacket
    {
        public Packet Sent { get; set;  }
        public Packet Recieved { get; set; }
        public ManualResetEventSlim Holder { get; set; } 
    }
    public class AsyncAwaitedPacket : AwaitedPacket
    {
        public Action<Packet> Callback { get; set; }
        public int Timeout { get; set; }
    }
}
