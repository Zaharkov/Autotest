using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoTest.helpers;
using NUnit.Framework;
using AutoTest.helpers.Parameters;

namespace LoadTest.helpers
{
    interface ILoadTest
    {
        void SetUp();
        IMethod SetUpMethod();
        List<int> GetList();
        List<IMethod> GetMethodsInfo();
    }

    [Serializable]
    class LoadCommands
    {
        [NonSerialized]
        public ParametersRead Param = ParametersRead.Instance();
        [NonSerialized]
        public ParametersInit ParamInit;
        [NonSerialized]
        public PostCommands Post;

        public LoadCommands(ParametersInit param = null)
        {
            ParamInit = param ?? new ParametersInit();
            Post = new PostCommands(ParamInit);
        }

        public void SetUpParam(ParametersInit param)
        {
            ParamInit = param;
            Post = new PostCommands(ParamInit);
        }

        [NonSerialized]
        private List<IMethod> _methods;

        public List<IMethod> GetMethodsInfo()
        {
            if (_methods != null)
                return _methods;

            var methods = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(t => t.GetCustomAttributes(typeof(TestAttribute), false).Any()).ToArray();

            if (!methods.Any()) 
                throw new Exception("У класса нет методов тестов");

            _methods = methods.Select(method => new MethodHelper(this, method)).Cast<IMethod>().ToList();
            return _methods;
        }

        [NonSerialized]
        private IMethod _setUpMethod;

        public IMethod SetUpMethod()
        {
            if (_setUpMethod != null)
                return _setUpMethod;

            var methods = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(t => t.GetCustomAttributes(typeof(SetUpAttribute), false).Any()).ToArray();

            if (!methods.Any())
                throw new Exception("У класса нет SetUp метода");

            _setUpMethod = new MethodHelper(this, methods.First());
            return _setUpMethod;
        }
    }
}


