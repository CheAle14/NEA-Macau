﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacauGame.UHWID
{
    public static class UHWIDEngine
    {
        public static string SimpleUid { get; private set; }

        public static string AdvancedUid { get; private set; }

        static UHWIDEngine()
        {
            var volumeSerial = DiskId.GetDiskId();
            var cpuId = CpuId.GetCpuId();
            var windowsId = WindowsId.GetWindowsId();
            SimpleUid = volumeSerial + cpuId;
            AdvancedUid = SimpleUid + windowsId;
        }
    }
}
