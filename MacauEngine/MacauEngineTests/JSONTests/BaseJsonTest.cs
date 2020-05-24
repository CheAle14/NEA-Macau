using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MacauEngineTests.JSONTests
{
    public abstract class BaseJsonTest
    {
        public abstract void EncodingPopulatesCorrectly();
        public abstract void DecodingPopulatesCorrectly();
        public abstract void DecodesPopulatesStringCorrectly();
    }
}
