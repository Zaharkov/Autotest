using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AutoTest.Properties;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Cookie = OpenQA.Selenium.Cookie;
using AutoTest.helpers.Parameters;

namespace AutoTest.helpers.Selenium
{
    /// <summary>
    /// Класс функциональности тестирования
    /// </summary>
    public class SeleniumCommands
    {
        /// <summary>
        /// Выгруженный элемент для цепочек
        /// </summary>
        private SelElement _element;
        /// <summary>
        /// Драйвер Selenium
        /// </summary>
        private RemoteWebDriver _driver;

        /// <summary>
        /// Различные параметры текущей сессии
        /// </summary>
        private SessionParam _sessionParam = new SessionParam();
        /// <summary>
        /// Различные параметры текущей комманды
        /// </summary>
        private CommandParam _commandParam = new CommandParam();

        /// <summary>
        /// Параметры тестов
        /// </summary>
        public ParametersRead Param = ParametersRead.Instance();

        /// <summary>
        /// Параметры тестов
        /// </summary>
        public ParametersInit ParamInit;
        /// <summary>
        /// Подключение к БД
        /// </summary>
        public SqlCommands Mysql;
        /// <summary>
        /// Использование Http запросов
        /// </summary>
        public PostCommands Post;

        private string _fileSaveDir;
        public string FileSaveDir
        {
            get
            {
                if (_fileSaveDir != null)
                    return _fileSaveDir;

                _fileSaveDir = PathCommands.SharedFolder + "Download\\" + Guid.NewGuid() + "\\";
                PathCommands.CreateDir(_fileSaveDir);
                return _fileSaveDir;  
            }
            set { _fileSaveDir = value; }
        }

        /// <summary>
        /// Задать различные глобальные параметры
        /// </summary>
        /// <param name="paramInit">Параметры тестов</param>
        /// <param name="driver">Драйвер Selenium</param>
        /// <param name="logoutHref">Ссылка на выйти</param>
        public void SetUpGlobalParam(ParametersInit paramInit = null, RemoteWebDriver driver = null, SelElement logoutHref = null)
        {
            _driver = driver ?? _driver;
            _sessionParam.LogoutHref = logoutHref ?? _sessionParam.LogoutHref;

            ParamInit = paramInit ?? (ParamInit ?? new ParametersInit());

            Mysql = Mysql ?? new SqlCommands(this);
            Post = Post ?? new PostCommands(ParamInit, this);
        }

        private void AddErrorFromCatch(Exception e)
        {
            var text = ErrorInfo.AddErrorFromCatch(e, PathCommands.GetProjectDir(), ParamInit.LogSaveDir, GetDateForDebug());
            AddErrorLog(text, ErrorType.CatchEx, null, DateTime.Now, null, null, true, null, new List<Guid>());
        }

        /// <summary>
        /// Инициализация драйвера
        /// </summary>
        private void SetUpDriver(bool isNeed = false)
        {
            if(!isNeed)
                _sessionParam.TryGetDriver++;

            if (_sessionParam.TryGetDriver < 9)
            {
                // Запуск удаленного драйвера
                if (ParamInit.RemoteWd)
                {
                    try
                    {
                        var url = new Uri("http://" + ParamInit.Host + ":" + ParamInit.Port + "/wd/hub");
                        var time = new TimeSpan(0, 0, ParametersInit.WebDriverTimeOut);

                        if (!string.IsNullOrEmpty(ParametersInit.GetLocalConfigValue("Reuse", true)) && !ParamInit.Parallel)
                        {
                            string sessionId;
                            DesiredCapabilities capabilities;
                            new PostCommands(ParamInit).GetWdSessionId(ParamInit.Host, ParamInit.Port, out sessionId, out capabilities);

                            _driver = sessionId != null
                                ? new RemoteWebDriver(url, sessionId, capabilities, time)
                                : new RemoteWebDriver(url, PrepareCapabilities(), time);
                        }
                        else 
                            _driver = new RemoteWebDriver(url, PrepareCapabilities(), time);
                    }
                    catch (WebDriverTimeoutException e)
                    {
                        AddErrorFromCatch(e);
                        ReloadBrowser();
                    }
                    catch (WebDriverException e)
                    {
                        AddErrorFromCatch(e);

                        if (!ParamInit.Parallel && e.Message.Contains("Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение"))
                        {
                            SeleniumProcess.PrepareProcess(ParamInit.Host);
                            SetUpDriver();
                        }
                        else if (e.Message.Contains("timed out after"))
                            ReloadBrowser();
                        else if (e.Message.Contains("за требуемое время не получен нужный отклик"))
                            throw FailingTest("Удаленный компьютер выключен", e);
                        else
                            throw FailingTest("Инициализация сессии провалилась", e);
                    }
                    catch (Exception e)
                    {
                        AddErrorFromCatch(e);
                        throw FailingTest("Инициализация сессии провалилась", e);
                    }
                }
                    // Запуск внутреннего драйвера на текущей машине
                else
                {
                    _driver = new FirefoxDriver(PrepareCapabilities());
                }

                _driver.Manage().Timeouts().SetScriptTimeout(new TimeSpan(0, 0, ParametersInit.AjaxTimeOut));
                _driver.Manage().Timeouts().SetPageLoadTimeout(new TimeSpan(0, 0, ParametersInit.WebDriverTimeOut));
                _driver.Manage().Window.Maximize();
            }
            else
                throw FailingTest("Инициализация сессии провалилась после " + _sessionParam.TryGetDriver + " попыток");
        }

        /// <summary>
        /// Подготовка профиля Firefox и параметров драйвера
        /// </summary>
        /// <returns></returns>
        private DesiredCapabilities PrepareCapabilities()
        {
            var fp = new FirefoxProfile(PathCommands.SharedFiles + @"ProfileFF", false);
            fp.SetPreference("browser.download.dir", ParamInit.RemoteWd ? "\\\\" + FileSaveDir : FileSaveDir);

            var capabilities = DesiredCapabilities.Firefox();
            capabilities.SetCapability("firefox_profile", fp.ToBase64String());
            capabilities.SetCapability("noFocusLib", true);
            capabilities.SetCapability("nativeEvents", false);
            capabilities.SetCapability("applicationCacheEnabled", true);
            return capabilities;
        }

        public TimeSpan Timeout
        {
            set
            {
                _driver.Timeout = value;
            }
            get
            {
                return _driver.Timeout;
            }
        }

        /// <summary>
        /// Получить экземпляр текущего драйвера
        /// </summary>
        /// <returns></returns>
        public RemoteWebDriver GetDriver()
        {
            return _driver;
        }

        /// <summary>
        /// Инициализация функционала тестирования
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SetUpGlobalParam();

            if (_driver == null)
                SetUpDriver();
        }

        /// <summary>
        /// Прибераемся перед началом теста
        /// </summary>
        public void Start()
        {
            var info = GetBackTraceInfo();
            _sessionParam.CurrentTestInfo = PathCommands.GetTest(info.ClassName);
            var name = _sessionParam.CurrentTestInfo.Number + "_" + info.MethodName;

            var saveDir = ParamInit.LogSaveDir + "screens\\";
            PathCommands.DelTree(saveDir + name, true);
            PathCommands.DelLikeFiles(saveDir + "all_failed", name);
            PathCommands.DelLikeFiles(saveDir + "bugs", name);

            _sessionParam.SetTimeoutCounter = 0;
            _sessionParam.SetIntervalCounter = 0;

            var text = "Тест начат в " + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second + Environment.NewLine;
            _sessionParam.Time.Restart();
            if (!ParamInit.Parallel)
                Console.WriteLine(text);

            _commandParam.Default();
        }

        protected void SetCurrentTestInfo(TestInfo testinfo)
        {
            _sessionParam.CurrentTestInfo = testinfo;
        }

        /// <summary>
        /// Перейти по URL
        /// </summary>
        /// <param name="url">Адрес, если null то Param.Address</param>
        public void Url(string url = null)
        {
            var time = new Stopwatch();
            time.Start();

            url = url ?? ParamInit.Address;

            _driver.Navigate().GoToUrl(url);

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            WaitForPageToLoad(_commandParam.Ajax);
            WaitForAjax();

            _commandParam.Default();
        }

        /// <summary>
        /// Залогинится на сайте
        /// </summary>
        /// <param name="name">Логин</param>
        /// <param name="pass">Пароль</param>
        public void Login(string name, string pass)
        {
            Url(ParamInit.Address);
            
            Click(Param.SiteIn);
            SendKeys(ParamInit.Login.SetValue(name));
            SendKeys(ParamInit.Password.SetValue(pass));
            Wait().Click(Param.In);

            _sessionParam.LogoutHref = Get(Param.SiteExit, "href");
        }

        /// <summary>
        /// Задать ссылку разлогинивания с сайта
        /// </summary>
        /// <param name="href">Ссылка</param>
        public void SetLogoutHref(SelElement href)
        {
            _sessionParam.LogoutHref = href;
        }

        /// <summary>
        /// Получить ссылку разлогинивания с сайта
        /// </summary>
        public SelElement GetLogoutHref()
        {
            return _sessionParam.LogoutHref;
        }

        /// <summary>
        /// Получить элемент //body
        /// </summary>
        /// <returns></returns>
        public IWebElement GetHtmElement()
        {
            return _sessionParam.Html;
        }

