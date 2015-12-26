using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using System.Web.Mvc;
using BOL.Site.Models;
using Digital.Web.Attributes;
using CurrentRequestContext = BOL.Site.Utils.ContextContainers.CurrentRequestContext;

namespace BOL.Site.Controllers
{
    public class AutotestController : BaseController
    {
        [HttpGet]
        [Digital.Web.Attributes.Route(name: "AutotestIndex", url: "autotest",
            defaults: "controller = Autotest, action = Index", priority: RoutePriorities.High)]
        public ActionResult Index()
        {
            var model = new Autotest();
            return View(model);
        }

        public ActionResult GetParallel(Guid parallelId)
        {
            var model = new Autotest();

            var parallel = model.GetParallelInfo(parallelId);
            var parallelContainer = new Dictionary<string, object>();
            var testList = new List<object>();

            foreach (var tests in model.GetTestsInfo(parallelId))
            {
                var testContainer = new Dictionary<string, object>();
                var errorList = model.GetErrorsInfo(parallelId, tests.TestId).Cast<object>().ToList();

                testContainer.Add("test", tests);
                testContainer.Add("errors", errorList);
                testList.Add(testContainer);
            }

            parallelContainer.Add("parallel", parallel);
            parallelContainer.Add("tests", testList);

            return Json(parallelContainer);
        }

        private static readonly Regex Rgx = new Regex(@"FN-\d+");

        public ActionResult GetParallel2(Guid parallelId)
        {
            var model = new Autotest();

            var parallel = model.GetParallelInfo(parallelId);
            var tests = model.GetTestsInfo(parallelId);
            var errors = model.GetErrorsInfo2(parallelId);

            for (var i = 0; i < errors.Count; i++)
            {
                errors[i].Text = errors[i].Text.Replace("<", "").Replace(">", "").Replace("\r\n", " <br /> ");
                
                if (!string.IsNullOrEmpty(errors[i].Bug))
                {
                    if(errors[i].Bug.Contains("http"))
                        errors[i].Bug = "<a href=\"" + errors[i].Bug + "\" target=\"_blank\">Ссылка на баг</a>";
                    else if (Rgx.IsMatch(errors[i].Bug))
                        errors[i].Bug = "<a href=\"http://jira.actidev.ru/browse/" + errors[i].Bug + "\" target=\"_blank\">Ссылка на баг</a>";
                }
            }

            var ownerList = new Dictionary<string, Dictionary<string, dynamic>>();

            var testsBegin = 0;
            var testsEnd = 0;
            var testsFailed = 0;
            var isChecked = true;

            foreach (var test in tests)
            {
                var testId = test.TestId;
                var errorList = errors.Where(t => t.TestId == testId).ToList();
                var isCheckedTest = errorList.All(t => t.IsChecked);

                isChecked = isChecked && isCheckedTest;

                if (test.TimeEnd != null)
                    testsEnd++;

                testsBegin++;

                if (!isCheckedTest)
                    testsFailed++;

                var testObj = new
                {
                    test.TestId,
                    test.Name,
                    test.LoginName,
                    test.TimeStart,
                    test.TimeEnd,
                    IsChecked = isCheckedTest,
                    Errors = errorList
                };


                if (ownerList.ContainsKey(test.Owner))
                {
                    ownerList[test.Owner]["Tests"].Add(testObj);
                    ownerList[test.Owner]["IsChecked"] = ownerList[test.Owner]["IsChecked"] && testObj.IsChecked;
                }
                else
                {
                    ownerList.Add(test.Owner, new Dictionary<string, dynamic>
                    {
                        { "Tests", new List<dynamic> { testObj } },
                        { "IsChecked", testObj.IsChecked }
                    });
                }
            }

            var owners = ownerList.Select(owner => new
            {
                OwnerName = owner.Key,
                IsChecked = owner.Value["IsChecked"],
                Tests = owner.Value["Tests"]
            }).Cast<object>().ToList();

            var parallelObj = new
            {
                parallel.Id,
                parallel.Address,
                parallel.ScreenPath,
                parallel.TimeStart,
                parallel.TimeEnd,
                parallel.TestsCount,
                Owners = owners,
                TestsBegin = testsBegin,
                TestsEnd = testsEnd,
                TestsFailed = testsFailed,
                IsChecked = isChecked
            };

            return Json(parallelObj);
        }

