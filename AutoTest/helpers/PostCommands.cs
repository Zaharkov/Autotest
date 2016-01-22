using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using OpenQA.Selenium.Remote;
using AutoTest.helpers.Parameters;
using AutoTest.helpers.Selenium;

namespace AutoTest.helpers
{
    #region PostCommands
    /// <summary>
    /// Класс для работы с Http запросами
    /// на сервер сайта напрямую
    /// </summary>
    public class PostCommands : ExecutePostController
    {
        public readonly ParametersRead Param = ParametersRead.Instance();
        public readonly ParametersInit ParamInit;

        public PostCommands(ParametersInit param, SeleniumCommands sel = null, string address = null)
            : base(address ?? param.Address, sel)
        {
            ParamInit = sel == null ? param : sel.ParamInit;
        }

        public void GetWdSessionId(string host, int port, out string sessionId, out DesiredCapabilities capabilities)
        {
            var baseAddress = HttpInfo.BaseAddress;
            try
            {
                HttpInfo.BaseAddress = "http://" + host + ":" + port;
                HttpInfo.Type = HttpType.Get;
                Execute("/wd/hub/sessions");

                var ids = GetResult()["value"].CastToType<object[]>();

                foreach (var id in ids)
                {
                    try
                    {
                        var data = id.CastToType<Dictionary<string, object>>();

                        HttpInfo.Type = HttpType.Get;
                        Execute("/wd/hub/session/" + data["id"] + "/url");

                        sessionId = data["id"].ToString();
                        var capa = data["capabilities"].CastToType<Dictionary<string, object>>();
                        capabilities = new DesiredCapabilities(capa);

                        HttpInfo.BaseAddress = baseAddress;
                        return;
                    }
                    catch(Exception e)
                    {
                        if (e.Message.Contains("It may have died"))
                        {
                            var id2 = id.CastToType<Dictionary<string, object>>();

                            HttpInfo.Type = HttpType.Delete;
                            Execute("/wd/hub/session/" + id2["id"]);
                        }
                    }
                }

                sessionId = null;
                capabilities = null;
            }
            finally
            {
                HttpInfo.BaseAddress = baseAddress;
            }
        }

        public virtual T DoPost<T>(HttpType type, string url, Dictionary<string, object> json = null) where T : class
        {
            HttpInfo.SetType(type);
            if (json != null)
                HttpInfo.AddJsonContent(json);
            Execute(url);

            return GetResult<T>();
        }
    }
    #endregion

    #region ExecuteAndThings
    /// <summary>
    /// Класс формирования и выполнения Http запросов
    /// </summary>
    public class ExecutePostController
    {
        private readonly SeleniumCommands _sel;
        private readonly string _baseAddress;
        public HttpInfo HttpInfo;

        public ExecutePostController(string address, SeleniumCommands sel = null)
        {
            _sel = sel;
            _baseAddress = address;
            var baseAddress = address.StartsWith("https") || address.Contains("localhost") ? address : address.Insert(4, "s");

            ServicePointManager.DefaultConnectionLimit = ParametersInit.ThreadCount;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            HttpInfo = new HttpInfo(ParametersInit.WebDriverTimeOut * 1000)
            {
                BaseAddress = baseAddress
            };
        }

        protected string GetAddress()
        {
            return _baseAddress;
        }

        private string _result;

