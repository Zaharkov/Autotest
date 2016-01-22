using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using AutoTest.helpers;
using Digital.Common.Utils;
using NUnit.Framework;
using AutoTest.helpers.Selenium;
using AutoTest.helpers.Parameters;

namespace AutoTest
{
    /// <summary>
    /// Особый класс, через который "прогоняются" тесты
    /// Основной фокус в том, что сессия браузера не закрывается, а используется для след. теста
    /// Таким образом экономим время (запуск браузера - 10-15 сек занимает)
    /// </summary>
    sealed class ManyInOne : SeleniumCommands
    {
        public delegate bool GetTestInfoDelegate(out TestInfo testInfo);

        [Test]
        public void AllTests(GetTestInfoDelegate getTest, int times)
        {
            for (var i = 0; i < times; i++)
                Thread.Sleep(15000);

            SetUp();

            TestInfo test;
            while (getTest(out test))
            {

                test.ClassObject.SetUpGlobalParam(ParamInit, GetDriver(), GetLogoutHref());
                SetCurrentTestInfo(test);
                test.ClassObject.FileSaveDir = FileSaveDir;

                var time = new Stopwatch();
                time.Start();
                var criticalError = false;

                if (ParamInit.ParallelGuid.HasValue)
                    Mysql.SetDefaultTestData(test.Attr.Id, test.ClassName.Remove(0, 9), test.Attr.Owner);

                foreach (var methodInfo in test.MethodInfos)
                {
                    var testName = string.Format("{0}.{1}", test.ClassName, methodInfo.Name);
                    try
                    {
                        try
                        {
                            if (ParamInit.ParallelGuid.HasValue)
                                Mysql.AddTestLog(test.Attr.Id, ParamInit.ParallelGuid.Value, DateTime.Now, ParamInit.Login.Value);

                            methodInfo.Invoke(test.ClassObject, new object[] { });
                            ErrorInfo.AddErrorText(testName, "Test Ok", ErrorType.Ok);

                            //if (ErrorInfo.IsHaveNoError(testName))
                            //    test.ClassObject.();
                        }
                        catch (Exception e)
                        {
                            if (time.Elapsed.TotalSeconds < 60 * 20 && !ErrorInfo.IsBugInFail(testName))
                            {
                                time.Restart();
                                ErrorInfo.DefaultClassError(testName);

                                if (ParamInit.ParallelGuid.HasValue)
                                {
                                    Mysql.DeleteTestLog(test.Attr.Id, ParamInit.ParallelGuid.Value);
                                    Mysql.AddTestLog(test.Attr.Id, ParamInit.ParallelGuid.Value, DateTime.Now, ParamInit.Login.Value);
                                }

                                methodInfo.Invoke(test.ClassObject, new object[] { });
                                ErrorInfo.AddErrorText(testName, "Test Ok", ErrorType.Ok);

                                //if (ErrorInfo.IsHaveNoError(testName))
                                //    test.ClassObject.();
                            }
                            else
                            {
                                ProcessEx(e, testName, test);
                                criticalError = true;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        ProcessEx(e, testName, test);
                        criticalError = true;
                    }
                }

                if (ParamInit.ParallelGuid.HasValue)
                    Mysql.SetTestLogTimeEnd(test.Attr.Id, ParamInit.ParallelGuid.Value, DateTime.Now);

                var print = ConfigurationManager.AppSettings.Get("ErrorsToParallel");

                if (!string.IsNullOrEmpty(print))
                {
                    if (bool.Parse(print))
                    {
                        var text = ErrorInfo.GetErrorText(ErrorInfo.ErrorResultType.WithOutBug, test.ClassName);
                        if (!string.IsNullOrEmpty(text))
                            Console.WriteLine(text);
                    }
                }

                SetUpGlobalParam(ParamInit, test.ClassObject.GetDriver(), test.ClassObject.GetLogoutHref());

                time.Stop();

                var datetime = DateTime.Now;
                Mysql.InsertTestData(test.Attr.Id, test.ClassName.Remove(0, 9), time.Elapsed.TotalSeconds, criticalError, true, true, datetime, test.Attr.Owner);
            }

            TearDown();
        }

        private void ProcessEx(Exception e, string testName, TestInfo test)
        {
            var failGuids = ErrorInfo.GuidCheck(testName);
            ErrorInfo.GuidEnd(testName);
            var stack = string.Format("{0} ({1}){2}{3}", e.GetBaseException().GetAllMessages(), GetDateForDebug(), Environment.NewLine, e.GetBaseException().GetStackTrace());
            
            ErrorInfo.AddErrorText(testName, string.Format("{0} ({1}){2}", stack.Replace(PathCommands.GetProjectDir().Replace("C:\\", "c:\\"), ""), ParamInit.Login.Value, ErrorInfo.FailGuidsToStr(failGuids)), ErrorType.NotOk);

            AddErrorLog(stack.Replace(PathCommands.GetProjectDir().Replace("C:\\", "c:\\"), ""),
                        ErrorType.NotOk, null, DateTime.Now,
                        ErrorInfo.IsBugInFail(testName) ? "Баг в Failed" : null, null, ErrorInfo.IsBugInFail(testName), test.Attr.Id, failGuids);
        }
    }
}
