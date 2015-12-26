using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoTest.helpers;
using NUnit.Framework;

namespace LoadTest.helpers
{
    class ParallelLoad
    {
        private List<TestInfo> _testList;
        private ThreadTest _currentThreadTest;

        public void PrepareData()
        {
            ParametersInit.ThreadCount = _currentThreadTest.Thread;
            _testList = new List<TestInfo>();

            for (var i = 0; i < _currentThreadTest.Thread; i++)
            {
                var paramTask = new ParametersInit();
                var test = _currentThreadTest.Test.CloneObj(paramTask);
                _testList.Add(test);
            }
        }

        private bool GetCurrentTest()
        {
            if (!_parallelList.Any())
                return false;

            var test = _parallelList.First();
            _currentThreadTest = test;

            _parallelList.Remove(test);

            return true;
        }

        private List<ThreadTest> _parallelList = new List<ThreadTest>();
        [SetUp]
        public void PrepareTestList()
        {
            if (!TestsInfo.TestList.Any())
                return;

            var b = new List<ThreadTest>();
            var a = new List<ThreadTest>();

            foreach (var loadTest in TestsInfo.TestList)
                foreach (var number in loadTest.TestIFace.GetList())
                { 
                    a.Add(new ThreadTest(loadTest, number));
                    b.Add(new ThreadTest(loadTest, number));
                }

            _parallelList = a.OrderBy(t => t.Thread).ToList();
            _parallelList = _parallelList.Concat(b.OrderByDescending(t => t.Thread)).ToList();
        }

        private static readonly Stopwatch StopWatch = new Stopwatch();

        [Test]
        public void RunTests()
        {
            StopWatch.Start();

            while (GetCurrentTest())
            {
                PrepareData();
                RunParallel();

                Console.WriteLine(@"Test " + _currentThreadTest.Test.GetName() + @" for "
                    + _currentThreadTest.Thread + @" threads is done");
            }

            //Собираю результаты
            var result = ErrorInfo.GetResult();

            try
            {
                GoogleCommands.SaveResult(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ErrorInfo.DumpResult("LoadTest_dump " + DateTime.Now.ToLongDateString() + ".xml");
                throw;
            }
           
        }

        public void RunParallel()
        {

            DoTaskWork(true);

            var swTotal = Stopwatch.StartNew();

            for (var j = 0; j < _currentThreadTest.Test.TestIFace.GetMethodsInfo().Count(); j++)
                DoTaskWork(false, j);

            swTotal.Stop();
        }

        private void DoTaskWork(bool setUp = false, int testNumber = 0)
        {
            var tasks = new Task[_currentThreadTest.Thread];

            for (var i = 0; i < _currentThreadTest.Thread; i++)
            {
                var j = i;
                var method = setUp
                    ? _testList[j].TestIFace.SetUpMethod()
                    : _testList[j].TestIFace.GetMethodsInfo()[testNumber];

                tasks[j] = Task.Factory.StartNew(() => RunMethod(method, j));
            }

            foreach (var task in tasks)
                task.Wait();
        }

        private void RunMethod(IMethod method, int threadNumber)
        {
            var sw = Stopwatch.StartNew();
            var time = new DateTime(StopWatch.Elapsed.Ticks);
            string error;
            try
            {
                method.Func()();
                error = null;
            }
            catch (Exception e)
            {
                var ex = e.GetBaseException();
                Console.WriteLine(error = @"Exception in " + method.GetName() + @" " + method.GetClassName() + @" (" + threadNumber + @")" +
                    ex.Message + Environment.NewLine + ex.StackTrace);
            }
            sw.Stop();

            if (method.IsLogged())
                ErrorInfo.AddError(_currentThreadTest, method.GetName(), threadNumber, time, sw.ElapsedMilliseconds, error);
        }
    }
}