        /// <summary>
        /// Выполнить Http запрос
        /// </summary>
        /// <param name="url">URL запроса</param>
        /// <param name="notSel"></param>
        public virtual void Execute(string url, bool notSel = false)
        {
            var funcName = url.Split('?').First().Split('/').Last();

            if (_sel != null && !notSel)
            {
                var time = new Stopwatch();
                time.Start();

                var task = Task.Run(() => ExecuteThread(url) );

                while (!task.IsCompleted && time.Elapsed.TotalSeconds < ParametersInit.WebDriverTimeOut)
                    _sel.TouchWebDriver();

                if (time.Elapsed.TotalSeconds > 30)
                    _sel.Error("HTTP запрос " + funcName + " длился более 30 секунд", ErrorType.Timed | ErrorType.Bug);

                if (task.IsFaulted && task.Exception != null)
                {
                    string error;
                    var ex = task.Exception.InnerExceptions.First();
                    try
                    {
                        var jss = new JavaScriptSerializer();
                        var result = jss.Deserialize<Dictionary<string, object>>(ex.Message);

                        if (result.ContainsKey("Error"))
                            error = result["Error"].ToString();
                        else if (result.ContainsKey("Message"))
                            error = result["Message"].ToString();
                        else
                            error = ex.Message;
                    }
                    catch (ArgumentException)
                    {
                        error = ex.Message;
                    }

                    throw FailedPostCommands(funcName + ": " + error, ex);
                }

                if (time.Elapsed.TotalSeconds > ParametersInit.WebDriverTimeOut)
                    throw FailedPostCommands("Время выполнения операции Post истекло: " + funcName);
            }
            else
            {
                ExecuteThread(url);
            }
        }

        /// <summary>
        /// Выполнить Http запрос в параллельном потоке
        /// </summary>
        /// <param name="url">URL запроса</param>
        private void ExecuteThread(string url)
        {
            string responseString;
            try
            {
                HttpInfo.CreateCookie();
                HttpInfo.CreateJson();
                var type = HttpInfo.Type;
                byte[] response;
                if (!type.HasFlag(HttpType.Get))
                {
                    response = HttpInfo.JsonContent == null
                        ? HttpInfo.UploadValues(url, type.ToString().ToUpper(), HttpInfo.QueryString)
                        : HttpInfo.UploadData(url, type.ToString().ToUpper(),
                            Encoding.UTF8.GetBytes(HttpInfo.JsonContent));
                }
                else
                    response = HttpInfo.DownloadData(url);

                responseString = Encoding.UTF8.GetString(response);
            }
            catch (WebException e)
            {
                if (e.Status.HasFlag(WebExceptionStatus.Timeout))
                    throw new HttpResponseException("Post response is Timeout", e);

                var stream = e.Response.GetResponseStream();

                if (stream == null)
                    throw new HttpResponseException("stream == null", e);

                responseString = new StreamReader(stream).ReadToEnd();

                throw new HttpResponseException(responseString, e);
            }
            finally
            {
                HttpInfo.SetDefaultHttpInfo();
            }

            _result = responseString;
        }

        public T GetResult<T>() where T : class
        {
            try
            {
                return new JavaScriptSerializer().DeserializeObject(_result).CastToType<T>();
            }
            catch (InvalidOperationException)
            {
                return _result.CastToType<T>();
            }
            catch (ArgumentException)
            {
                return _result.CastToType<T>();
            }
        }

        public Dictionary<string, object> GetResult()
        {
            return GetResult<Dictionary<string, object>>();
        }

        public void CheckSchema(string jsonString, string schemaString)
        {
            var schema = JsonSchema.Parse(schemaString);
            var result = JObject.Parse(jsonString);
            IList<string> errors;
            var check = result.IsValid(schema, out errors);

            if (!check)
            {
                var strBuilder = new StringBuilder();

                foreach (var error in errors)
                    strBuilder.AppendLine(error);

                if(_sel != null)
                    _sel.Error(strBuilder.ToString(), ErrorType.Typo);
                else
                    Console.WriteLine(strBuilder.ToString());
            }
        }

        /// <summary>
        /// Сообщение об ошибке (через selenium или ексепшеном)
        /// </summary>
        /// <param name="text">Информация об ошибке</param>
        /// <param name="e"></param>
        public Exception FailedPostCommands(string text, Exception e = null)
        {
            if(_sel != null)
                throw _sel.FailingTest("Не удалось выполнить Http запрос", text, e);

            throw new PostException(text, e);
        }
    }
    
    public static class CastObjectToType
    {
        public static T CastToType<T>(this object source, T type = null) where T : class
        {
            var cast = source as T;

            if (cast == null)
                throw new InvalidCastException("Не смог преобразовать объект " + source.GetType() + " к " + typeof(T));

            return cast;
        }

