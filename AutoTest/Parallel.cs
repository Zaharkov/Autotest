using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AutoTest.helpers;
using Digital.Common.Utils;
using NUnit.Framework;
using AutoTest.helpers.Parameters;

namespace AutoTest
{
    /// <summary>
    /// Класс запуска тестов в параллельном режиме
    /// с использование цикла
    /// </summary>
    public class Parallel
    {
        protected ParametersInit Param;
        private List<TestInfo> _testsInfo;
        private SqlCommands _mysql;

        public void InitParam()
        {
            Param = new ParametersInit
            {
                Parallel = true,
            };
            _mysql = new SqlCommands();
            _testsInfo = new List<TestInfo>();
        }

        public virtual HubHolder GetHubHolder()
        {
            var hubHolder = new HubHolder("locahost");
            //hubHolder.AddNode("");

            return hubHolder;
        }

        public virtual List<ParametersProject> GetProjects()
        {
            var projects = new List<ParametersProject>
            {
                new ParametersProject(ProjectType.None)
            };

            return projects;
        }

        public virtual void PrepareProcess(HubHolder hubHolder)
        {
            //SeleniumProcess.ReloadVirtuals(hubHolder);
            //SeleniumProcess.ReloadProcess(hubHolder);
            SeleniumProcess.CleanVirtuals(hubHolder);
        }