        public ActionResult GetParallels()
        {
            var model = new Autotest();
            return Json(model.GetParallelsInfo());
        }

        public ActionResult DeleteParallel(Guid parallelId)
        {
            var model = new Autotest();
            model.DeleteParallelInfo(parallelId);

            return Json("Лог запуска успешно удален");
        }

        public ActionResult DeleteTest(Guid parallelId, Guid testId)
        {
            var model = new Autotest();
            model.DeleteTestInfo(parallelId, testId);

            return Json("Лог теста успешно удален");
        }

        public ActionResult DeleteError(Guid errorId)
        {
            var model = new Autotest();
            model.DeleteErrorInfo(errorId);

            return Json("Лог ошибки успешно удален");
        }

        public ActionResult CheckTest(Guid parallelId, Guid testId)
        {
            var model = new Autotest();
            model.CheckTestInfo(parallelId, testId);

            return Json("Лог теста успешно отмечен проверенным");
        }

        public ActionResult CheckError(Guid errorId)
        {
            var model = new Autotest();
            model.CheckErrorInfo(errorId);

            return Json("Лог ошибки успешно отмечен проверенным");
        }

        public ActionResult UnCheckError(Guid errorId)
        {
            var model = new Autotest();
            model.UnCheckErrorInfo(errorId);

            return Json("Лог ошибки успешно отмечен НЕ проверенным");
        }

        public ActionResult GetScreen(string address)
        {
            var addressUri = new Uri(address);

            if (addressUri.Scheme != Uri.UriSchemeFile)
                throw new ArgumentException("Адрес address должен быть путем к файлу");

            if(Path.GetExtension(addressUri.LocalPath) == "")
                throw new ArgumentException("Адрес address должен быть путем к файлу");

            string base64;
            using (var user1 = new UncAccess(addressUri.LocalPath, "", "", ""))
            {
                try
                {
                    var byteFromFile = System.IO.File.ReadAllBytes(addressUri.LocalPath);
                    base64 = Convert.ToBase64String(byteFromFile);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message + Environment.NewLine + user1.LastError);
                }
            }

            return Json(base64);
        }

        public ActionResult CopyScreen(Guid parallelId, string screenPath)
        {
            var model = new Autotest();
            var addressFrom = model.GetParallelInfo(parallelId).ScreenPath;

            var addressFromUri = new Uri(addressFrom);
            var addressToUri = new Uri(screenPath);

            if (addressFromUri.Scheme != Uri.UriSchemeFile)
                throw new ArgumentException("Адрес addressFrom должен быть путем к папке");

            if (addressToUri.Scheme != Uri.UriSchemeFile)
                throw new ArgumentException("Адрес addressTo должен быть путем к папке");

            if (Path.GetExtension(addressFromUri.LocalPath) != "")
                throw new ArgumentException("Адрес addressFrom должен быть путем к папке, а не к файлу");

            if (Path.GetExtension(addressToUri.LocalPath) != "")
                throw new ArgumentException("Адрес addressTo должен быть путем к файлу, а не к файлу");

            var dirFrom = new DirectoryInfo(addressFromUri.LocalPath);
            var dirTo = new DirectoryInfo(addressToUri.LocalPath);

            using (var user1 = new UncAccess(dirFrom.FullName.Substring(0, dirFrom.FullName.Count() - 1), "", "", ""))
            {
                using (var user2 = new UncAccess(dirTo.FullName.Substring(0, dirTo.FullName.Count() - 1), "", "", ""))
                {
                    try
                    {
                        CopyFilesRecursively(dirFrom, dirTo);
                        model.ChangeParallelScreenPath(parallelId, screenPath);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.Message + Environment.NewLine + user1.LastError + Environment.NewLine + user2.LastError);
                    }
                }
            }

