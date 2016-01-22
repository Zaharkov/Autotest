using System;
using NUnit.Framework;

namespace AutoTest.To_Others_Tests
{
    class NewGuid
    {
        [Test]
        public void Test()
        {
            for (int i = 1; i <= 10; i++)
            {
                Console.WriteLine(Guid.NewGuid().ToString().ToUpper());
            }
        }
    }
}
