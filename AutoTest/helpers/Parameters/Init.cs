using System;
using System.Collections.Generic;
using System.Configuration;

namespace AutoTest.helpers
{
    public class ParametersInit
    {
        public ParametersInit()
        {
            RemoteWd = bool.Parse(GetAppConfig("RemoteWd"));
            Address = GetAppConfig("Address");
            Host = GetAppConfig("Host");
            Port = int.Parse(GetAppConfig("Port"));
            Login.SetValue(GetAppConfig("Login"));
            Password.SetValue(GetAppConfig("Pass"));
        }

        public static string GetAppConfig(string name)
        {
            var value = ConfigurationManager.AppSettings.Get(name);

            if(string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException("Параметр " + name + " не задан в App.Config");

            return value;
        }

        public readonly ParamField Login = new ParamField("//login");
        public readonly ParamField Password = new ParamField("//pass");

        /// <summary>
        /// Указатель типа запускаемого WebDriver'а
        /// False - локальный
        /// True - удаленный (RemoteWebDriver)
        /// Берется из App.Config
        /// </summary>
        public bool RemoteWd;

        /// <summary>
        /// Путь к сохранению файлов с браузера
        /// </summary>
        public static string FileSaveDir = @"C:\Web\Files";

        public string LogSaveDir
        {
            get { return _logSaveDir ?? PathCommands.SharedFolder + (Parallel ? "parallel\\" : "local\\"); }
            set { _logSaveDir = value; }
        }

        private string _logSaveDir;

        /// <summary>
        /// Адрес БД
        /// </summary>
        public static string SqlServer = "";
        /// <summary>
        /// Пользователь доступа к БД
        /// </summary>
        public static string SqlUser = "";
        /// <summary>
        /// Пароль доступа к БД
        /// </summary>
        public static string SqlPass = "";

        /// <summary>
        /// Количество потоков в параллельном запуске 
        /// </summary>
        public static int ThreadCount = 10; //лучше пусть совпадает с серверным
        /// <summary>
        /// Метка, что тест запускается в параллельном режиме
        /// (внутренний параметр, менять не нужно)
        /// </summary>
        public bool Parallel = false;
        public Guid? ParallelGuid = null;

        /// <summary>
        /// Название чек-листа для записи результатов прогона тестов
        /// </summary>
        public static string SpreadSheetName = "";
        /// <summary>
        /// Название вкладки чек-листа для записи результатов тестов
        /// </summary>
        public static List<string> WorkSheetName = new List<string>();
        /// <summary>
        /// Логин gmail с доступом к чек-листу в googleDocs
        /// </summary>
        public static string GoogleLogin = "";
        /// <summary>
        /// Пароль gmail с доступом к чек-листу в googleDocs
        /// </summary>
        public static string GooglePass = "";

        public static string GoogleHost = "imap.gmail.com";

        public static string GoogleServiceAccountEmail = "";
        public static string GoogleServiceAccountKey = "";

        public static List<int> DefaultLoadRuns = new List<int> { 1};

        /// <summary>
        /// Адрес почтового сервера
        /// </summary>
        public static string MailHost = "";
        /// <summary>
        /// Логин на почтовом сервере
        /// </summary>
        public static string MailLogin = "";
        /// <summary>
        /// Пароль логина на почтовом сервере
        /// </summary>
        public static string MailPass = "";
        /// <summary>
        /// Макс. время ожидания загрузки страницы (сек)
        /// </summary>
        public static int Time = 10;
        /// <summary>
        /// Макс. время ожидания загрузки Ajax (сек)
        /// </summary>
        public static int TimeJ = 30;
        /// <summary>
        /// Макс. время ожидания отклика от Http запроса (сек)
        /// </summary>
        public static int TimeMax = 120;
        /// <summary>
        /// Макс. время ожидания для вывода в логи
        /// </summary>
        public static int TimeBug = 20;
        /// <summary>
        /// Начало имени для сервера SeleniumRC
        /// </summary>
        public static string ServerName = "selenium-server-standalone";
        /// <summary>
        /// Версия используемого Firefox
        /// </summary>
        public static string VersionFf = "41.0.2";
        /// <summary>
        /// Делать ли проверку на 404 страницу
        /// </summary>
        public bool Check404 = true;

        /// <summary>
        /// Мыло, на которое по дефолту будет скидываться 
        /// результаты прогона тестов
        /// </summary>
        public static string DefaultMail = "zaharkov@test.ru";

        /// <summary>
        /// Адрес тестируемого сайта
        /// Берется из App.Config
        /// </summary>
        public string Address;
        /// <summary>
        /// Адрес хоста, где будут запускаться тесты
        /// Берется из App.Config
        /// </summary>
        public string Host;
        /// <summary>
        /// Порт хоста, где будут запускаться тесты
        /// Берется из App.Config
        /// </summary>
        public int Port;
    }
}
