using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using AutoTest.helpers;
using AutoTest.helpers.Parameters;

namespace LoadTest.helpers
{
    [Serializable]
    class TestInfo
    {
        public readonly LoadCommands TestObj;
        public ILoadTest TestIFace;

        public TestInfo(LoadCommands obj)
        {
            TestObj = obj;
            var face = obj as ILoadTest;
            TestIFace = face;
        }

        public TestInfo CloneObj(ParametersInit param)
        {
            var type = TestObj.GetType();
            var obj = Activator.CreateInstance(type) as LoadCommands;

            var newObj = new TestInfo(obj);
            newObj.SetUpParam(param);

            return newObj;
        }

        public string GetName()
        {
            return TestObj.GetType().Name;
        }

        public void SetUpParam(ParametersInit param)
        {
            TestObj.SetUpParam(param);
        }
    }

    class MethodHelper : IMethod
    {
        private readonly object _obj;
        private readonly MethodInfo _method;

        public MethodHelper(object obj, MethodInfo method)
        {
            _obj = obj;
            _method = method;
        }

        private void Invoke()
        {
            _method.Invoke(_obj, new object[] {});
        }

        public Action Func()
        {
            return Invoke;
        }

        public string GetName()
        {
            return _method.Name;
        }

        public string GetClassName()
        {
            return _obj.GetType().Name;
        }

        public bool IsLogged()
        {
            var name = GetName();
            return !name.Contains("Skip") && !name.Contains("SetUp");
        }
    }

    interface IMethod
    {
        Action Func();
        string GetName();
        string GetClassName();
        bool IsLogged();
    }

    [Serializable]
    class ThreadTest
    {
        public TestInfo Test;
        public int Thread;
        public ThreadTest(TestInfo test, int thread)
        {
            Test = test;
            Thread = thread;
        }
    }
    
    [Serializable]
    class Error
    {
        public DateTime TimeBegin;
        public long Duration;
        public string Text;

        public Error(DateTime timeBegin, long duration, string text)
        {
            TimeBegin = timeBegin;
            Duration = duration;
            Text = text;
        }
    }

    static class TestsInfo
    {
        public static List<TestInfo> TestList = new List<TestInfo>();

        static TestsInfo()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace != null && t.Namespace.Contains("LoadTest.Tests"));
            var tests = types.Where(t => t.GetInterface("ILoadTest") != null && t.BaseType == typeof(LoadCommands)).ToArray();

            foreach (var test in tests)
            {
                var obj = Activator.CreateInstance(test) as LoadCommands;
                var face = obj as ILoadTest;

                if (obj != null && face != null)
                    TestList.Add(new TestInfo(obj));
            }
        }
    }

    static class ErrorInfo
    {
        private static Dictionary<ThreadTest, Dictionary<string, Dictionary<int, Error>>> _errorList
            = new Dictionary<ThreadTest, Dictionary<string, Dictionary<int, Error>>>();

        private static readonly object LockObj = new object();

        public static void AddError(ThreadTest test, string methodName, int threadNum, DateTime date, long time, string text = null)
        {
            lock (LockObj)
            {
                if (_errorList.Any(t => t.Key.Equals(test)))
                {
                    if (_errorList[test].ContainsKey(methodName))
                    {
                        if (_errorList[test][methodName].ContainsKey(threadNum))
                            _errorList[test][methodName][threadNum] = new Error(date, time, text);
                        else
                            _errorList[test][methodName].Add(threadNum, new Error(date, time, text));
                    }
                    else
                        _errorList[test].Add(methodName, new Dictionary<int, Error> { { threadNum, new Error(date, time, text) } });
                }
                else
                    _errorList.Add(test, new Dictionary<string, Dictionary<int, Error>> 
                    { { methodName, new Dictionary<int, Error> { { threadNum, new Error(date, time, text) } } } });
            }
        }

        public static List<OutPutClass> GetResult()
        {
            var result = new List<OutPutClass>();
            var threadNumber = 0;
            foreach (var threadTest in _errorList)
            {
                foreach (var test in threadTest.Value)
                {
                    var name = threadTest.Key.Test.GetName() + test.Key;//+ threadTest.Key.Thread;
                    threadNumber++;
                    foreach (var thread in test.Value)
                    {
                        var duration = thread.Value.Duration;
                        var startTime = thread.Value.TimeBegin;
                        result.Add(new OutPutClass(name, duration, threadNumber, startTime, thread.Value.Text));
                    }
                }
            }

            return result;
        }
        
        public static void DumpResult(string dumpName)
        {
            var path = PathCommands.SharedFolder + dumpName;
            var s = new BinaryFormatter();

            using (var fs = File.Open(path, FileMode.Create))
            {
                s.Serialize(fs, _errorList);
            }
        }

        public static void LoadDumpResult(string dumpName)
        {
            var path = PathCommands.SharedFolder + dumpName;
            var s = new BinaryFormatter();

            using (var fs = File.Open(path, FileMode.Open))
            {
                var s2 = s.Deserialize(fs).CastToType(_errorList);
                _errorList = s2;
            }
        }
    }
}