        public virtual void CreateColumnAndMailToStart()
        {
            GoogleCommands.LoadingGuidTexts = true;
            GoogleCommands.LoadGuidsText();
            GoogleCommands.MakeNewCol(string.Format("{0} {1}{2}{3}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), Environment.NewLine, Param.Address));

            foreach (var mail in AllProjects.Mails)
                MailCommands.Action.SendStartTests(mail.Value,
                    string.Format("Автотесты были запущены на адресе {0}", Param.Address));
        }

        public virtual void ChangeLogin(ParametersInit param, LoginInfo login)
        {
            param.Login.SetValue(login.Login);
            param.Password.SetValue(login.Password);
        }

        [Test]
        public virtual void ParallelInFor()
        {
            InitParam();
            TestInParallel();
        }

        public void TestInParallel(string specName = null)
        {
            var hubHolder = GetHubHolder();
            var saveDir = string.Format("{0}{1}", Param.LogSaveDir, !string.IsNullOrEmpty(specName) ? specName + "\\" : "");

            DefaultMailAndFolder(saveDir);
            PathCommands.DelTree(string.Format("{0}screens", saveDir));
            ErrorInfo.DefaultFiles(saveDir);
            PrepareProcess(hubHolder);

            var projects = GetProjects();
            var threadsParams = new Dictionary<int, ParametersInit>();

            PrepareTestsArrays(projects);
            var threadsCount = Math.Min(_testsInfo.Count, ParametersInit.ThreadCount);
            var logins = new List<LoginInfo>();//??
            StartParallelLog(Param.Address.Replace("http://", ""), DateTime.Now, string.Format("{0}screens\\", saveDir), _testsInfo.Count);

            for (var i = 0; i < threadsCount; i++)
            {
                var param = new ParametersInit
                {
                    Host = hubHolder.Hub,
                    RemoteWd = true,
                    Parallel = true,
                    Address = Param.Address,
                    ParallelGuid = Param.ParallelGuid
                };

                param.LogSaveDir += !string.IsNullOrEmpty(specName) ? string.Format("{0}\\", specName) : "";

                ChangeLogin(param, logins[i]);

                threadsParams[i] = param;

                // не забыть убрать
                //var myTest = _globalParam.Path.GetInfoTests(new List<string> {"026"});
                //
                //for (var i = 0; i < myTest.Count; i++)
                //    myTest[myTest.ElementAt(i).Key] = myTest.ElementAt(i).Value.TouchClassObject();
                //
                //threadsTestInfo[sortArray.Key].TestsInfo = myTest;
            }

            var webDrivers = new ManyInOne[threadsParams.Count];
            var timeMax = TimeSpan.FromSeconds(GetMaxTime(threadsCount, hubHolder.Nodes.Count));

            CreateColumnAndMailToStart();

            var timeNow = DateTime.Now;
            var strBuild = new StringBuilder()
                .AppendLine()
                .AppendLine(string.Format(@"Распараллеливание начато в {0} ({1} потоков)", timeNow.ToLongTimeString(), threadsParams.Count));

            strBuild.AppendLine(string.Format(@"Расчетное время - {0} часов {1} минут {2} секунд. Всего {3} тестов", timeMax.Hours, timeMax.Minutes, timeMax.Seconds, _testsInfo.Count))
                    .AppendLine(string.Format(@"Расчетное время завершения - {0}", timeNow.Add(timeMax).ToLongTimeString()));
            
            strBuild.AppendLine();
            Console.WriteLine(strBuild.ToString());

            var taskList = new List<Task>();
            foreach (var parameters in threadsParams)
            {
                var key = parameters.Key;
                webDrivers[key] = new ManyInOne();
                webDrivers[key].SetUpGlobalParam(parameters.Value);

                taskList.Add(Task.Run(() =>
                {
                    try
                    {
                        webDrivers[key].AllTests(GetTestInfo, hubHolder.Nodes.Count == 0 ? 0 : key / hubHolder.Nodes.Count * 2);
                    }
                    catch (Exception e)
                    {
                        var error = string.Format("Поток с номером '{0}' завершился с ошибкой ({1}):{2}{3}{2}{4}{2}{2}", key, DateTime.Now.ToLongTimeString(), Environment.NewLine, e.GetBaseException().GetAllMessages(), e.GetBaseException().GetStackTrace());
                        ErrorInfo.AddErrorText("Parallel", error, ErrorType.Exception);
                        Console.WriteLine(error);
                    }
                }));
            }

            foreach (var task in taskList)
                task.Wait();

            if (Param.ParallelGuid.HasValue) 
                EndParallelLog(Param.ParallelGuid.Value, DateTime.Now);

            ErrorInfo.PrintErrorsAndResult(saveDir);

            SendResultToMailAndGoogle(saveDir);

            if(!ErrorInfo.IsHaveNoError())
                ThrowException();
        }

        public virtual void SendResultToMailAndGoogle(string saveDir)
        {
            ErrorInfo.SaveMailResult();
            ErrorInfo.SaveGuidResult(_testsInfo, saveDir);
        }

        public virtual void DefaultMailAndFolder(string saveDir)
        {
            MailCommands.Action.SweepMail();
            MailCommands.Google.SweepMail();
        }

        public virtual void StartParallelLog(string address, DateTime timeStart, string screenPath, int testsCount)
        {
            Param.ParallelGuid = _mysql.AddParallelLog(address, timeStart, screenPath, testsCount);
        }

        public virtual void EndParallelLog(Guid id, DateTime timeEnd)
        {
            _mysql.SetParallelLogTimeEnd(id, timeEnd);
        }

        public virtual void ThrowException()
        {
            
        }

        public void SetAddress(string address)
        {
            Param.Address = address;
        }

        private void PrepareTestsArrays(IEnumerable<ParametersProject> projectsInfo)
        {
            foreach (var project in projectsInfo)
                _testsInfo = _testsInfo.Concat(PathCommands.GetTests(project)).ToList();
            //запуск по заранее подготовленному списку
            //_testsInfo = PathCommands.GetTests(new List<string> { "626" });
            //запуск упавших тестов по ID прогона (parallelId)
            //_testsInfo = PathCommands.GetTests(_mysql.GetFailedTest(Guid.Parse("")));
            //запуск тестов по owner'у
            //_testsInfo = _testsInfo.Where(t => t.Attr.Owner == ProjectOwners.Bogach).ToList();

            _testsInfo = _testsInfo.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private int GetMaxTime(int threadCount, int hubNumber)
        {
            var time = new Dictionary<int, double>();

            for (var i = 0; i < threadCount; i++)
                time[i] = 15 + (hubNumber == 0 ? 0 : i / hubNumber) * 30;

            var tests = _mysql.GetTestDate().Where(t => _testsInfo.Any(k => k.Attr.Id == t.Key)).ToList();

            foreach (var test in tests)
            {
                var key = time.Aggregate((a, b) => a.Value < b.Value ? a : b).Key;
                time[key] += test.Value;
            }

            return (int) time.Max(t => t.Value);
        }

        private static readonly object LockObj = new object();
        private bool GetTestInfo(out TestInfo testInfo)
        {
            lock (LockObj)
            {
                if (_testsInfo.Any(t => !t.IsBeenRunned))
                {
                    testInfo = _testsInfo.First(t => !t.IsBeenRunned);
                    testInfo.IsBeenRunned = true;
                    return true;
                }

                testInfo = null;
                return false;
            }
        }
    }

    public sealed class GuidsResult
    {
        [XmlAttribute]
        public Guid Guid;
        [XmlAttribute]
        public string Value;
    }

    public class LoginInfo
    {
        public string Login;
        public string Password;
    }
}
