using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AutoTest.helpers.Parameters
{
    public class ParametersInit
    {
        private static readonly string LocalConfigFileName;
        private static readonly Configuration LocalConfig;

        static ParametersInit()
        {
            LocalConfigFileName = GetAppConfigValue("LocalConfigFileName");
            var localConfigFileTemplateName = GetAppConfigValue("LocalConfigFileTemplateName");
            var localConfigPath = GetAppConfigValue("LocalConfigPath") + LocalConfigFileName;

            if (!File.Exists(localConfigPath))
                throw new ConfigurationErrorsException(string.Format("Файла {0} не существует. {1} Создайте его, скопировав содержимое {2}",
                        LocalConfigFileName, Environment.NewLine, localConfigFileTemplateName));

            var configFileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = localConfigPath
            };

            LocalConfig = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

            FileSaveDir = GetAppConfigValue("FileSaveDir");

            ThreadCount = int.Parse(GetAppConfigValue("ThreadCount"));

            SqlServer = GetLocalConfigValue("SqlServer", true);
            SqlUser = GetLocalConfigValue("SqlUser", true);
            SqlPass = GetLocalConfigValue("SqlPass", true);
            var sqlWindowsAuth = GetLocalConfigValue("SqlWindowsAuth", true);
            SqlWindowsAuth = !string.IsNullOrEmpty(sqlWindowsAuth) && bool.Parse(sqlWindowsAuth);

            SpreadSheetName = GetAppConfigValue("SpreadSheetName", true);
            var workSheetName = GetAppConfigValue("WorkSheetName", true);
            WorkSheetName = string.IsNullOrEmpty(workSheetName) ? new List<string>() : workSheetName.Split(';').ToList();
            GoogleLogin = GetAppConfigValue("GoogleLogin", true);
            GooglePass = GetAppConfigValue("GooglePass", true);
            GoogleHost = GetAppConfigValue("GoogleHost", true);
            GoogleServiceAccountEmail = GetAppConfigValue("GoogleServiceAccountEmail", true);
            GoogleServiceAccountKey = GetAppConfigValue("GoogleServiceAccountKey", true);

            MailLogin = GetAppConfigValue("MailLogin", true);
            MailPass = GetAppConfigValue("MailPass", true);
            MailHost = GetAppConfigValue("MailHost", true);
            DefaultMail = GetAppConfigValue("DefaultMail", true);

            FindElementTimeOut = int.Parse(GetAppConfigValue("FindElementTimeOut"));
            AjaxTimeOut = int.Parse(GetAppConfigValue("AjaxTimeOut"));
            WebDriverTimeOut = int.Parse(GetAppConfigValue("WebDriverTimeOut"));
            TimeOutForLog = int.Parse(GetAppConfigValue("TimeOutForLog"));

            ServerName = GetAppConfigValue("ServerName");
            VersionFf = GetAppConfigValue("VersionFf");
        }

        public ParametersInit()
        {
            RemoteWd = bool.Parse(GetLocalConfigValue("RemoteWd"));
            Address = GetLocalConfigValue("Address");
            Host = GetLocalConfigValue("Host");
            Port = int.Parse(GetLocalConfigValue("Port"));
            Login = Login.SetValue(GetLocalConfigValue("Login"));
            Login = Password.SetValue(GetLocalConfigValue("Pass"));

            Parallel = false;
            ParallelGuid = null;
            Check404 = true;
        }

        public static string GetLocalConfigValue(string name, bool canBeEmpty = false)
        {
            string value;
            try
            {
                value = LocalConfig.AppSettings.Settings[name].Value;
            }
            catch (KeyNotFoundException)
            {
                throw new ConfigurationErrorsException(string.Format("Параметр {0} не задан в {1}", name, LocalConfigFileName));
            }

            if (canBeEmpty)
                return value;
            
            if(string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format("Параметр {0} не должен быть пустым в {1}", name, LocalConfigFileName));

            return value;
        }

        private static string GetAppConfigValue(string name, bool canBeEmpty = false)
        {
            var value = ConfigurationManager.AppSettings.Get(name);

            if (canBeEmpty)
                return value;

            if (string.IsNullOrEmpty(value))
                throw new ConfigurationErrorsException(string.Format("Параметр {0} не задан в App.config", name));

            return value;
        }

        public readonly ParamField Login = new ParamField("//login");
        public readonly ParamField Password = new ParamField("//pass");

        /// <summary>
        /// Указатель типа запускаемого WebDriver'а
        /// False - локальный
        /// True - удаленный (RemoteWebDriver)
        /// </summary>
        public bool RemoteWd;

        /// <summary>
        /// Путь к сохранению файлов
        /// </summary>
        public static string FileSaveDir;

        public string LogSaveDir
        {
            get { return _logSaveDir ?? PathCommands.SharedFolder + (Parallel ? @"parallel\" : @"local\"); }
            set { _logSaveDir = value; }
        }

        private string _logSaveDir;

        /// <summary>
        /// Адрес БД
        /// </summary>
        public static string SqlServer;
        /// <summary>
        /// Пользователь доступа к БД
        /// </summary>
        public static string SqlUser;
        /// <summary>
        /// Пароль доступа к БД
        /// </summary>
        public static string SqlPass;
        /// <summary>
        /// Пароль доступа к БД
        /// </summary>
        public static bool SqlWindowsAuth;

        /// <summary>
        /// Количество потоков в параллельном запуске 
        /// </summary>
        public static int ThreadCount; //лучше пусть совпадает с серверным
        /// <summary>
        /// Метка, что тест запускается в параллельном режиме
        /// (внутренний параметр, менять не нужно)
        /// </summary>
        public bool Parallel;
        public Guid? ParallelGuid;

        /// <summary>
        /// Название чек-листа для записи результатов прогона тестов
        /// </summary>
        public static string SpreadSheetName;
        /// <summary>
        /// Название вкладки чек-листа для записи результатов тестов
        /// </summary>
        public static List<string> WorkSheetName;
        /// <summary>
        /// Логин gmail с доступом к чек-листу в googleDocs
        /// </summary>
        public static string GoogleLogin;
        /// <summary>
        /// Пароль gmail с доступом к чек-листу в googleDocs
        /// </summary>
        public static string GooglePass;

        public static string GoogleHost;

        public static string GoogleServiceAccountEmail;
        public static string GoogleServiceAccountKey;

        /// <summary>
        /// Адрес почтового сервера
        /// </summary>
        public static string MailHost;
        /// <summary>
        /// Логин на почтовом сервере
        /// </summary>
        public static string MailLogin;
        /// <summary>
        /// Пароль логина на почтовом сервере
        /// </summary>
        public static string MailPass;
        /// <summary>
        /// Мыло, на которое по дефолту будет скидываться 
        /// результаты прогона тестов
        /// </summary>
        public static string DefaultMail;
        /// <summary>
        /// Макс. время поиска элемента (сек)
        /// </summary>
        public static int FindElementTimeOut;
        /// <summary>
        /// Макс. время ожидания загрузки Ajax (сек)
        /// </summary>
        public static int AjaxTimeOut;
        /// <summary>
        /// Макс. время ожидания отклика от Http запроса (сек)
        /// </summary>
        public static int WebDriverTimeOut;
        /// <summary>
        /// Макс. время ожидания для вывода в логи
        /// </summary>
        public static int TimeOutForLog;
        /// <summary>
        /// Начало имени для сервера SeleniumRC
        /// </summary>
        public static string ServerName;
        /// <summary>
        /// Версия используемого Firefox
        /// </summary>
        public static string VersionFf;
        /// <summary>
        /// Делать ли проверку на 404 страницу
        /// </summary>
        public bool Check404;

        /// <summary>
        /// Адрес тестируемого сайта
        /// </summary>
        public string Address;
        /// <summary>
        /// Адрес хоста, где будут запускаться тесты
        /// </summary>
        public string Host;
        /// <summary>
        /// Порт хоста, где будут запускаться тесты
        /// </summary>
        public int Port;
    }
}