        public static T CastObject<T>(this object source) where T : class
        {
            var ser = new JavaScriptSerializer();
            var str = ser.Serialize(source);
            var obj = ser.Deserialize<T>(str);
            return obj;
        }
    }

    public static class AttributesHelperExtension
    {
        public static string ToDescription(this Enum value)
        {
            var da = (System.ComponentModel.DescriptionAttribute[])value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
            return da.Length > 0 ? da[0].Description : value.ToString();
        }
    }

    /// <summary>
    /// Тип запроса
    /// </summary>
    public enum HttpType
    {
        Post,
        Get,
        Delete,
        Put
    }

    /// <summary>
    /// Контейнер для информации о Http запросе
    /// </summary>
    [System.ComponentModel.DesignerCategory(@"Code")]
    public class HttpInfo : WebClient
    {
        public string JsonContent;

        public HttpType Type;
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Обнулить информацию о Http запросе
        /// (сбросить данные контента, куки и хеадеров)
        /// </summary>
        public void SetDefaultHttpInfo()
        {
            JsonContent = null;
            Type = HttpType.Post;
            QueryString.Clear();
            Headers.Clear();
            _cookies.Clear();
            IsJsonSet = false;
            _json = null;
        }

        public HttpInfo() : this(60000)
        {
            Type = HttpType.Post;
        }

        public HttpInfo(int timeout)
        {
            Type = HttpType.Post;
            Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            if (request != null)
            {
                request.AllowAutoRedirect = false;
                request.Timeout = Timeout;
            }
            return request;
        }

        private readonly Dictionary<string, string> _cookies = new Dictionary<string, string>(); 

        /// <summary>
        /// Добавить Cookie в запрос
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="value">Значение</param>
        public HttpInfo AddCookie(string name, string value)
        {
            _cookies.Add(name, value);
            return this;
        }

        public void CreateCookie()
        {
            Headers.Add(HttpRequestHeader.Cookie, _cookies.Aggregate("", (current, cookie) => current + cookie.Key + "=" + cookie.Value + ";"));
        }

        /// <summary>
        /// Добавить Header в запрос
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="value">Значение</param>
        public HttpInfo AddHeaders(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }

        public WebHeaderCollection GetHeaders()
        {
            return ResponseHeaders;
        }

        private object _json;
        public bool IsJsonSet;

        public HttpInfo AddJsonContent(Dictionary<string, object> jsonObj)
        {
            _json = jsonObj;
            IsJsonSet = true;
            return this;
        }

        public HttpInfo AddJsonContent(List<Dictionary<string,object>> jsonObj)
        {
            _json = jsonObj;
            IsJsonSet = true;
            return this;
        }

        public HttpInfo AddInfoToJson(string name, object value)
        {
            if (IsJsonSet && _json is Dictionary<string, object>)
            {
                var json = (Dictionary<string, object>) _json;
                if(!json.ContainsKey(name))
                    json.Add(name, value);
            }

            return this;
        }

        public HttpInfo CreateJson()
        {
            if (IsJsonSet)
            {
                var js = new JavaScriptSerializer();
                var json = js.Serialize(_json);

                Headers.Add("Content-Type", "application/json");
                JsonContent = json; 
            }

            return this;
        }

        /// <summary>
        /// Добавить контент в запрос
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="value">Значение</param>
        public HttpInfo AddContent(string name, string value)
        {
            QueryString.Add(name, value);
            return this;
        }

        /// <summary>
        /// Задать тип запроса
        /// </summary>
        /// <param name="type">Тип запроса</param>
        public HttpInfo SetType(HttpType type)
        {
            Type = type;
            return this;
        }
    }

    [Serializable]
    public class HttpResponseException : ApplicationException
    {
        public HttpResponseException()
        {
        }

        public HttpResponseException(string message) : base(message)
        {
        }

        public HttpResponseException(string message, Exception inner) : base(message, inner)
        {
        }

        protected HttpResponseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class PostException : ApplicationException
    {
        public PostException()
        {
        }

        public PostException(string message) : base(message)
        {
        }

        public PostException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PostException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    #endregion
}