        /// <summary>
        /// Получить кукки
        /// </summary>
        /// <param name="name">Имя кукки</param>
        /// <param name="returnEx"></param>
        /// <returns></returns>
        public string GetCookie(string name, bool returnEx = true)
        {
            foreach (var cookie in _driver.Manage().Cookies.AllCookies)
            {
                if (cookie.Name == name)
                    return cookie.Value;
            }

            if (returnEx)
                throw FailingTest("Получение cookies не удалось", "Не найдена cookies с именем '" + name + "'");
            
            return null;
        }

        /// <summary>
        /// Добавить кукку
        /// </summary>
        /// <param name="name">Имя кукки</param>
        /// <param name="value">Значение кукки</param>
        /// <param name="domain">домен</param>
        /// <returns></returns>
        public void SetCookie(string name, string value, string domain = null)
        {
            var cookie = domain == null
                ? new Cookie(name, value)
                : new Cookie(name, value, domain, "/", DateTime.Now.AddDays(1));

            _driver.Manage().Cookies.AddCookie(cookie);
        }

        /// <summary>
        /// Нажать "назад" в браузере
        /// </summary>
        public void Back()
        {
            _driver.Navigate().Back();
            Refresh();
        }

        /// <summary>
        /// Обновить страницу
        /// </summary>
        public void Refresh()
        {
            _driver.Navigate().Refresh();

            if (_commandParam.AlertNot || _commandParam.AlertOk)
            {
                IAlert alert;
                try
                {
                    alert = _driver.SwitchTo().Alert();
                }
                catch (Exception)
                {
                    alert = null;
                }

                if (alert == null)
                {
                    if (!_commandParam.NoDebug)
                        Error("Не был найден алерт после рефреша", ErrorType.Typo);
                }
                else if (_commandParam.AlertNot)
                    alert.Dismiss();
                else
                    alert.Accept();
            }

            WaitForPageToLoad();
            WaitForAjax();
        }

        /// <summary>
        /// Получить текущий URL страницы
        /// </summary>
        /// <returns></returns>
        public string GetLocation()
        {
            return _driver.Url;
        }

        /// <summary>
        /// Нажать на кнопку с клавиатуры
        /// </summary>
        /// <param name="key">Название кнопки через Keys</param>
        public void DoKey(string key)
        {
            var action = new Actions(_driver);
            action.SendKeys(key).Perform();
            WaitForAjax();
        }

        /// <summary>
        /// Инициализация завершения теста
        /// и остановка браузера и драйвера
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (!ParamInit.Parallel)
            {
                ErrorInfo.PrintErrorsTest(ParamInit.LogSaveDir);
                var text = Environment.NewLine + "Тест закончен в " + DateTime.Now.ToShortTimeString() + ":" + DateTime.Now.Second;
                Console.WriteLine(text);
                Console.WriteLine(_sessionParam.Time.Elapsed.TotalSeconds);
            }

