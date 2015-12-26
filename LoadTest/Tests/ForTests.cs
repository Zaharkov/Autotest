using System;
using System.Collections.Generic;
using LoadTest.helpers;
using NUnit.Framework;

namespace LoadTest.Tests
{
    [Serializable]
    class ForTest : LoadCommands, ILoadTest
    {
        private readonly List<int> _list = new List<int>{100};

        public List<int> GetList()
        {
            return _list;
        }
        
        [SetUp]
        public void SetUp()
        { 
            
        }

        [Test]
        public void Test()
        {
            
        }
    }
}