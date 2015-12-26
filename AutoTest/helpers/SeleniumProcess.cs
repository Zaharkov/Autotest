using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTest.helpers
{
    /// <summary>
    /// Класс для удаленного управления
    /// Сделан из отчаяния, боли и страданий создателя.
    /// Если что-то сломается...бери бубен и танцуй, ничто другое не спасет
    /// </summary>
    internal static class SeleniumProcess
    {
        #region Fields
        /// <summary>
        /// Пользователь, от имени которого выполнять команды
        /// </summary>
        private static readonly string User = ParametersInit.GetAppConfig("User");
        /// <summary>
        /// Пароль пользователя
        /// </summary>
        private static readonly string Pass = ParametersInit.GetAppConfig("Pass");
        /// <summary>
        /// Домен в котором находится пользователь
        /// </summary>
        private static readonly string Domain = ParametersInit.GetAppConfig("Domain");
        private static readonly string DomainAndUser = Domain + "\\" + User;
        /// <summary>
        /// Дефолтный порт для WsMan
        /// </summary>
        private const int WsManPort = 5985;
        private const string AppName = "/wsman";
        private const string ShellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

        private const int DefaultHubPort = 4444;//задан в HUB.json
        #endregion

        #region Execute

        #region авторизация (New-Object System.Management.Automation.PsCredential)
        /// <summary>
        /// Создание среды выполнения для команды
        /// с указанием авторизационных данных
        /// </summary>
        /// <param name="host">имя компа к которому создается среда выполнения</param>
        /// <returns></returns>
        private static Runspace GetRunspace(string host)
        {
            //идея простая - нужно сделать разрешения на текущем компе (клиенте) и
            //компах, где будут выполняться удаленные команды (сервере), для powershell'a (кароч сначала вводи "winrm qc")
            //и расшарить аутентификацию по credssp для клиента (в режиме админа: Enable-WsManCredspp -role client -delegatecomputer * -force)
            //и для серверов (Enable-WsManCredspp -role server)
            //после этого, достаточно будет авторизационных данных, чтобы все команды работали автоматом без всяких проблем с "залогинтесь %username%"
            //ну то есть в HiddenSettings должны быть правильно заданы User, Pass, Domain (ясен пень у юзера должно быть разрешение на удаленное управление на сервере)
            
            var pass = ConvertToSecureString(Pass);
            var psCredential = new PSCredential(DomainAndUser, pass);
            var connection = new WSManConnectionInfo(false, host, WsManPort, AppName, ShellUri, psCredential)
            {
                AuthenticationMechanism = AuthenticationMechanism.Credssp,
                Culture = CultureInfo.GetCultureInfo("en-US"),
                UICulture = CultureInfo.GetCultureInfo("en-US")
            };

            var runspace = RunspaceFactory.CreateRunspace(connection);//Если тут падает - установи пакет KB2819745
            runspace.Open();
            return runspace;
        }
        #endregion

        #region удаленная сессия (New-PSSession)
        /// <summary>
        /// Выполнение команды powershell с созданием среды выполнения
        /// </summary>
        /// <param name="host">имя компа</param>
        /// <param name="cmd">команда</param>
        /// <param name="psexec">используется ли psexec?</param>
        /// <returns></returns>
        private static string PowerShellCmd(string host, string cmd, bool psexec = false)
        {
            using (var runspace = GetRunspace(host))
            {
                //выполняем команду в созданой ранее среде выполнения
                return PowerShellCmd(runspace, cmd, psexec);
            }
        }
        #endregion

        #region выполнение команды (invoke-command)
        /// <summary>
        /// Выполнение powershell команды
        /// </summary>
        /// <param name="runspace">среда выполнения</param>
        /// <param name="cmd">команда</param>
        /// <param name="psexec">запуск через psexec</param>
        /// <returns></returns>
        private static string PowerShellCmd(Runspace runspace, string cmd, bool psexec = false)
        {
            // когда-то это работало так ...
            //if (IsLocalHost(host))
            //    cmd = "invoke-command {" + cmd + "}";
            //else
            //{
            //    cmd = "$password = ConvertTo-SecureString \"" + ParametersInit.GetAppConfig("Pass") + "\" -AsPlainText -Force; " +
            //          "$credential = New-Object System.Management.Automation.PsCredential(\"" + ParametersInit.GetAppConfig("Domain") + "\\" + ParametersInit.GetAppConfig("User") + "\", $password); " +
            //          "$session = New-PSSession -authentication credssp -credential $credential -computerName " + host + ";" +
            //          "invoke-command -session $session {" + cmd + "};" +
            //          "Remove-PSSession -session $session;";
            //}
           
            using (var ps = PowerShell.Create())
            {
                ps.Runspace = runspace;
                //может показатся странным (так и есть о_О)
                //почему то chcp работает нормально только в среде выполнения cmd 
                //и да он тут нужен - кодовая страница на компах может отличаться, и ответ будет приходит на разных языках, что не очень хорошо
                ps.AddScript("cmd /c chcp 437 \">\" nul \"&\" powershell -outputformat text -command { " + cmd + " }");
                var output = ps.Invoke();

                var errors = ps.Streams.Error.ReadAll();
                //получаем ошибки
                if (ps.HadErrors)
                {
                    //psexec возвращает "обычный" ответ как ошибку (return code == 1)
                    //поэтому для него такой костыль =\
                    if (psexec)
                    {
                        return errors.Where(t => t != null).Aggregate("", (current, error) => current + error.Exception.Message + (errors.Count > 1 ? Environment.NewLine : null));
                    }

                    //роняем выполнение с первой же ошибкой
                    foreach (var ex in errors)
                    {
                        if (ex != null)
                        {
                            throw ex.Exception;
                        }
                    }
                }

                //возвращаем результат выполнения
                return output
                    .Where(psObject => psObject != null)
                    .Aggregate("", (current, psObject) => current + psObject.BaseObject + (output.Count > 1 ? Environment.NewLine : null));
            }  
        }
        #endregion

        /// <summary>
        /// Превращаем пароль в зашифрованную хрень
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();
            foreach (var c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();
            return securePassword;
        }

        /// <summary>
        /// Запуск процесса через PsExec 
        /// он нужен чтобы запускать процесс в интерактивной сессии пользователя (-i)
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cmd"></param>
        private static void PsExec(string host, string cmd)
        {
            var id = GetActiveSession(host);
            cmd = "-d -i " + id + " -u " + DomainAndUser + " -p " + Pass + " \\\\" + host + " -accepteula " + cmd;

            var result = PowerShellCmd(Environment.MachineName, PathCommands.SharedFiles + "psexec.exe " + cmd, true);

            if (!result.Contains("started on " + host + " with process ID"))
                throw new ProcessException(result, ExceptionType.PsExec);
        }
        
        /// <summary>
        /// Проверка является ли хост локалхостом
        /// </summary>
        /// <param name="host">Хост</param>
        /// <returns>Да/Нет</returns>
        private static bool IsLocalHost(string host)
        {
            if(host == "localhost")
                return true;

            return Environment.MachineName == host.ToUpper();
        }
        #endregion

        #region Share
        /// <summary>
        /// Проверка расшарен ли путь с нужным именем на текущей машине
        /// </summary>
        /// <param name="name">имя шары</param>
        /// <param name="path">локальный путь к шаре</param>
        /// <param name="onlyName">проверять только имя</param>
        /// <returns></returns>
        private static bool IsShared(string name, string path, bool onlyName = false)
        {
            var result = PowerShellCmd(Environment.MachineName, 
                "$files = gwmi -q \"SELECT * FROM Win32_Share WHERE Name = '" + name +
                "' AND Path = '" + path.Replace("\\", "\\\\") + "'\"; $files.Name" + (onlyName ? "" : " + \" \" + $files.Path"));

            return result.Contains(name + (onlyName ? "" : " " + path));
        }

        /// <summary>
        /// Удалить шару с указанным именем
        /// </summary>
        /// <param name="name">имя шары</param>
        private static void DeleteShare(string name)
        {
            var cmd = "net share " + name + " /delete /y";
            var result = PowerShellCmd(Environment.MachineName, cmd);

            if (!result.Contains(name + " was deleted successfully"))
                throw new ProcessException(result, ExceptionType.Share);
        }

        /// <summary>
        /// Расшарить (или ничего не делать если уже расшарен)
        /// папку на текущем компе под указанным именем
        /// </summary>
        /// <param name="name">имя шары</param>
        /// <param name="path">путь к шаре</param>
        public static void ShareFolder(string name, string path)
        {
            PathCommands.CreateDir(path);

            if (!IsShared(name, path))
            {
                if (IsShared(name, path, true))
                    DeleteShare(name);

                var cmd = "net share " + name + "=\"" + path + "\"";
                var result = PowerShellCmd(Environment.MachineName, cmd);

                if (!result.Contains(name + " was shared successfully"))
                    throw new ProcessException(result, ExceptionType.Share);
            }
        }
        #endregion

        #region Process
        /// <summary>
        /// Получить запущенный процесс со всеми ключами запуска
        /// </summary>
        /// <param name="host">Хост</param>
        /// <param name="procName">Название процесса</param>
        /// <returns>Запущенный процесс со всеми ключами запуска или "" если такого нет</returns>
        private static string GetProcess(string host, string procName)
        {
            var cmd = "get-wmiobject  win32_process -filter \"name like '%" + procName + ".exe'\" | select -expand commandLine";
            var result = PowerShellCmd(host, cmd);

            return result;
        }

        /// <summary>
        /// Остановить процесс с указанным именем
        /// </summary>
        /// <param name="host">Хост</param>
        /// <param name="name">Имя процесса</param>
        private static void StopProcess(string host, string name)
        {
            if (GetProcess(host, name) == "")
                return;

            var result = PowerShellCmd(host, "stop-Process -force -processname " + name);

            if (result.Contains("java"))
                Thread.Sleep(5000);

            if (GetProcess(host, name) != "")
                throw new ProcessException("Process " + name + " won't stop" + Environment.NewLine + result, ExceptionType.Process);
        }
        #endregion

        #region Firefox
        /// <summary>
        /// Проверка версии Firefox на хосте
        /// </summary>
        /// <param name="host">Хост</param>
        private static void CheckFirefoxVersion(string host)
        {
            if(!IsLocalHost(host) && !CheckFfVersion(host))
                UpdateFirefox(host);
        }

        /// <summary>
        /// Проверить совпадает ли версия Firefox с указанной
        /// </summary>
        /// <param name="host">Хост</param>
        /// <returns>Да/Нет</returns>
        private static bool CheckFfVersion(string host)
        {
            var result = PowerShellCmd(host, "cmd /c \"${Env:ProgramFiles(x86)}\\Mozilla Firefox\\firefox\" -v | more");
            return result.Contains(ParametersInit.VersionFf);
        }

        /// <summary>
        /// Удаление из папки Temp временных профилей Firefox
        /// </summary>
        /// <param name="host">имя компа</param>
        private static void DelTempDir(string host)
        {
            if (!IsLocalHost(host))
            {
                PowerShellCmd(host, "Remove-Item \"$Env:Temp\\*userprofile*\" -Force -Recurse -ErrorAction SilentlyContinue");
                PowerShellCmd(host, "Remove-Item \"$Env:Temp\\*webdriver*\" -Force -Recurse -ErrorAction SilentlyContinue");
            }
        }

        /// <summary>
        /// Обновить версию Firefox до заранее установленной
        /// </summary>
        /// <param name="host">Хост</param>
        private static void UpdateFirefox(string host)
        {
            var result = PowerShellCmd(host, PathCommands.SharedFiles + "updateFF.exe /S | Out-Null");

            if(!string.IsNullOrEmpty(result))
                throw new ProcessException(result, ExceptionType.Firefox);

            if(!CheckFfVersion(host))
                throw new ProcessException("updateFF not update firefox", ExceptionType.Firefox);
        }
        #endregion

        #region Selenium
        /// <summary>
        /// Подготовка процесса на хосте
        /// </summary>
        /// <param name="host">Хост (имя компа в сети)</param>
        /// <param name="role">Роль хоста (локальный == "", хаб, нод)</param>
        /// <param name="hubHost">hub</param>
        public static void PrepareProcess(string host, ProcessType role = ProcessType.StandAlone, string hubHost = null)
        {
            ShareFolder("Files", ParametersInit.FileSaveDir);
            CheckFirefoxVersion(host);
            CheckSeleniumProcess(host, role, hubHost);
        }

        /// <summary>
        /// Получить номер активной сессии на удаленном хосте
        /// </summary>
        /// <param name="host">Хост</param>
        /// <returns>Номер сессии</returns>
        private static string GetActiveSession(string host)
        {
            var cmd = "query session | Select-String \"" + User + "\\s+(\\w+)\" | Foreach {$_.Matches[0].Groups[1].Value}";

            var result = PowerShellCmd(host, cmd);

            if(string.IsNullOrEmpty(result))
                result = CreateActiveSession(host);

            int numberSession;
            if (int.TryParse(result, out numberSession))
                return numberSession.ToString();

            throw new ProcessException("Создание сессии вернуло хрень: " + result, ExceptionType.Session);
        }

        /// <summary>
        /// Создать активную интерактивную сессию пользователя
        /// </summary>
        /// <param name="host">имя компа</param>
        /// <returns></returns>
        private static string CreateActiveSession(string host)
        {
            //4 дня....четыре ебучих дня, карл, у меня ушло на то, чтобы понять как это сделать
            //боже упаси тут что-то менять
            using (var runspace = RunspaceFactory.CreateRunspace()) //нужна именно такая (локальная) среда выполнения. В созданной обычным способом cmdkey и mstsc не работают
            {
                runspace.Open();
                //создаем пару домаин:юзер + пароль чтобы RDP открылся автоматически
                PowerShellCmd(runspace, "cmdkey.exe /generic:TERMSRV/" + host + " /user:" + DomainAndUser + " /pass:" + Pass);
                //открываем RDP как отдельный процесс со скрытым окном (чтобы никто не увидел мвахахаха)
                var mstscId = PowerShellCmd(runspace, "$mstsc = Start-Process -PassThru -WindowStyle Hidden \"mstsc\" -ArgumentList \"/v:" + host + " /admin\"; $mstsc.Id;");
                //чуток ждем чтобы интерактивная сессия ожила
                Thread.Sleep(15000);
                //нужно...почему-то если сессию оставить просто так - она умирает через некоторое время
                //поетому кидаем туда калькулятор который её "закрепляет"
                PsExec(host, "calc.exe");
                //чуток ждем чтобы сессия "закрепилась"
                Thread.Sleep(15000);
                //вырубаем калькулятор
                StopProcess(host, "calc");
                //вырубаем RDP
                PowerShellCmd(runspace, "stop-process " + mstscId);
            }

            //проверяем что сессия все же создалась
            var cmd = "query session | Select-String \"" + User + "\\s+(\\w+)\" | Foreach {$_.Matches[0].Groups[1].Value}";
            var result = PowerShellCmd(host, cmd);

            if(string.IsNullOrEmpty(result))
                throw new ProcessException("Не смог создать сессию", ExceptionType.Session);

            return result;
        }

        /// <summary>
        /// Проверка процесса seleniumRC на хосте
        /// </summary>
        /// <param name="host">Хост</param>
        /// <param name="role">Роль сервера (локальный, хаб, нод)</param>
        /// <param name="hubHost">hub</param>
        /// <returns>Успешно/Неуспешно</returns>
        private static void CheckSeleniumProcess(string host, ProcessType role, string hubHost)
        {
            var result = GetProcess(host, "javaw");
            var grid = role != ProcessType.StandAlone 
                ? (role == ProcessType.Hub 
                    ? "-role hub" 
                    : "-hub http://" + role + ":" + DefaultHubPort + "/grid/register") 
                : " ";

            if (result.Contains("javaw") && result.Contains(FindSeleniumRc()))
            {
                if (!(result.Contains(grid) || role == ProcessType.StandAlone) ||
                    (role == ProcessType.StandAlone && result.Contains("-role")))
                {
                    StopProcess(host, "javaw");
                    RunSeleniumRc(host, role, hubHost);
                }
            }
            else
            {
                if (result != "")
                    StopProcess(host, "javaw");

                RunSeleniumRc(host, role, hubHost);
            }
        }

        /// <summary>
        /// Запуск сервера seleniumRC на хосте
        /// </summary>
        /// <param name="host">Хост</param>
        /// <param name="role">Роль сервера (локальный, хаб, нод)</param>
        /// <param name="hubHost">хаб</param>
        private static void RunSeleniumRc(string host, ProcessType role, string hubHost)
        {
            //TODO сделать установку JRE если java не установлена
            var path = GetProgramPath(host, "javaw");
            CheckFireWallRule(host, "javaw", path);

            var cmdGrid = "";
            if (role != ProcessType.StandAlone)
            {
                if (role == ProcessType.Hub)
                    cmdGrid = " -role hub -hubConfig " + PathCommands.SharedFiles + "HUB.json";
                else
                    cmdGrid = " -role node -nodeConfig " + PathCommands.SharedFiles + "NODE.json -hub http://" + hubHost + ":" + DefaultHubPort + "/grid/register";
            }

            var cmd = "\"" + path + "\" -jar " + FindSeleniumRc() + cmdGrid;

            PsExec(host, cmd);
            Thread.Sleep(10000);

            var result = GetProcess(host, "javaw");

            if(string.IsNullOrEmpty(result))
                throw new ProcessException("Process javaw not start", ExceptionType.Process);
        }

        /// <summary>
        /// Получить полное имя сервера SeleniumRC (вместе с версией)
        /// </summary>
        /// <returns>Полное имя сервера SeleniumRC</returns>
        private static string FindSeleniumRc()
        {
            var files = Directory.GetFiles(PathCommands.SharedFiles);
            string seleniumRc = null;

            foreach (var file in files)
            {
                var fileName = file.Replace(PathCommands.SharedFiles, "");

                if (fileName.Count() > ParametersInit.ServerName.Count())
                    if (fileName.Substring(0, ParametersInit.ServerName.Count()) == ParametersInit.ServerName)
                        seleniumRc = file;
            }

            if (seleniumRc == null)
                throw new NullReferenceException("Не найден " + ParametersInit.ServerName);

            return seleniumRc;
        }
        #endregion

        #region FireWall

        /// <summary>
        /// Проверить правило для брандмауера
        /// </summary>
        /// <param name="host">хост</param>
        /// <param name="name">имя программы</param>
        /// <param name="path"></param>
        private static void CheckFireWallRule(string host, string name, string path)
        {
            SetFireWallRule(host, name, path);
        }

        /// <summary>
        /// Получить путь к программе
        /// </summary>
        /// <param name="host">хост</param>
        /// <param name="name">имя программы</param>
        /// <returns></returns>
        private static string GetProgramPath(string host, string name)
        {
            var res = PowerShellCmd(host, "$command = get-command " + name + "; $command.FileVersionInfo.FileName");
            //косяк в том, что полученный файл может быть symlink (та ещё задница)
            //написал getPath.exe который вытаскивает реальный путь к файлу
            var res2 = PowerShellCmd(host, "cmd /c " + PathCommands.SharedFiles + "getPath.exe \"" + res + "\"");

            if (string.IsNullOrEmpty(res2))
                throw new ProcessException("Not found program '" + name + "'", ExceptionType.ProgramPath);

            return res2;
        }

        /// <summary>
        /// Удалить правило брандмауера для программы
        /// </summary>
        /// <param name="host">хост</param>
        /// <param name="name">имя программы</param>
        private static void DeleteFireWallRule(string host, string name)
        {
            var res = PowerShellCmd(host, "netsh advfirewall firewall delete rule name=" + name);

            if (res.Contains("No rules match the specified criteria") ||
                (res.Contains("Deleted ") && res.Contains(" rule(s)." + Environment.NewLine + "Ok.")))
                return;

            throw new ProcessException("Not delete rule:" + res, ExceptionType.FireWall);
        }

        /// <summary>
        /// Добавить правило брандмауера для программы
        /// </summary>
        /// <param name="host">хост</param>
        /// <param name="name">имя программы</param>
        /// <param name="path">путь к программе</param>
        private static void AddFireWallRule(string host, string name, string path)
        {
            var res = PowerShellCmd(host, "netsh advfirewall firewall add rule name=\"" + name + "\" profile=domain dir=in program=\"" + path + "\" action=allow");

            if (res.Contains("Ok."))
                return;

            throw new ProcessException("Not add rule:" + res, ExceptionType.FireWall);
        }

        /// <summary>
        /// Задать правило брандмауера (если его нету) для программы
        /// </summary>
        /// <param name="host">хост</param>
        /// <param name="name">имя программы</param>
        /// <param name="path">путь к программе</param>
        private static void SetFireWallRule(string host, string name, string path)
        {
            var result = PowerShellCmd(host, "netsh advfirewall firewall show rule name=\"" + name + "\" profile=domain dir=in verbose");
            var getRules = result.Split(new []{"----------------------------------------------------------------------"}, StringSplitOptions.None);

            if (getRules.Select(rule => rule.Replace(" ", "")).Any(clearRule => clearRule.Contains("Program:" + path)))
                return;

            DeleteFireWallRule(host, name);
            AddFireWallRule(host, name, path);
        }

        #endregion

        #region Parallels
        /// <summary>
        /// Закрыть 
        /// процессы Firefox, 
        /// зависшие браузеры Firefox'а (WerFault),
        /// запущенный процесс печати в onenote,
        /// почистить папку temp 
        /// </summary>
        /// <param name="hubHolder"></param>
        public static void CleanVirtuals(HubHolder hubHolder)
        {
            DoInThread((key, value) =>
            {
                StopProcess(value, "firefox");
                StopProcess(value, "WerFault");
                StopProcess(value, "ONENOTE");
                DelTempDir(value);
            }, hubHolder.Nodes);
        }

        /// <summary>
        /// Перезапуск процессов
        /// </summary>
        /// <param name="hubHolder"></param>
        public static void ReloadParallel(HubHolder hubHolder)
        {
            var hub = hubHolder.Clone();
            hub.AddNode(hub.Hub);

            DoInThread((key, value) =>
            {
                StopProcess(value, "firefox");
                StopProcess(value, "WerFault");
                StopProcess(value, "ONENOTE");
                StopProcess(value, "javaw");
                DelTempDir(value);
            }, hub.Nodes);

            PrepareParallel(hubHolder);
        }

        /// <summary>
        /// Перезагрузить ноды
        /// </summary>
        /// <param name="hubHolder"></param>
        public static void ReloadVirtuals(HubHolder hubHolder)
        {
            DoInThread((key, value) =>
            {
                PowerShellCmd(value, "shutdown -r -t 0");
                Thread.Sleep(5000);
                Exception ex = null;
                var sw = Stopwatch.StartNew();
                var notSuccessful = true;
                while (notSuccessful && sw.Elapsed.TotalSeconds < ParametersInit.TimeMax * 5)
                {
                    try
                    {
                        PowerShellCmd(value, "echo 1");
                        notSuccessful = false;
                    }
                    catch (PSRemotingTransportException e)
                    {
                        ex = e;
                    }
                }

                if (notSuccessful && ex != null)
                    throw ex;

            }, hubHolder.Nodes);
        }

        /// <summary>
        /// Подготовить процессы SeleniumRC на удаленных машинах
        /// </summary>
        /// <param name="hubHolder">Контейнер хаб+ноды</param>
        public static void PrepareParallel(HubHolder hubHolder)
        {
            var hub = hubHolder.Hub;
            var nodes = hubHolder.Nodes;
            var param = new Dictionary<string, ProcessType> {{hub, ProcessType.Hub}};
            
            foreach (var node in nodes)
                param.Add(node.Value, ProcessType.Node);

            DoInThread((key, value) => PrepareProcess(key, value, value == ProcessType.Node ? hub : null), param);
        }

        /// <summary>
        /// Выполнение процедур для нодов в параллельном режиме
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="action">что нужно сделать</param>
        /// <param name="dic">список нодов</param>
        private static void DoInThread<TKey, TValue>(Action<TKey, TValue> action, Dictionary<TKey, TValue> dic)
        {
            var tasks = dic.Select(kv => Task.Run(() => action(kv.Key, kv.Value))).ToArray();
            Task.WaitAll(tasks);
        }
        #endregion
    }

    #region HubHolder and ProcessException
    /// <summary>
    /// Контейнер хаб+ноды
    /// </summary>
    public class HubHolder
    {
        public string Hub;
        public Dictionary<int, string> Nodes = new Dictionary<int, string>();

        public HubHolder(string hub, Dictionary<int, string> nodes = null)
        {
            Hub = hub;

            if(nodes != null)
                Nodes = nodes;
        }

        /// <summary>
        /// Добавить нод в контейнер
        /// </summary>
        /// <param name="host"></param>
        public HubHolder AddNode(string host)
        {
            Nodes[Nodes.Count] = host;
            return this;
        }

        public HubHolder Clone()
        {
            return new HubHolder(Hub, new Dictionary<int, string>(Nodes));
        }
    }

    public enum ProcessType
    {
        StandAlone,
        Hub,
        Node
    }

    public enum ExceptionType
    {
        PsExec,
        Share,
        Process,
        Firefox,
        Session,
        ProgramPath,
        FireWall
    }

    [Serializable]
    public class ProcessException : ApplicationException
    {
        public ExceptionType Type;

        public ProcessException(ExceptionType type)
        {
            Type = type;
        }

        public ProcessException(string message, ExceptionType type) : base(message)
        {
            Type = type;
        }

        public ProcessException(string message, ExceptionType type, Exception inner)
            : base(message, inner)
        {
            Type = type;
        }

        protected ProcessException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    #endregion
}