            if (_driver != null && (string.IsNullOrEmpty(ParametersInit.GetLocalConfigValue("Reuse", true)) || ParamInit.Parallel))
                _driver.Quit();
        }

        public void ErrorElement(string text, ErrorType typo)
        {
            text += _element == null ? "" : " в элементе '" + _element.Link + "'";
            Error(text, typo);
        }

        /// <summary>
        /// Запись ошибки тестирования
        /// </summary>
        /// <param name="text">Текст ошибки</param>
        /// <param name="typo">Тип ошибки</param>
        public void Error(string text, ErrorType typo)
        {
            var info = GetBackTraceInfo();

            List<Guid> failGuids;
            if (!typo.HasFlag(ErrorType.Timed) && !typo.HasFlag(ErrorType.TestFunc))
                failGuids = ErrorInfo.GuidCheck(info.FullName);
            else
                failGuids = new List<Guid>();

            if (text != null)
            {
                var errorText = text + Environment.NewLine + "Имя теста: " + info.FullName + ", строка в тесте: " + info.Line +
                                ", время: " + GetDateForDebug();

                if (info.Bug != null)
                {
                    errorText += Environment.NewLine + "На данной строке существует BUG - '" + info.Bug + "'";
                    typo |= ErrorType.Bug;
                }

                if (typo.HasFlag(ErrorType.Failed) || typo.HasFlag(ErrorType.Exception))
                {
                    text += Environment.NewLine + "Url: " + GetLocation();
                    errorText += Environment.NewLine + "Url: " + GetLocation();
                }

                ErrorInfo.AddErrorText(info.FullName, errorText + ErrorInfo.FailGuidsToStr(failGuids), typo);

                if (!typo.HasFlag(ErrorType.Timed) && !typo.HasFlag(ErrorType.NoScreen) && !typo.HasFlag(ErrorType.TestFunc))
                {
                    var screenDir = ParamInit.LogSaveDir + "screens\\";

                    var dirName = _sessionParam.CurrentTestInfo.Number + "_" + info.MethodName;
                    string screenPath;

                    if (!ParamInit.Parallel)
                        screenPath = screenDir + dirName + "\\line-" + info.Line +
                                     (typo.HasFlag(ErrorType.Failed) ? "-failed" : "") +
                                     (info.Bug != null ? "(have bug)" : "") + ".png";
                    else
                    {
                        if (info.Bug == null)
                            screenPath = screenDir + dirName + "\\line-" + info.Line +
                                         (typo.HasFlag(ErrorType.Failed) ? "-failed" : "") + ".png";
                        else
                            screenPath = screenDir + "bugs\\" + dirName + "-line-" + info.Line +
                                         (typo.HasFlag(ErrorType.Failed) ? "-failed" : "") + "(have bug).png";

                    }

                    GetScreenshot(screenPath);

                    AddErrorLog(text, typo, info.Line, DateTime.Now, info.Bug, screenPath.Replace(screenDir, ""),
                        info.Bug != null, null, failGuids);

                    if (typo.HasFlag(ErrorType.Failed) && info.Bug == null)
                    {
                        screenPath = screenDir + "all_failed\\" + dirName + "-line-" + info.Line + ".png";
                        GetScreenshot(screenPath);
                    }
                }
                else
                    AddErrorLog(text, typo, info.Line, DateTime.Now, null, null, true, null, failGuids);
            }
        }

        public void AddErrorLog(string text, ErrorType type, int? line, DateTime time, string bug, 
            string screenPath, bool isChecked, Guid? testId = null, List<Guid> guidError = null)
        {
            if (ParamInit.ParallelGuid.HasValue && _sessionParam.CurrentTestInfo != null)
            {
                Mysql.AddErrorLog(testId ?? _sessionParam.CurrentTestInfo.Attr.Id, ParamInit.ParallelGuid.Value, text, type.ToString(), 
                    line, time, bug, screenPath, isChecked, ErrorInfo.FailGuidsToHtml(guidError));
            }
        }

        /// <summary>
        /// Начать проверку требования из чек-листа
        /// </summary>
        /// <param name="guid">GUID требования</param>
        public void GuidStart(string guid)
        {
            var info = GetBackTraceInfo();
            var error = ErrorInfo.GuidStart(Guid.Parse(guid), info.FullName);

            if (error != null)
                Error(error, ErrorType.TestFunc);
        }

        /// <summary>
        /// Закончить проверку требования из чек-листа
        /// </summary>
        /// <param name="guid">GUID проверки, или null - тогда будут закончены все начаты проверки</param>
        public void GuidEnd(string guid = null)
        {
            var info = GetBackTraceInfo();
            var error = guid == null
                ? ErrorInfo.GuidEnd(info.FullName)
                : ErrorInfo.GuidEnd(Guid.Parse(guid), info.FullName);

            if(error != null)
                Error(error, ErrorType.TestFunc);
        }

        /// <summary>
        /// Дата для вывода ошибок
        /// </summary>
        /// <returns></returns>
        protected static string GetDateForDebug()
        {
            var datetime = DateTime.Now;
            return datetime.ToShortDateString() + " " + datetime.ToShortTimeString() + ":" + (datetime.Second > 9 ? datetime.Second.ToString() : "0" + datetime.Second);
        }

        /// <summary>
        /// Получить информацию об ошибке
        /// </summary>
        /// <returns></returns>
        private BackTraceInfo GetBackTraceInfo()
        {
            var stackTraceAll = new StackTrace(true).GetFrames();
            if(stackTraceAll == null)
                throw new NullReferenceException("Стек вызовов пустой");

            var method = stackTraceAll.First(t => t.GetMethod().IsDefined(typeof(TestAttribute)));
            var classObj = method.GetMethod().DeclaringType;
            if(classObj == null)
                throw new NullReferenceException("Класс объекта вызова пустой");

            var className = classObj.Name;
            var methodName = method.GetMethod().Name;
            var lineNumber = method.GetFileLineNumber();
            var bug = _sessionParam.CurrentTestInfo != null ?
                _sessionParam.CurrentTestInfo.GetBug(lineNumber) : null;

            return new BackTraceInfo(className, methodName, lineNumber, bug);
        }

        /// <summary>
        /// Закончить тест с критической ошибкой
        /// </summary>
        /// <param name="failText">Текст ошибки</param>
        /// <param name="errorText">Дополнительная информация об ошибке</param>
        /// <param name="reloadBrowser">Перезагружать ли браузер</param>
        /// <param name="type">Тип ошибки</param>
        /// <param name="e"></param>
        /// <returns></returns>
        public SeleniumFailException FailingTest(string failText, string errorText = null, bool reloadBrowser = false, ErrorType type = ErrorType.Failed, Exception e = null)
        {
            Error(errorText, type);

            if (reloadBrowser)
                ReloadBrowser();

            throw new SeleniumFailException(failText, e);
        }

        public SeleniumFailException FailingElement(string failText, string errorText)
        {
            ErrorElement(errorText, ErrorType.Failed);
            throw new SeleniumFailException(failText);
        }

        public SeleniumFailException FailingTest(string failText, Exception e)
        {
            return FailingTest(failText, null, false, ErrorType.Failed, e);
        }

        public SeleniumFailException FailingTest(string failText, string errorText, Exception e)
        {
            return FailingTest(failText, errorText, false, ErrorType.Failed, e);
        }

        /// <summary>
        /// Получить скриншот
        /// </summary>
        /// <param name="path">Путь к сохранению скрина</param>
        private void GetScreenshot(string path)
        {
            try
            {
                var screen = _driver.GetScreenshot();
                PathCommands.CreateDir(path, true);
                screen.SaveAsFile(path, ImageFormat.Png);
            }
            catch (Exception e)
            {
                AddErrorFromCatch(e);
                throw FailingTest("Попытка сделать скрин провалилась", null, true, ErrorType.NoScreen, e);
            }
        }

        /// <summary>
        /// Перезагрузка браузера и драйвера
        /// </summary>
        public void ReloadBrowser(bool isNeed = false)
        {
            try
            {
                if(_driver != null)
                    _driver.Quit();
            }
            catch (WebDriverException e)
            {
                AddErrorFromCatch(e);

                if (ParamInit.RemoteWd)
                    Thread.Sleep(ParametersInit.WebDriverTimeOut * 1000 + 1);
            }

            _element = null;
            _sessionParam.Html = null;
            _driver = null;

            SetUpDriver(isNeed);
        }

        /// <summary>
        /// Ожидание завершения загрузки страницы
        /// </summary>
        /// <param name="time"></param>
        private void WaitForPageToLoad(int time = 0)
        {
            _element = null;
            _sessionParam.SetTimeoutMaxKey = 0;
            _sessionParam.SetIntervalMaxKey = 0;
            _sessionParam.Ajax = 0;

            WaitForAjax(time);
            LoadHtml();

            FindException();
        }

        /// <summary>
        /// Тип таймаута js
        /// </summary>
        private enum TimeoutType
        {
            SetTimeOut,
            SetInterval
        }

        /// <summary>
        /// Ожидание завершения Ajax запросов
        /// </summary>
        /// <param name="time">Время ожидания</param>
        private void WaitForAjax(int time = 0)
        {
            if (time < ParametersInit.AjaxTimeOut)
                time = ParametersInit.AjaxTimeOut;

            var getTime = new Stopwatch();
            getTime.Start();

            var oldAjax = _sessionParam.Ajax;
            int returnAjax;
            do
            {
                var returnArray = ExecuteJs(
                    Resources.getOffSet + Environment.NewLine + 
                    Resources.GetAjax, new object[] { _sessionParam.SetTimeoutMaxKey, _sessionParam.SetIntervalMaxKey }) as Dictionary<string, object>;

                if (returnArray == null)
                    throw FailingTest("GetAjax вернул бяку");

                var setTimeouts1 = returnArray["SetTimeouts"] as Dictionary<string, object>;
                var setTimeouts2 = returnArray["SetIntervals"] as Dictionary<string, object>;
                var firstTimeNow = Int64.Parse(returnArray["TimeNow"].ToString());
                var secondTimeNow = WaitTimeouts(setTimeouts1, firstTimeNow, TimeoutType.SetTimeOut);
                secondTimeNow = WaitTimeouts(setTimeouts2, secondTimeNow, TimeoutType.SetInterval);

                returnAjax = int.Parse(returnArray["Ajax"].ToString());

                if (firstTimeNow != secondTimeNow)
                    returnAjax++;
            }
            while (returnAjax != _sessionParam.Ajax && getTime.Elapsed.TotalSeconds < time);
           
            _sessionParam.Ajax = returnAjax;

            if (getTime.Elapsed.TotalSeconds > ParametersInit.TimeOutForLog)
                Error("Внимание! Загрузка страницы превысила " + ParametersInit.TimeOutForLog + " секунд : " + getTime.Elapsed.TotalSeconds
                    + " (" + _sessionParam.Ajax + ")", ErrorType.Timed);
            else if (_sessionParam.Ajax > 0 && _sessionParam.Ajax != oldAjax)
                Error("Повисший Ajax (" + _sessionParam.Ajax + ")", ErrorType.Timed);

            if (getTime.Elapsed.TotalSeconds > (time > ParametersInit.WebDriverTimeOut ? time : ParametersInit.WebDriverTimeOut))
                throw FailingTest("Слишком долго грузится страница", null, true);
        }

        private Int64 WaitTimeouts(Dictionary<string, object> setTimeoutsArray, Int64 timeNow, TimeoutType type)
        {
            var ignoredTimeouts = new Dictionary<Int64, string[]>();

            //var calcDelay = new Dictionary<Int64, Dictionary<string, object>>();

            setTimeoutsArray = setTimeoutsArray.OrderBy(t => Int64.Parse(t.Key)).ToDictionary(t => t.Key, t => t.Value);

            foreach (var setTimeout in setTimeoutsArray)
            {
                if (type == TimeoutType.SetTimeOut)
                    _sessionParam.SetTimeoutMaxKey = Int64.Parse(setTimeout.Key);
                else
                    _sessionParam.SetIntervalMaxKey = Int64.Parse(setTimeout.Key);

                var value = setTimeout.Value as Dictionary<string, object>;

                if (value == null)
                    throw new NullReferenceException("Приведение типов накрылось");

                Int64 delay;
                var result = Int64.TryParse(Regex.Replace(value["delay"].ToString(), ",\\d+", ""), out delay);

                if (!result)
                    continue;

                if (delay < 3500)
                {
                    if (ignoredTimeouts.ContainsKey(delay))
                    {
                        var ignore = ignoredTimeouts[delay].Any(t => value["cb"].ToString().Contains(t));

                        if (ignore)
                            continue;
                    }

                    var date1 = new TimeSpan(Int64.Parse(value["calltime"].ToString()));
                    var date2 = new TimeSpan(timeNow);

                    var diff = date1.Ticks + delay - date2.Ticks;
                    if (diff > 0)
                    {
                        //calcDelay[diff] = value;
                        Thread.Sleep((int)(diff * 1.1));

                        if (type == TimeoutType.SetTimeOut)
                            _sessionParam.SetTimeoutCounter++;
                        else
                            _sessionParam.SetIntervalCounter++;

                        timeNow += diff;
                    }

                    if (delay == 0)
                    {
                        Thread.Sleep(100);
                        timeNow += 100;
                    }
                }
            }

            return timeNow;
        }

        /// <summary>
        /// Ожидание пока элемент станет видимым
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="time">Время ожидания</param>
        private static bool WaitForVisible(IWebElement element, int time = 5)
        {
            var getTime = new Stopwatch();
            getTime.Start();

            var visible = element.Displayed;
            while (!visible && getTime.Elapsed.TotalSeconds < time)
            {
                Thread.Sleep(10);
                visible = element.Displayed;
            }

            return visible;
        }

        /// <summary>
        /// Скрол к элементу
        /// </summary>
        /// <param name="element">элемент</param>
        private void ScrollToView(IWebElement element)
        {
            if(!_commandParam.NoScroll)
                ExecuteJs(Resources.getOffSet + Environment.NewLine + Resources.scrollToView, new object[] { element, _sessionParam.WindowY });
        }

        /// <summary>
        /// Поиск эксепшенов на странице
        /// </summary>
        private void FindException()
        {
            var html = TryGetHtml();
            var error1 = html.Contains("Server Error in '/' Application");
            var error3 = html.Contains("Ошибка сервера в приложении");
            var error404 = html.Contains("Страница не найдена.");

            if (error1 || error3)
                Error("Внимание!!! Найден эксепшен =(" + Environment.NewLine, ErrorType.Exception);

            if (error404 && ParamInit.Check404)
                Error("Внимание!!! Найдена ошибка 404 =(", ErrorType.Exception);
        }

        private string TryGetHtml()
        {
            try
            {
                return _sessionParam.Html.Text;
            }
            catch (StaleElementReferenceException)
            {
                LoadHtml();
                return _sessionParam.Html.Text;
            }
        }

        private void LoadHtml()
        {
            var isNotGood = true;
            while (isNotGood)
            {
                try
                {
                    NotChain();
                    NoScroll();
                    _sessionParam.Html = IsPresentOrFall("//body").Element;
                    _sessionParam.WindowY = _sessionParam.Html.Size.Height/2;
                    isNotGood = false;
                }
                catch (StaleElementReferenceException)
                {

                }
                finally
                {
                    NotChain(false);
                    NoScroll(false);
                }
            }
        }

        /// <summary>
        /// Выполнить код javascript
        /// </summary>
        /// <param name="script">Скрипт</param>
        /// <param name="args">Параметры</param>
        /// <returns></returns>
        private object ExecuteJs(string script, object[] args = null)
        {
            try
            {
                return _driver.ExecuteScript(script, args ?? new object[] { });
            }
            catch (Exception e)
            {
                AddErrorFromCatch(e);
                throw FailingTest("Выполнение js скрипта провалилось", null, true, ErrorType.Failed, e);
            }
        }

        /// <summary>
        /// Выгрузка элемента
        /// с проверкой его существования и видимости
        /// </summary>
        /// <param name="link">Xpath</param>
        /// <param name="isDisabled">Проверять ли на задизейбленность</param>
        /// <returns></returns>
        private SelElement IsPresentOrFall(string link, bool isDisabled = true)
        {
            IWebElement element;

            if (_commandParam.Visible)
            {
                link = ParametersFunctions.DefaultXPathCount(link);
                var el = GetOnlyOneVisible(link);
                element = el.Element;
                link = el.Link;
            }
            else
                element = GetElement(link, ParametersInit.FindElementTimeOut);

            if (element == null)
            {
                throw FailingElement("Необходимый элемент не найден", "Элемент : '" + link + "' не найден");
            }

            ScrollToView(element);

            var visible = WaitForVisible(element);

            if (!visible)
                throw FailingElement("Необходимый элемент невидимый", "Элемент : '" + link + "' есть, но невидим");

            if (!isDisabled) 
                return new SelElement(element, link);
            
            if (!element.Enabled)
                throw FailingElement("Необходимый элемент заблокирован", "Элемент : '" + link + "' есть, но не активен");

            return new SelElement(element, link);
        }

        /// <summary>
        /// Получить единственный видимый элемент на странице 
        /// </summary>
        /// <param name="link">Xpath</param>
        /// <param name="returnNull">В случае отсутствия элемента вернуть null</param>
        /// <returns></returns>
        private SelElement GetOnlyOneVisible(string link, bool returnNull = false)
        {
            var elements = GetElements(link);
            var count = elements != null ? elements.Count : 0;
            var newlink = link;
            var newElem = elements != null ? (elements.Any() ? elements.First() : null) : null;

            if (elements == null)
            {
                if (!returnNull)
                    throw FailingElement("Необходимый элемент не найден", "Число элементов : '" + link + "' равно нулю");
                
                return null;
            }

            if (elements.Any())
            {
                var i = 1;
                var visibleCount = 0;

                while (i < count + 1)
                {
                    var element = elements[i - 1];

                    if (element == null)
                    {
                        if (!returnNull)
                            throw FailingElement("Необходимый элемент не найден", "Элемент : '" + newlink + "' не найден");
                        
                        return null;
                    }
                    
                    if (element.Displayed)
                    {
                        newElem = element;
                        newlink = ParametersFunctions.GetXPathCount(link, i);
                        visibleCount++;
                    }

                    if (visibleCount == 0 && i == count)
                    {
                        if (!returnNull)
                            throw FailingElement("Необходимые элементы невидимые", "Все найденые элементы : '" + link + "' невидимые");
                        
                        return null;
                    }
                    
                    if (visibleCount > 1)
                    {
                        if (!returnNull)
                            throw FailingElement("Необходимых элементов более одного", "Найдено более одного видимого элемента : '" + link + "'");
                        
                        return null;
                    }

                    i++;
                }
            }
            else
            {
                if (!returnNull)
                    throw FailingElement("Необходимый элемент не найден", "Элемент : '" + newlink + "' не найден");
                
                return null;
            }

            return new SelElement(newElem, newlink);
        }

        /// <summary>
        /// Поиск элемента драйвера
        /// </summary>
        /// <param name="xpath">Xpath</param>
        /// <param name="time">Время поиска</param>
        /// <returns></returns>
        private IWebElement GetElement(string xpath, int time = 0)
        {
            IWebElement el;
            try
            {
                var wait = new WebDriverWait(_driver, new TimeSpan(0, 0, time));

                if (_element != null && !_commandParam.NotChain)
                    xpath = xpath.StartsWith("(") ? "(" + _element.Link + xpath.Substring(1) : _element.Link + xpath;
                
                el = wait.Until(driver => driver.FindElement(By.XPath(xpath)));
            }
            catch (WebDriverException)
            {
                el = null;
            }

            _sessionParam.LastGetElement = DateTime.Now;
            return el;
        }

        /// <summary>
        /// Поиск элементов драйвера
        /// </summary>
        /// <param name="xpath">Xpath</param>
        /// <param name="time">Время поиска</param>
        /// <returns></returns>
        private ReadOnlyCollection<IWebElement> GetElements(string xpath, int time = 0)
        {
            ReadOnlyCollection<IWebElement> el;
            try
            {
                var wait = new WebDriverWait(_driver, new TimeSpan(0, 0, time));

                if (_element != null && !_commandParam.NotChain)
                    xpath = xpath.StartsWith("(") ? "(" + _element.Link + xpath.Substring(1) : _element.Link + xpath;

                el = wait.Until(driver => driver.FindElements(By.XPath(xpath)));
            }
            catch (WebDriverException)
            {
                el = null;
            }

            _sessionParam.LastGetElement = DateTime.Now;
            return el;
        }

        /// <summary>
        /// "Тронуть" драйвер для избежания
        /// ошибки таймаута бездействия
        /// </summary>
        /// <returns></returns>
        public bool TouchWebDriver()
        {
            try
            {
                if (DateTime.Now.Subtract(_sessionParam.LastGetElement).TotalSeconds * 4 > ParametersInit.WebDriverTimeOut)
                {
                    TouchMySelf();
                    _sessionParam.LastGetElement = DateTime.Now;
                }
                return false;
            }
            catch (WebDriverException)
            {
                return true;
            }
        }

        private void TouchMySelf()
        {
            _driver.FindElementByXPath("//*[@class='its for crash']");
        }

        /// <summary>
        /// После выполнения команды ждать загрузки страницы
        /// </summary>
        /// <param name="wait"></param>
        /// <returns></returns>
        public SeleniumCommands Wait(bool wait = true)
        {
            _commandParam.Wait = wait;
            return this;
        }

        /// <summary>
        /// Искать только видимые элементы в команде
        /// </summary>
        /// <param name="visible"></param>
        /// <returns></returns>
        public SeleniumCommands Visible(bool visible = true)
        {
            _commandParam.Visible = visible;
            return this;
        }

        /// <summary>
        /// Не выводить дебаг информацию в команде
        /// </summary>
        /// <param name="noDebug"></param>
        /// <returns></returns>
        public SeleniumCommands NoDebug(bool noDebug = true)
        {
            _commandParam.NoDebug = noDebug;
            return this;
        }

        /// <summary>
        /// Не ждать Ajax
        /// </summary>
        /// <param name="notAjax"></param>
        /// <returns></returns>
        public SeleniumCommands NotAjax(bool notAjax = true)
        {
            _commandParam.NotAjax = notAjax;
            return this;
        }

        public SeleniumCommands NoScroll(bool noScroll = true)
        {
            _commandParam.NoScroll = noScroll;
            return this;
        }

        /// <summary>
        /// После выполнения команды нажать на Ок в алерте
        /// </summary>
        /// <param name="alertOk"></param>
        /// <returns></returns>
        public SeleniumCommands AlertOk(bool alertOk = true)
        {
            if(_commandParam.AlertNot)
                throw new ArgumentException("Не может быть одновременно AlertOk и AlertNot");

            _commandParam.AlertOk = alertOk;
            return this;
        }

        /// <summary>
        /// После выполнения команды нажать на Отмену в алерте
        /// </summary>
        /// <param name="alertNot"></param>
        /// <returns></returns>
        public SeleniumCommands AlertNot(bool alertNot = true)
        {
            if (_commandParam.AlertOk)
                throw new ArgumentException("Не может быть одновременно AlertOk и AlertNot");

            _commandParam.AlertNot = alertNot;
            return this;
        }

        /// <summary>
        /// После выполнения команды подождать N секунд
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns></returns>
        public SeleniumCommands Sleep(int time)
        {
            _commandParam.Sleep = time;
            return this;
        }

        /// <summary>
        /// После выполнения команды ждать завершения Ajax N секунд
        /// </summary>
        /// <param name="time">Время ожидания в секундах</param>
        /// <returns></returns>
        public SeleniumCommands Ajax(int time)
        {
            _commandParam.Ajax = time;
            return this;
        }

        public SeleniumCommands NotChain(bool notChain = true)
        {
            _commandParam.NotChain = notChain;
            return this;
        }

        /// <summary>
        /// Переключение на цепочку
        /// (поиск элементов будет относительно заданого)
        /// </summary>
        /// <param name="button">Xpath высшего элемента цепочки</param>
        /// <returns></returns>
        public SeleniumCommands GetChained(ParamButton button)
        {
            var el = IsPresentOrFall(button.Link);

            _commandParam.Default();

            var sel = new SeleniumCommands
            {
                FileSaveDir = FileSaveDir,
                Param = Param,
                ParamInit = ParamInit,
                Mysql = Mysql,
                Post = Post,
                _commandParam = _commandParam,
                _driver = _driver,
                _element = new SelElement(el.Element, _element == null ? el.Link : (el.Link.StartsWith("(") ? "(" + _element.Link + el.Link.Substring(1) : _element.Link + el.Link)),
                _sessionParam = _sessionParam,
            };

            return sel;
        }

        /// <summary>
        /// Кликнуть по элементу-кнопке
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public SelElement Click(ParamButton button)
        {
            var element = IsPresentOrFall(button.Link);

            var time = new Stopwatch();
            time.Start();

            element.Element.Click();

            if (_commandParam.AlertNot || _commandParam.AlertOk)
            {
                IAlert alert;
                try
                {
                    alert = _driver.SwitchTo().Alert();
                }
                catch (Exception)
                {
                    alert = null;
                }

                if (alert == null)
                {
                    if(!_commandParam.NoDebug)
                        Error("Не был найден алерт после клика на '" + element.Link + "'", ErrorType.Typo);
                }
                else if (_commandParam.AlertNot)
                    alert.Dismiss();
                else
                    alert.Accept();
            }

            if(_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            if (_commandParam.Wait)
                WaitForPageToLoad();

            if(!_commandParam.NotAjax)
                WaitForAjax(_commandParam.Ajax);

            _commandParam.Default();

            time.Stop();

            return element;
        }

        /// <summary>
        /// Ввести значение в поле
        /// </summary>
        /// <param name="field">Параметр поля</param>
        /// <returns></returns>
        public SelElement SendKeys(ParamField field)
        {
            if (field.Value == null)
                throw new ArgumentException("Значение поля должно быть задано");

            var element = IsPresentOrFall(field.Field.Link);

            var value = element.Element.GetAttribute("value");

            if (field.Value != value)
            {
                if (!string.IsNullOrEmpty(value))
                    element.Element.Clear();

                element.Element.Click();
                element.Element.SendKeys(field.Value);

                if (_commandParam.Sleep > 0)
                    Thread.Sleep(_commandParam.Sleep * 1000);

                WaitForAjax(_commandParam.Ajax);

                _commandParam.Default();
            }

            return element;
        }

        /// <summary>
        /// Выбрать значение в селекте
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public SelSelect Select(ParamSelect select)
        {
            if (select.Value == null)
                throw new ArgumentException("Значение селекта должно быть задано");

            var comPar = _commandParam.Copy();

            var but = Click(select.But);
            var item = Click(select.ItemValue);

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            if (comPar.Wait)
                WaitForPageToLoad();

            _commandParam.Default();

            return new SelSelect(but, item);
        }

        /// <summary>
        /// Перетащить элемент-кнопку по координатам
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="x">Координата x</param>
        /// <param name="y">Координата y</param>
        public void DragAndDrop(ParamButton button, int x, int y)
        {
            var element = IsPresentOrFall(button.Link);

            var actions = new Actions(_driver);
            actions.DragAndDropToOffset(element.Element, x, y).Perform();

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            WaitForAjax(_commandParam.Ajax);

            if (_commandParam.Wait)
                WaitForPageToLoad();

            _commandParam.Default();
        }

        /// <summary>
        /// Получить аттрибут элемента-кнопки
        /// </summary>
        /// <param name="field">Параметр кнопки</param>
        /// <param name="attr">Название аттрибута</param>
        /// <returns></returns>
        public SelElement Get(ParamButton field, string attr)
        {
            var element = IsPresentOrFall(field.Link, false);
            var attr2 = element.Element.GetAttribute(attr);

            element.Attr = attr2;
            
            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return element;
        }

        public string GetCss(ParamButton button, string style)
        {
            var element = IsPresentOrFall(button.Link, false);
            return element.Element.GetCssValue(style);
        }

        /// <summary>
        /// Получить значение из поля
        /// </summary>
        /// <param name="field">Параметр поля</param>
        /// <returns></returns>
        public string Get(ParamField field)
        {
            return Get(field.Field, "value").Attr;
        }

        /// <summary>
        /// Получить текст элемента-кнопки
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public string Get(ParamButton button)
        {
            var element = IsPresentOrFall(button.Link, false);

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return element.Element.Text.Replace(" ", " ");
        }

        /// <summary>
        /// Получить количество элементов-кнопок
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public int GetCount(ParamButton button)
        {
            var elements = GetElements(button.Link);

            var i = elements == null ? 0 : elements.Count;

            if(elements != null)
            {
                foreach(var element in elements)
                {
                    ScrollToView(element);
                    if(!element.Displayed)
                        i--;
                }
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return i;
        }

        /// <summary>
        /// Получить текст выбранного значения селекта
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public string Get(ParamSelect select)
        {
            var element = IsPresentOrFall(select.But.Link, false);
            return element.Element.Text;
        }

        /// <summary>
        /// Получить значение из таблицы
        /// </summary>
        /// <param name="table">Параметр таблицы</param>
        /// <returns></returns>
        public string Get(ParamTable table)
        {
            var element = IsPresentOrFall(table.XPath);

            return element.Element.Text;
        }

        /// <summary>
        /// Получить все значения из селекта
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public List<string> GetSelectOptions(ParamSelect select)
        {
            var element = GetElement(select.Select.Link);
           

            if (element != null)
            {
                var selectElement = new SelectElement(element);
                var options = new List<string>();

                selectElement.Options.ToList().ForEach(t => options.Add(t.Text));

                return options;
            }

            Click(select.But);
            var elements = GetElements(select.Item.Link);

            if (elements != null)
            {
                var list = elements.Select(el => el.Text).ToList();
                Click(select.But);
                return list;
            }

            throw FailingElement("Необходимый элемент не найден", "Элемент : '" + select.Select.Link + "' не найден");
        }

        /// <summary>
        /// Получить номер Xpath
        /// </summary>
        /// <param name="link">Xpath</param>
        /// <param name="count">Номер видимого</param>
        /// <returns></returns>
        private int GetVisibleLink(string link, int count = 1)
        {
            var elements = GetElements(link);
            var array = new Dictionary<int, bool>();
            var realCount = elements == null ? 0 : elements.Count;

            if (elements != null)
            {
                var i = 1;
                foreach (var element in elements)
                {
                    ScrollToView(element);
                    array[i] = element.Displayed;

                    i++;
                }
            }

            var visibleFound = 0;
            for (var j = 1; j <= realCount; j++)
            {
                if (array[j]) visibleFound++;
                if (visibleFound == count) return j;
            }

            throw FailingElement("Необходимый элемент не найден", "Не найден видимый элемент '" + link + "' по счету '" + count + "'");
        }

        /// <summary>
        /// Получить видимый по счету
        /// </summary>
        /// <param name="param"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public T GetVisible<T>(T param, int count = 1) where T : IParam<T>
        {
            return param.SetCount(GetVisibleLink(param.Link, count));
        }

        private Task _waitForDelete;
        private string GetFolderPath()
        {
            if (_waitForDelete != null) _waitForDelete.Wait();
            return FileSaveDir;
        }

        /// <summary>
        /// Проверка что файл скачался
        /// </summary>
        /// <param name="filename">Имя файла</param>
        /// <returns></returns>
        public bool CheckDownloadFile(string filename)
        {
            filename = Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(filename)));

            var dirHost = GetFolderPath();

            var forReturn = true;
            if(!File.Exists(dirHost + filename))
            {
                if(!_commandParam.NoDebug)
                    Error("Файл: '" + filename + "' не найден", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Удалить файл
        /// </summary>
        /// <param name="filename">Имя файла</param>
        /// <returns></returns>
        public bool DeleteOldFile(string filename)
        {
            filename = Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(filename)));

            var dirHost = GetFolderPath();
            var name = filename.Replace(new FileInfo(dirHost + filename).Extension, "");
            var deleting = PathCommands.DelLikeFiles(dirHost, name);

            if (deleting) _waitForDelete = Task.Run(() =>Thread.Sleep(10000));
            
            return false;
        }

        /// <summary>
        /// Подождать пока файл загрузится
        /// </summary>
        /// <param name="filename">Имя файла</param>
        /// <param name="time">Время ожидания</param>
        /// <returns></returns>
        public bool WaitDownloadFile(string filename, int time = 0)
        {
            var comPar = _commandParam.Copy();
            var way = GetFolderPath();

            var filePath = Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(way + filename)));

            time = time < ParametersInit.AjaxTimeOut ? ParametersInit.AjaxTimeOut : time;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while(stopwatch.Elapsed.TotalSeconds < time)
            {
                if(!File.Exists(filePath))
                    TouchWebDriver();
                else
                    break;
            }

            if(!File.Exists(filePath))
            {
                if(!comPar.NoDebug)
                    Error("Файл '" + filename + "' не начал загружаться в течении " + time + " секунд", ErrorType.Typo);
                
                _commandParam.Default();
                return false;
            }

            stopwatch.Restart();

            while(stopwatch.Elapsed.TotalSeconds < time)
            {
                try
                {
                    File.OpenRead(filePath).Dispose();
                    _commandParam.Default();
                    return true;
                }
                catch (Exception) 
                {
                    TouchWebDriver();
                }
            }

            try
            {
                File.OpenRead(filePath).Dispose();
                _commandParam.Default();
                return true;
            }
            catch (Exception) 
            {
                if (!comPar.NoDebug)
                    Error("Файл '" + filename + "' не загрузился за " + time + " секунд.", ErrorType.Typo);

                _commandParam.Default();
                return false;
            }
        }

        /// <summary>
        /// Кликнуть на кнопку скачивания файла
        /// и подождать его загрузки
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="filename">Имя файла</param>
        /// <returns></returns>
        public bool ClickDownloadFile(ParamButton button, string filename)
        {
            var comPar = _commandParam.Copy();

            DeleteOldFile(filename);
            Wait().Click(button);

            var result = NoDebug(comPar.NoDebug).WaitDownloadFile(filename, comPar.Ajax);
            DeleteOldFile(filename);

            _commandParam.Default();

            return result;
        }

        /// <summary>
        /// Находится ли элемент-кнопка в фокусе ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsFocused(ParamButton button)
        {
            var activeEl = _driver.SwitchTo().ActiveElement();
            var el = IsPresentOrFall(button.Link, false);

            if (!el.Element.Equals(activeEl))
            {
                if(!_commandParam.NoDebug)
                    ErrorElement("Элемент: '" + el.Link + "' не в фокусе", ErrorType.Typo);

                if (_commandParam.Sleep > 0)
                    Thread.Sleep(_commandParam.Sleep * 1000);

                _commandParam.Default();

                return false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return true;
        }

        /// <summary>
        /// Не находится ли элемент-кнопки в фокусе ?
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public bool IsNotFocused(ParamButton button)
        {
            var activeEl = _driver.SwitchTo().ActiveElement();
            var el = IsPresentOrFall(button.Link, false);

            if (el.Element.Equals(activeEl))
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Элемент: '" + el.Link + "' в фокусе", ErrorType.Typo);

                if (_commandParam.Sleep > 0)
                    Thread.Sleep(_commandParam.Sleep * 1000);

                _commandParam.Default();

                return false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return true;
        }

        /// <summary>
        /// Существует ли видимый текст на странице или в элементе-цепочке ?
        /// </summary>
        /// <param name="args">Текст</param>
        /// <returns></returns>
        public bool IsPresent(params string[] args)
        {
            var source = _element == null ? TryGetHtml() : _element.Element.Text;

            var forReturn = true;
            foreach (var text in args)
            {
                if (!source.Contains(text))
                {
                    if (!_commandParam.NoDebug)
                        ErrorElement("Текст: '" + text + "' не найден", ErrorType.Typo);

                    forReturn = false;
                }
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Не существует ли видимый текст на странице или в элементе-цепочке ?
        /// </summary>
        /// <param name="args">Текст</param>
        /// <returns></returns>
        public bool IsNotPresent(params string[] args)
        {
            var source = _element == null ? TryGetHtml() : _element.Element.Text;

            var forReturn = true;
            foreach (var text in args)
            {
                if (source.Contains(text))
                {
                    if (!_commandParam.NoDebug)
                        ErrorElement("Текст: '" + text + "' найден", ErrorType.Typo);

                    forReturn = false;
                }
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент-кнопка незадизейблена ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsEditable(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var element = IsPresentOrFall(button.Link, false);

            var forReturn = true;
            if(!element.Element.Enabled)
            {
                if(!comPar.NoDebug)
                    ErrorElement("Элемент: " + element.Link + ", не активен", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент-кнопка задизейблена ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsNotEditable(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var element = IsPresentOrFall(button.Link, false);

            var forReturn = true;
            if (element.Element.Enabled)
            {
                if (!comPar.NoDebug)
                    ErrorElement("Элемент: " + element.Link + ", активен", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Сравнение 2 значений  (должны совпадать)
        /// </summary>
        /// <param name="value1">первое</param>
        /// <param name="value2">второе</param>
        /// <param name="text">текст выводимый в дебаг</param>
        /// <returns></returns>
        public bool Assert<T>(T value1, T value2, string text = "") where T : struct
        {
            var forReturn = true;
            if (value1.ToString() != value2.ToString())
            {
                if (!_commandParam.NoDebug)
                    Error("Значение '" + value1 + "' != '" + value2 + "' " + text, ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Сравнение 2 значений  (должны не совпадать)
        /// </summary>
        /// <param name="value1">первое</param>
        /// <param name="value2">второе</param>
        /// <param name="text">текст выводимый в дебаг</param>
        /// <returns></returns>
        public bool AssertNot<T>(T value1, T value2, string text = "") where T : struct
        {
            var forReturn = true;
            if (value1.ToString() == value2.ToString())
            {
                if (!_commandParam.NoDebug)
                    Error("Значение '" + value1 + "' == '" + value2 + "' " + text, ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// В элементе-кнопке находится заданный текст?
        /// </summary>
        /// <param name="button">Элемент-кнопка</param>
        /// <param name="value">Текст</param>
        /// <returns></returns>
        public bool Assert(ParamButton button, string value)
        {
            var comPar = _commandParam.Copy();

            var newValue = Get(button);

            var forReturn = true;
            if (newValue != value)
            {
                if (!comPar.NoDebug)
                    ErrorElement("Значение '" + (value ?? "null") + "' != '" + (newValue ?? "null") + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент-кнопка содержит заданный текст?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="value">Текст</param>
        /// <returns></returns>
        public bool AssertContains(ParamButton button, string value)
        {
            var comPar = _commandParam.Copy();
            var newValue = Get(button);

            var forReturn = true;
            if(!newValue.Contains(value))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Значение '" + newValue + "' не содержит '" + (value ?? "null") + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент-кнопка не содержит заданный текст?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="value">Текст</param>
        /// <returns></returns>
        public bool AssertNotContains(ParamButton button, string value)
        {
            var comPar = _commandParam.Copy();
            var newValue = Get(button);

            var forReturn = true;
            if (newValue.Contains(value))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Значение '" + newValue + "' содержит '" + (value ?? "null") + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент существует на странице или в элементе-цепочке (даже невидимый) ?
        /// </summary>
        /// <param name="button">Xpath</param>
        /// <returns></returns>
        public bool Assert(ParamButton button)
        {
            var el = GetElement(button.Link);

            var forReturn = true;
            if (el == null)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Элемент : '" + button.Link + "' не найден", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        public bool AssertNot(ParamButton button)
        {
            var el = GetElement(button.Link);

            var forReturn = true;
            if (el != null)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Элемент : '" + button.Link + "' найден", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Элемент-кнопка существует на странице или в элементе-цепочке ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="count">Какой по счету</param>
        /// <returns></returns>
        public bool IsPresent(ParamButton button, int? count = null)
        {
            var oldLink = button.Link;

            var isVisible = true;
            var isPresent = true;
            if (count.HasValue)
            {
                var link = button.SetCount(count.Value).Link;

                var el = GetElement(link);

                if (el != null)
                {
                    if (!el.Displayed)
                    {
                        if(!_commandParam.NoDebug)
                            ErrorElement("Элемент : '" + link + "' есть, но невидим", ErrorType.Typo);

                        isVisible = false;
                    }
                }
                else
                {
                    if(!_commandParam.NoDebug)
                        ErrorElement("Элемент : '" + link + "' не найден", ErrorType.Typo);

                    isPresent = false;
                    isVisible = false;
                }
            }
            else
            {
                var elements = GetElements(button.Link);
                var link = button.Link;
            
                if(elements == null)
                {
                    if(!_commandParam.NoDebug)
                        ErrorElement("Элемент : '" + oldLink + "' не найден", ErrorType.Typo);

                    isPresent = false;
                }
                else
                {
                    if (elements.Any())
                    {
                        var j = 1;
                        foreach (var element in elements)
                        {
                            link = ParametersFunctions.GetXPathCount(link, j);
                            ScrollToView(element);

                            if (element.Displayed)
                            {
                                isVisible = true;
                                break;
                            }

                            isVisible = false;

                            j++;
                        }
                    }
                    else
                    {
                        if (!_commandParam.NoDebug)
                            ErrorElement("Элемент : '" + oldLink + "' не найден", ErrorType.Typo);

                        isPresent = false;
                    }
                }
            }

            if(!isVisible && isPresent)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Элемент : '" + oldLink + "' есть, но невидим", ErrorType.Typo);
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return isPresent && isVisible;
        }

        /// <summary>
        /// Элемент-кнопка не существует на странице или в элементе-цепочке ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="count">Какой по счету</param>
        /// <returns></returns>
        public bool IsNotPresent(ParamButton button, int? count = null)
        {
            var oldLink = button.Link;

            var isVisible = false;

            if (count.HasValue)
            {
                var link = button.SetCount(count.Value).Link;
                var el = GetElement(link);

                if (el != null)
                {
                    if (el.Displayed)
                    {
                        if (!_commandParam.NoDebug)
                            ErrorElement("Элемент : '" + link + "' есть и видимый", ErrorType.Typo);

                        isVisible = true;
                    }
                }
            }
            else
            {
                var elements = GetElements(button.Link);
                var link = button.Link;

                if(elements != null)
                {
                    var j = 1;
                    foreach (var element in elements)
                    {
                        link = ParametersFunctions.GetXPathCount(link, j);
                        ScrollToView(element);

                        if (element.Displayed)
                        {
                            isVisible = true;
                            break;
                        }
                        j++;
                    }
                }
            }

            if (isVisible)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Элемент : '" + oldLink + "' есть и видимый", ErrorType.Typo);
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return !isVisible;
        }

        /// <summary>
        /// В поле находится заданное значение ?
        /// </summary>
        /// <param name="field">Параметр поля</param>
        /// <returns></returns>
        public bool Assert(ParamField field)
        {
            if (field.Value == null)
                throw new ArgumentException("Значение поля должно быть задано");

            var comPar = _commandParam.Copy();
            var getVal = Get(field);

            var forReturn = true;
            if (field.Value != getVal)
            {
                if (!comPar.NoDebug)
                    ErrorElement("Значение: '" + field.Value + "' != '" + getVal + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// В скрытом элементе-кнопке находится заданное значение ?
        /// </summary>
        /// <param name="field">Параметр кнопки</param>
        /// <returns></returns>
        public bool AssertHidden(ParamField field)
        {
            if (field.Value == null)
                throw new ArgumentException("Значение поля должно быть задано");

            var el = GetElement(field.Field.Link);

            var forReturn = true;
            if (el != null)
            {
                var getVal = el.GetAttribute("value");

                if (getVal != field.Value)
                {
                    if (!_commandParam.NoDebug)
                        ErrorElement("Значение: '" + field.Value + "' != '" + getVal + "'", ErrorType.Typo);

                    forReturn = false;
                }
            }
            else
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Скрытое значение '" + field.Field.Link + "' не найдено", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// В селекте находится заданное значение ?
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public bool Assert(ParamSelect select)
        {
            if (select.Value == null)
                throw new ArgumentException("Значение селекта должно быть задано");

            var comPar = _commandParam.Copy();
            var getVal = Get(select);

            var forReturn = true;
            if (select.Value != getVal)
            {
                if (!comPar.NoDebug)
                    ErrorElement("Значение: '" + select.Value + "' != '" + getVal + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// В селекте есть заданное значение ?
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public bool AssertContains(ParamSelect select)
        {
            if (select.Value == null)
                throw new ArgumentException("Значение селекта должно быть задано");

            var comPar = _commandParam.Copy();

            Click(select.But);
            var forReturn = NoDebug(comPar.NoDebug).IsPresent(select.ItemValue);
            Click(select.But);

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// В селекте нет заданного значения ?
        /// </summary>
        /// <param name="select">Параметр селекта</param>
        /// <returns></returns>
        public bool AssertNotContains(ParamSelect select)
        {
            if (select.Value == null)
                throw new ArgumentException("Значение селекта должно быть задано");

            var comPar = _commandParam.Copy();

            Click(select.But);
            var forReturn = NoDebug(comPar.NoDebug).IsNotPresent(select.ItemValue);
            Click(select.But);

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Проверка валидации в поле
        /// </summary>
        /// <param name="field">Параметр поля</param>
        /// <param name="button">Кнопка на которую нажать, чтобы появилась ошибка</param>
        /// <param name="checkText">Текст ошибки валидации</param>
        /// <returns></returns>
        public bool Check(ParamField field, ParamButton button, string checkText)
        {
             if (field.Value == null)
                 throw new ArgumentException("Значение поля должно быть задано");

            var comPar = _commandParam.Copy();

            var value = Get(field);
            SendKeys(field);

            if(button != null)
                Click(button);

            var forReturn = NoDebug(comPar.NoDebug).IsPresent(checkText);
            SendKeys(field.SetValue(value));

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Содержится ли в атрибуте элемента-кнопки значение ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="attr">Имя атрибута</param>
        /// <param name="contain">Значение</param>
        /// <returns></returns>
        public bool Assert(ParamButton button, string attr, string contain)
        {
             var comPar = _commandParam.Copy();

             var attrGet = Get(button, attr);

            var forReturn = true;
            if (!attrGet.Attr.Contains(contain))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Аттрибут поля: '" + attrGet.Link + "' не содержит '" + (contain ?? "null") + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Не содержится ли в атрибуте элемента-кнопки значение ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <param name="attr">Имя атрибута</param>
        /// <param name="contain">Значение</param>
        /// <returns></returns>
        public bool AssertNot(ParamButton button, string attr, string contain)
        {
            var comPar = _commandParam.Copy();

            var attrGet = Get(button, attr);

            var forReturn = true;
            if (attrGet.Attr.Contains(contain))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Аттрибут поля: '" + attrGet.Link + "' содержит '" + (contain ?? "null") + "'", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли чек-бокс как выбранный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsChecked(ParamButton button)
        {
            var el = IsPresentOrFall(button.Link, false);

            var forReturn = true;
            if(!el.Element.Selected)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Радио-кнопка: '" + el.Link + "' не выбрана", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли чек-бокс как не выбранный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsNotChecked(ParamButton button)
        {
            var el = IsPresentOrFall(button.Link, false);

            var forReturn = true;
            if (el.Element.Selected)
            {
                if (!_commandParam.NoDebug)
                    ErrorElement("Радио-кнопка: '" + el.Link + "' выбрана", ErrorType.Typo);

                forReturn = false;
            }

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли элемент-кнопка как задизейбленный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsDisabled(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var disabled = Get(button, "disabled");

            var forReturn = true;
            if(disabled.Attr != "true")
            {
                if (!comPar.NoDebug)
                    ErrorElement("Кнопка: '" + button.Link + "' активна", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли элемент-кнопка как не задизейбленный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsNotDisabled(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var disabled = Get(button, "disabled");

            var forReturn = true;
            if (disabled.Attr == "true")
            {
                if (!comPar.NoDebug)
                    ErrorElement("Кнопка: '" + button.Link + "' не активна", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли элемент-кнопка как активный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsActive(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var attr = Get(button, "class");

            var forReturn = true;
            if (!attr.Attr.Contains("active"))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Кнопка: '" + button.Link + "' не нажата", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Отмечен ли элемент-кнопка как не активный ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsNotActive(ParamButton button)
        {
            var comPar = _commandParam.Copy();

            var attr = Get(button, "class");

            var forReturn = true;
            if (attr.Attr.Contains("active"))
            {
                if (!comPar.NoDebug)
                    ErrorElement("Кнопка: '" + button.Link + "' нажата", ErrorType.Typo);

                forReturn = false;
            }

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            _commandParam.Default();

            return forReturn;
        }

        /// <summary>
        /// Существует ли элемент с текстом ?
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="count">Какой по счету</param>
        /// <param name="notText">Какого текста не должно быть</param>
        /// <param name="probel">Разделитель</param>
        /// <returns></returns>
        public bool IsElementTextPresent(string text, int? count = null, string notText = null, string probel = " ")
        {
            var addNotText = "";
            string[] words;
            if(notText != null)
            {
                words = notText.Split(' ');
                words.ToList().ForEach(t => addNotText += "[text()[not(contains(.,'" + t + "'))]]");
            }

            var addText = "";
            words = text.Split(' ');

            foreach(var word in words)
            {
                if(words.Length > 1)
                {
                    if(word == words[0])
                        addText += "[text()[contains(.,'" + word + probel + "')]]";
                    else if(word == words[words.Length - 1])
                        addText += "[text()[contains(.,'" + probel + word + "')]]";
                    else
                        addText += "[text()[contains(.,'"+ probel + word + probel + "')]]";
                }
                else
                    addText += "[text()[contains(.,'" + word + "')]]";
            }

            var link = "//*" + addText + " " +addNotText;

            return IsPresent(new ParamButton(link), count);
        }

        /// <summary>
        /// Не существует ли элемент с текстом ?
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="count">Какой по счету</param>
        /// <param name="notText">Какого текста не должно быть</param>
        /// <param name="probel">Разделитель</param>
        /// <returns></returns>
        public bool IsElementTextNotPresent(string text, int? count = null, string notText = null, string probel = " ")
        {
            var addNotText = "";
            string[] words;
            if (notText != null)
            {
                words = notText.Split(' ');
                words.ToList().ForEach(t => addNotText += "[text()[not(contains(.,'" + t + "'))]]");
            }

            var addText = "";
            words = text.Split(' ');

            foreach (var word in words)
            {
                if (words.Length > 1)
                {
                    if (word == words[0])
                        addText += "[text()[contains(.,'" + word + probel + "')]]";
                    else if (word == words[words.Length - 1])
                        addText += "[text()[contains(.,'" + probel + word + "')]]";
                    else
                        addText += "[text()[contains(.,'" + probel + word + probel + "')]]";
                }
                else
                    addText += "[text()[contains(.,'" + word + "')]]";
            }

            var link = "//*" + addText + " " + addNotText;

            return IsNotPresent(new ParamButton(link), count);
        }

        /// <summary>
        /// Существует ли элемент с текстом ?
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="count">Какой по счету</param>
        /// <param name="notText">Какого текста не должно быть</param>
        /// <returns></returns>
        public bool IsElementMaskPresent(string text, int? count = null, string notText = null)
        {
            string addNotText;
            if(notText != null)
                addNotText = "[not(contains(.,'" + notText + "'))]";
            else
                addNotText = "";

            var addText = "[contains(.,'" + text + "')]";

            var link = "//*[text()" + addText + " " + addNotText + "]";

            return IsPresent(new ParamButton(link), count);
        }

        /// <summary>
        /// Не существует ли элемент с текстом ?
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="count">Какой по счету</param>
        /// <param name="notText">Какого текста не должно быть</param>
        /// <returns></returns>
        public bool IsElementMaskNotPresent(string text, int? count = null, string notText = null)
        {
            string addNotText;
            if (notText != null)
                addNotText = "[not(contains(.,'" + notText + "'))]";
            else
                addNotText = "";

            var addText = "[contains(.,'" + text + "')]";
            var link = "//*[text()" + addText + " " + addNotText + "]";

            return IsNotPresent(new ParamButton(link), count);
        }

        /// <summary>
        /// Видимый ли элемент-кнопка ?
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns></returns>
        public bool IsVisible(ParamButton button)
        {
            IWebElement element;

            if (!_commandParam.Visible)
                element = GetElement(button.Link);
            else
            {
                var el = GetOnlyOneVisible(button.Link, true);
                element = el != null ? el.Element : null;
            }

            if(element != null)
            {
                ScrollToView(element);

                return element.Displayed;
            }

            if (!_commandParam.NoDebug)
                throw FailingElement("Необходимый элемент не найден", "Элемент '" + button.Link + "' не найден");

            _commandParam.Default();

            return false;
        }

        /// <summary>
        /// Загрузить XML по ссылке
        /// </summary>
        /// <param name="href">Ссылка</param>
        /// <param name="filename">Имя файла</param>
        /// <returns></returns>
        public SelXml LoadXmlbyHref(string href, string filename)
        {
            filename = Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(filename)));

            DeleteOldFile(filename);

            if(_waitForDelete != null) _waitForDelete.Wait();

            Url(href);
            WaitDownloadFile(filename);

            var xml = GetXmlFile(filename);
            DeleteOldFile(filename);
            return xml;
        }

        /// <summary>
        /// Выгрузить XML в объект из файла
        /// </summary>
        /// <param name="filename">Имя файла</param>
        /// <returns></returns>
        private SelXml GetXmlFile(string filename)
        {
            var xml = new XmlDocument();

            if (CheckDownloadFile(filename))
            {
                xml.Load(GetFolderPath() + filename);

                var node = xml.DocumentElement;

                if (node == null)
                    throw new ArgumentException("node");

                XmlNamespaceManager nsmgr = null;
                if (!string.IsNullOrEmpty(node.NamespaceURI))
                {
                    nsmgr = new XmlNamespaceManager(xml.NameTable);
                    nsmgr.AddNamespace("ns", node.NamespaceURI);
                }

                return new SelXml(node, nsmgr);
            }

            throw FailingTest("Файл " + filename + " не найден");
        }

        /// <summary>
        /// Открыть новое окно по ссылке
        /// </summary>
        /// <param name="button">Параметр кнопки</param>
        /// <returns>GUID нового окна</returns>
        public string OpenNewWindow(ParamButton button)
        {
            var windowsOld = _driver.WindowHandles;

            var comPar = _commandParam.Copy();
            Click(button);

            var time = new Stopwatch();
            time.Start();

            ReadOnlyCollection<string> windowsNew;
            do
            {
                windowsNew = _driver.WindowHandles;
            }
            while (windowsNew.Count == windowsOld.Count && time.Elapsed.TotalSeconds < ParametersInit.FindElementTimeOut);

            var newWindow = windowsNew.ToArray().Except(windowsOld).ToArray();

            if (!newWindow.Any())
                throw FailingTest("Новое окно не появилось");

            var windowId = newWindow.First();
            _driver.SwitchTo().Window(windowId);

            if (comPar.Sleep > 0)
                Thread.Sleep(comPar.Sleep * 1000);

            WaitForPageToLoad(comPar.Ajax);
            WaitForAjax();

            _commandParam.Default();

            return windowId;
        }

        /// <summary>
        /// Закрыть текущее окно и переключится за изначальное
        /// </summary>
        public void CloseNewWindow()
        {
            _driver.Close();

            IAlert alert;
            try
            {
                alert = _driver.SwitchTo().Alert();
            }
            catch (Exception)
            {
                alert = null;
            }

            if (alert != null)
                alert.Accept();

            var windows = _driver.WindowHandles;

            if(!windows.Any())
                throw FailingTest("Было закрыто последнее активное окно сессии");

            _driver.SwitchTo().Window(windows.First());

            if (_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            WaitForPageToLoad();
            WaitForAjax();

            _commandParam.Default();
        }

        /// <summary>
        /// Закрыть все окна кроме текущего
        /// </summary>
        public void CloseAllWindowsButThis()
        {
            var currentWin = _driver.CurrentWindowHandle;
            var currentWins = _driver.WindowHandles;

            foreach (var win in currentWins)
            {
                if(win != currentWin)
                {
                    SwitchToWindow(win);
                    CloseNewWindow();
                }
            }

            WaitForPageToLoad();
            WaitForAjax();
        }

        /// <summary>
        /// Переключится на другое окно по GUID
        /// </summary>
        /// <param name="windowId">GUID окна</param>
        public void SwitchToWindow(string windowId)
        {
            var windows = _driver.WindowHandles;

            if (ParametersFunctions.IsValidGuid(windowId))
            {
                if (windows.Contains(windowId))
                    _driver.SwitchTo().Window(windowId);
                else
                    throw FailingTest("Окна с таким ID не найдено");
            }
            else
                throw FailingTest("На вход должен быть гуид окна");

            if(_commandParam.Sleep > 0)
                Thread.Sleep(_commandParam.Sleep * 1000);

            WaitForPageToLoad();

            _commandParam.Default();
        }

        /// <summary>
        /// Создать новое окно и переключится на него
        /// </summary>
        /// <returns>GUID нового окна</returns>
        public string CreateWindowAndSwitch()
        {
            var windowsOld = _driver.WindowHandles;

            var action = new Actions(_driver);
            action.KeyDown(Keys.Control).SendKeys("n").KeyUp(Keys.Control).Perform();

            var time = new Stopwatch();
            time.Start();

            ReadOnlyCollection<string> windowsNew;
            do
            {
                windowsNew = _driver.WindowHandles;
            }
            while (windowsNew.Count == windowsOld.Count && time.Elapsed.TotalSeconds < ParametersInit.FindElementTimeOut);

            var newWindow = windowsNew.ToArray().Except(windowsOld).ToArray();

            if(!newWindow.Any())
                throw FailingTest("Новое окно не появилось");

            var windowId = newWindow.First();
            _driver.SwitchTo().Window(windowId);
            return windowId;
        }

        public void GoToFrame(string name)
        {
            _driver.SwitchTo().Frame(name);
            WaitForPageToLoad();
        }

        public void GoToFrame(int name)
        {
            _driver.SwitchTo().Frame(name);
            WaitForPageToLoad();
        }

        public void OutFromFrame()
        {
            _driver.SwitchTo().DefaultContent();
            WaitForPageToLoad();
        }
    }

    /// <summary>
    /// Контейнер параметров текущей сессии
    /// </summary>
    sealed class SessionParam
    {
        /// <summary>
        /// Элемент //body для поиска текста
        /// </summary>
        public IWebElement Html;

        public SelElement LogoutHref;
        /// <summary>
        /// Макс номер таймаута js
        /// </summary>
        public Int64 SetTimeoutMaxKey;
        /// <summary>
        /// Макс номер таймаута интервала js
        /// </summary>
        public Int64 SetIntervalMaxKey;
        /// <summary>
        /// Число текущих Ajax запросов jQuery
        /// </summary>
        public int Ajax;

        /// <summary>
        /// Счетчик таймаутов которых ждали 
        /// </summary>
        public int SetTimeoutCounter;
        /// <summary>
        /// Счетчик таймаутов интервалов которых ждали
        /// </summary>
        public int SetIntervalCounter;

        /// <summary>
        /// Высота окна браузера
        /// </summary>
        public int WindowY;

        public Stopwatch Time = new Stopwatch();

        public DateTime LastGetElement = DateTime.Now;

        public TestInfo CurrentTestInfo;

        public int TryGetDriver;
    }

    /// <summary>
    /// Контейнер текущей выполняемой команды
    /// </summary>
    sealed class CommandParam
    {
        public bool Wait;
        public bool Visible;
        public bool NoDebug;
        public bool AlertOk;
        public bool AlertNot;
        public int Sleep;
        public int Ajax;
        public bool NotChain;
        public bool NotAjax;
        public bool NoScroll;

        public void Default()
        {
            Wait = false;
            AlertOk = false;
            AlertNot = false;
            Sleep = 0;
            NotChain = false;
            Visible = false;
            NoDebug = false;
            NotAjax = false;
            NoScroll = false;
            Ajax = 0;
        }

        public CommandParam Copy()
        {
            var newCom = new CommandParam
            {
                Wait = Wait,
                Visible = Visible,
                NoDebug = NoDebug,
                AlertNot = AlertNot,
                AlertOk = AlertOk,
                Sleep = Sleep,
                Ajax = Ajax,
                NotChain = NotChain,
                NotAjax = NotAjax,
                NoScroll = NoScroll
            };

            return newCom;
        }
    }

    /// <summary>
    /// Информация об ошибке в тесте
    /// </summary>
    sealed class BackTraceInfo
    {
        /// <summary>
        /// Имя класса
        /// </summary>
        public string ClassName;
        /// <summary>
        /// Имя метода
        /// </summary>
        public string MethodName;
        /// <summary>
        /// Строчка
        /// </summary>
        public int Line;
        /// <summary>
        /// Текст бага to.do
        /// </summary>
        public string Bug;

        public string FullName;

        public BackTraceInfo(string className, string methodName, int line, string bug)
        {
            ClassName = className;
            MethodName = methodName;
            Line = line;
            Bug = bug;
            FullName = ClassName + "." + MethodName;
        }
    }

    [Serializable]
    public class SeleniumFailException : ApplicationException
    {
        public SeleniumFailException()
        {
        }

        public SeleniumFailException(string message) : base(message)
        {
        }

        public SeleniumFailException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SeleniumFailException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