            return Json("Скрины логов успешно перемещены");
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {   
            if (!CurrentRequestContext.IsAuthenticated || !CurrentRequestContext.IsQaUser)
                filterContext.Result = new RedirectResult("/Error/Error404");

            base.OnActionExecuting(filterContext);
        }

        //Переписал из BaseController - мне важно возвращать даты вместе со временем!
        private int? _maxJsonLength;
        private int MaxJsonLength
        {
            get
            {
                if (!_maxJsonLength.HasValue)
                {
                    System.Configuration.Configuration conf = WebConfigurationManager.OpenWebConfiguration("~");
                    var jsonSection = conf.GetSection("system.web.extensions/scripting/webServices/jsonSerialization") as
                            ScriptingJsonSerializationSection;
                    if (jsonSection != null)
                        _maxJsonLength = jsonSection.MaxJsonLength;
                    else
                        _maxJsonLength = 1024 * 1024 * 2;
                }
                return _maxJsonLength.Value;
            }
        }

        //Переписал из BaseController - мне важно возвращать даты вместе со временем!
        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = MaxJsonLength // allow big JSON
            };
        }
    }

    //вспомогательный класс для аутентификации на удаленной машине + под другим доменом
    public class UncAccess : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct UserInfo2
        {
            internal String ui2_local;
            internal String ui2_remote;
            internal String ui2_password;
            internal UInt32 ui2_status;
            internal UInt32 ui2_asg_type;
            internal UInt32 ui2_refcount;
            internal UInt32 ui2_usecount;
            internal String ui2_username;
            internal String ui2_domainname;
        }

        [DllImport("NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern UInt32 NetUseAdd(
            String uncServerName,
            UInt32 level,
            ref UserInfo2 buf,
            out UInt32 parmError);

        [DllImport("NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern UInt32 NetUseDel(
            String uncServerName,
            String useName,
            UInt32 forceCond);

        private string _sUncPath;
        private string _sUser;
        private string _sPassword;
        private string _sDomain;
        private int _iLastError;

        public UncAccess()
        {
        }

        public UncAccess(string uncPath, string user, string domain, string password)
        {
            Login(uncPath, user, domain, password);
        }

        public int LastError
        {
            get { return _iLastError; }
        }

        ///
        /// Connects to a UNC share folder with credentials
        ///

        /// UNC share path
        /// Username
        /// Domain
        /// Password
        /// True if login was successful
        public bool Login(string uncPath, string user, string domain, string password)
        {
            _sUncPath = uncPath;
            _sUser = user;
            _sPassword = password;
            _sDomain = domain;
            return NetUseWithCredentials();
        }

        private bool NetUseWithCredentials()
        {
            try
            {
                var useinfo = new UserInfo2
                {

                    ui2_remote = _sUncPath,
                    ui2_username = _sUser,
                    ui2_domainname = _sDomain,
                    ui2_password = _sPassword,
                    ui2_asg_type = 0,
                    ui2_usecount = 1
                };
                uint paramErrorIndex;
                var returncode = NetUseAdd(null, 2, ref useinfo, out paramErrorIndex);
                _iLastError = (int)returncode;
                return returncode == 0;
            }
            catch
            {
                _iLastError = Marshal.GetLastWin32Error();
                return false;
            }
        }

        ///
        /// Closes the UNC share
        ///

        /// True if closing was successful
        public bool NetUseDelete()
        {
            try
            {
                var returncode = NetUseDel(null, _sUncPath, 2);
                _iLastError = (int)returncode;
                return (returncode == 0);
            }
            catch
            {
                _iLastError = Marshal.GetLastWin32Error();
                return false;
            }
        }

        public void Dispose()
        {
            NetUseDelete();
        }
    }
}
