using System;
using System.Collections.Generic;
using System.Diagnostics;
using AutoTest.helpers;
using NUnit.Framework;

namespace AutoTest.To_Others_Tests
{
    class GuidCoverage
    {
        [Test]
        public void Test()
        {
            var c = new Stopwatch();
            c.Start();

            var guidsInTests = new List<Guid>();
            var infoTests = PathCommands.GetTests();
            
            foreach (var infoTest in infoTests)
            {
                foreach (var guid in infoTest.GetGuids())
                {
                    if (!guidsInTests.Contains(guid))
                        guidsInTests.Add(guid);
                }
            }

            GoogleCommands.SetNewId();
            GoogleCommands.CheckCoverage(guidsInTests);
            
            Console.WriteLine(@"Time wasted "+c.Elapsed.TotalSeconds);
        }
    }
}
