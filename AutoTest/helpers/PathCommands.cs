using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Build.Evaluation;
using NUnit.Framework;
using AutoTest.helpers.Parameters;
using AutoTest.helpers.Selenium;

namespace AutoTest.helpers
{
    /// <summary>
    /// Класс для работы с файлами тестов
    /// </summary>
    public static class PathCommands
    {
        /// <summary>
        /// Список из названий классов тестов и информации о них
        /// </summary>
        private static readonly Project Project;
        /// <summary>
        /// Проект автотестов - для переименовки
        /// </summary>
        private static Microsoft.Build.Evaluation.Project _msProject;
        /// <summary>
        /// Список содержимого в проекте _msProject
        /// </summary>
        private static ICollection<ProjectItem> _msItems;
        /// <summary>
        /// Нумерация для переименовки
        /// </summary>
        private static int _count = 1;

        /// <summary>
        /// Формат для названия файлов при переименовке
        /// 0000 - когда тестов больше 999...
        /// то есть его нужно будет поменять на 00000, когда станет ДЕСЯТЬ ТЫСЯЧ СПАРТАНЦЕВ xD
        /// </summary>
        private const string Format = "000";

        private static readonly Regex NoNumbers = new Regex(@"[^\d]");
        private static readonly Regex BeginOfFile = new Regex(@"^\d+(?(?=_{1})_\d*)");
        private static readonly Regex ClassName = new Regex(@"([\D]+)(\d+(?(?=_{1})_\d*))");

        static PathCommands()
        {
            //получаем архитектуру тестов
            using (var sr = new StreamReader(@"testInfo.xml"))
            {
                var xml = sr.ReadToEnd();
                var serializer = new XmlSerializer(typeof (Project));
                
                using (TextReader reader = new StringReader(xml))
                {
                    Project = (Project)serializer.Deserialize(reader);
                }
            }
            //выгружаем список тестов в сборке
            var typelist = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttributes(typeof(AutoTestAttribute), false).Any()).ToArray();
            //обрабатываем список в зависимости от начальных параметров (ParametersProject)
            LoadTestsInfo(Project.Dir, typelist, ProjectType.None);
            //проверяем что у всех заданы неповторяющиеся гуиды
            CheckValidDdInfo();
            //проверяем расшаренную папку и копируем общие файлы
            ShareAndCopy();
        }
        /// <summary>
        /// Папка в которой брать "изолированные" файлы - внутренние
        /// </summary>
        public static string IsolateFolder = Environment.CurrentDirectory + "\\Files\\";
        private static string _sharedFolder;
        /// <summary>
        /// Путь к расшаренной папке
        /// </summary>
        public static string SharedFolder
        {
            get
            {
                WaitForShare();
                return _sharedFolder;
            }
            set { _sharedFolder = value; }
        }
        private static string _sharedFiles;
        /// <summary>
        /// Путь к расшаренным файлам
        /// </summary>
        public static string SharedFiles
        {
            get
            {
                WaitForShare();
                return _sharedFiles;
            }
            set { _sharedFiles = value; }
        }
        private static Task _taskForShareAndCopy;

        private static void ShareAndCopy()
        {
            _taskForShareAndCopy = Task.Run(() =>
            {
                _sharedFolder = "\\\\" + Environment.MachineName + "\\Files\\";

                if (!Directory.Exists(_sharedFolder))
                {
                    //SeleniumProcess.ShareFolder("Files", ParametersInit.FileSaveDir);
                    throw new PathException("На данном компьютере должна быть расшарена папка с именем 'Files'" + Environment.NewLine +
                        "Желательно по пути: " + ParametersInit.FileSaveDir);
                }

                _sharedFiles = _sharedFolder + "FilesToShare\\";
                Directory.CreateDirectory(_sharedFiles);

                //Копирую содержимое папки Files (проекта) в FileSaveDir - оттуда будут браться файлы для загрузки на сервер
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = "cmd",
                    Arguments = "/c xcopy \"" + Environment.CurrentDirectory + "\\FilesToShare" + "\" \"" + _sharedFiles + "\" /S /Y",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = new Process
                {
                    StartInfo = startInfo
                };

                process.Start();
                process.WaitForExit();
            });
        }

        private static void WaitForShare()
        {
            _taskForShareAndCopy.Wait();

            if(_taskForShareAndCopy.IsFaulted)
                throw new PathException("Расшаривание и копирование упало", _taskForShareAndCopy.Exception);
        }

        /// <summary>
        /// Получить путь к проекту
        /// </summary>
        /// <returns>Путь к проекту</returns>
        public static string GetProjectDir()
        {
            return Project.ProjectDir;
        }

        /// <summary>
        /// Переименовка номеров тестов
        /// </summary>
        /// <param name="dir">Путь к директории тестов</param>
        public static void RenameTests(TestDir dir = null)
        {
            if (dir == null)
                dir = Project.Dir;

            var firstTime = false;
            if (_msProject == null)
            {
                _msProject = new Microsoft.Build.Evaluation.Project(Project.ProjectPath);
                _msItems = _msProject.GetItems("Compile");
                firstTime = true;
            }

            foreach (var dirIn in dir.Dirs)
            {
                RenameTests(dirIn);
            }

            var testsName = new Dictionary<string, KeyValuePair<string, string>>();
            foreach (var file in dir.Files)
            {
                var item = _msItems.First(t => t.EvaluatedInclude.Contains(file.Name));
                var path = Project.ProjectDir + item.EvaluatedInclude;

                using (var sr = new StreamReader(path))
                {
                    var source = sr.ReadToEnd();
                    var number = _count.ToString(Format);
                    var matchClassName = ClassName.Match(file.ClassName);
                    var matchBeginOfFile = BeginOfFile.Match(file.Name);

                    var newClass = matchClassName.Groups[1].Value + number;
                    var newFileName = file.Name.Replace(matchBeginOfFile.Value, number);
                    var newFilePath = path.Replace(file.Name, newFileName);

                    _count++;

                    if (file.ClassName == newClass && path == newFilePath)
                        continue;
                    
                    source = source.Replace(file.ClassName, newClass);
                    testsName[newFilePath] = new KeyValuePair<string, string>(path, source);
                    item.Rename(item.EvaluatedInclude.Replace(file.Name, newFileName));
                }
            }

            foreach (var valuePair in testsName)
            {
                File.WriteAllText(valuePair.Value.Key, valuePair.Value.Value);

                if (valuePair.Value.Key != valuePair.Key)
                {
                    var move = false;
                    var sw = Stopwatch.StartNew();
                    while (!move)
                    {
                        try
                        {
                            File.Move(valuePair.Value.Key, valuePair.Key);
                            move = true;
                        }
                        catch (IOException)
                        {
                            if (sw.Elapsed.TotalSeconds > 10)
                                throw;

                            move = false;
                        }
                    }
                }
            }

            if (firstTime)
            {
                _msProject.Save();
                _msProject.Build();
            }
        }

        /// <summary>
        /// Загрузка информации о тестах
        /// </summary>
        /// <param name="dir">Директория с тестами</param>
        /// <param name="typelist"></param>
        /// <param name="projectType"></param>
        private static void LoadTestsInfo(TestDir dir, Type[] typelist, ProjectType projectType)
        {
            foreach (var file in dir.Files)
            {
                var matchBeginOfFile = BeginOfFile.Match(file.Name);

                if (!matchBeginOfFile.Success)
                    throw new PathException("Тест " + file.Name + " имеет название, отличное от стандарного");

                var matchClassName = ClassName.Match(file.ClassName);

                if (!matchClassName.Success)
                    throw new PathException("Тест " + file.Name + " имеет класс, отличный от стандарного");

                var classNumber = matchClassName.Groups[2].Value;

                if (classNumber != matchBeginOfFile.Value)
                    throw new PathException("Тест " + file.Name + " имеет номер отличный от номера класса: " + classNumber);

                var number = NoNumbers.Replace(file.ClassName, "");
                var type = typelist.Where(t => t.Name == file.ClassName).ToArray().First();
                var targetObject = Activator.CreateInstance(type) as SeleniumCommands;

                var attr = type.GetCustomAttributes(typeof(AutoTestAttribute), false).Cast<AutoTestAttribute>().First();

                var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(t => t.GetCustomAttributes(typeof(TestAttribute), false).Any()).ToArray();

                if (!methods.Any())
                    throw new PathException("Тест " + number + " не имеет метода с атрибутом Test");

                file.Number = number;
                file.ProjectType = projectType;
                file.Attr = attr;
                file.Guids.RemoveAll(t => t.Value == attr.Id);
                file.ClassObject = targetObject;
                file.MethodInfos = methods;
            }

            foreach (var dirIn in dir.Dirs)
            {
                var newType = projectType;
                if (projectType == ProjectType.None)
                {
                    foreach (var project in AllProjects.List)
                    {
                        TestList testList;
                        if (project.Value.TestList.IsPathInList(dirIn.Name, out testList))
                            newType = project.Key;
                    }
                }
                LoadTestsInfo(dirIn, typelist, newType);
            }
        }

        /// <summary>
        /// Проверка что у загруженных тестов задан корректный GUID
        /// </summary>
        private static void CheckValidDdInfo()
        {
            var ids = new Dictionary<Guid, string>();

            foreach (var test in Project.GetTests())
            {
                var className = test.ClassName;
                var id = test.Attr.Id;

                if (ids.ContainsKey(id))
                    throw new ArgumentException("У теста " + className + " не уникальный GUID: совпадает с " + ids[id] + Environment.NewLine + 
                        "(возьми этот: '" + Guid.NewGuid().ToString().ToUpper() + "'");

                ids.Add(id, className);
            }
        }

        /// <summary>
        /// Получить список тестов по входному проекту
        /// </summary>
        /// <param name="project">Информация проекта</param>
        /// <returns>Список тестов</returns>
        public static List<TestInfo> GetTests(ParametersProject project)
        {
            var testInfo = new List<TestInfo>();

            testInfo.AddRange(GetTestsFromList(Project.Dir, project.TestList));
            
            return testInfo;
        }

        private static IEnumerable<TestInfo> GetTestsFromList(TestDir dir, TestList testList)
        {
            if (testList == null)
                return dir.GetTests();

            var testsInfo = new List<TestInfo>();

            foreach (var testDir in dir.Dirs)
            {
                TestList list;
                if (testList.IsPathInList(testDir.Name, out list))
                    testsInfo.AddRange(GetTestsFromList(testDir, list));
            }

            foreach (var testFile in dir.Files)
            {
                TestList list;
                if (testList.IsPathInList(testFile.Name, out list))
                    testsInfo.Add(testFile);
            }

            return testsInfo;
        }
        /// <summary>
        /// Получить список тестов по списку их "номеров"
        /// </summary>
        /// <param name="testsList"></param>
        /// <returns></returns>
        public static List<TestInfo> GetTests(List<string> testsList)
        {
            var testsInfo = new List<TestInfo>();

            foreach (var testInfo in Project.GetTests())
            {
                foreach (var test in testsList)
                {
                    if (test.Contains("-"))
                    {
                        var endIndex = test.IndexOf("-", StringComparison.CurrentCulture);
                        var firstNum = int.Parse(test.Substring(0, endIndex));
                        var secondNum = int.Parse(test.Substring(endIndex + "-".Length));

                        if(firstNum > secondNum)
                            throw new ArgumentException("Первое число больше второго");

                        for (var i = firstNum; i <= secondNum; i++)
                        {
                            var strI = i.ToString(Format);

                            if (testInfo.Number == strI && testsInfo.All(t => t.ClassName != testInfo.ClassName))
                                testsInfo.Add(testInfo);
                        }
                    }
                    else
                    {
                        if (testInfo.Number == test.Replace("_", "") && testsInfo.All(t => t.ClassName != testInfo.ClassName))
                            testsInfo.Add(testInfo);
                    }
                }
            }

            return testsInfo;
        }

        /// <summary>
        /// Получить список всех загруженных тестов
        /// </summary>
        /// <returns>Список всех тестов</returns>
        public static List<TestInfo> GetTests()
        {
            var allTests = new List<TestInfo>();
            return AllProjects.List.Aggregate(allTests, (current, project) => current.Concat(GetTests(project.Value)).ToList());
        }

        /// <summary>
        /// Получить список тестов c привязкой к мылу
        /// </summary>
        /// <returns>Список тестов c привязкой к мылу</returns>
        public static Dictionary<string, string> GetInfoMails()
        {
            var mailtInfo = new Dictionary<string, string>();

            foreach (var test in Project.GetTests())
                mailtInfo[test.ClassName] = AllProjects.Mails[test.Attr.Owner];

            return mailtInfo;
        }
        
        /// <summary>
        /// Получить исходный код теста из списка тестов.
        /// </summary>
        /// <param name="name">Имя класса теста</param>
        /// <returns>Исходный код</returns>
        public static TestInfo GetTest(string name)
        {
            var tests = Project.GetTests();
            if (tests.Any(t => t.ClassName == name))
                return tests.First(t => t.ClassName == name);

            throw new ArgumentOutOfRangeException("Не удалось получить информацию теста." + Environment.NewLine + name);
        }

        /// <summary>
        /// Создание директории по заданному пути
        /// </summary>
        /// <param name="path">Путь</param>
        /// <param name="isFile">файл</param>
        public static void CreateDir(string path, bool isFile = false)
        {
            path = isFile ? Directory.GetParent(path).FullName : path;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Удаление всех файлов и директорий из директории
        /// </summary>
        /// <param name="folder">Директория</param>
        /// <param name="root">Удалять ли саму директорию после</param>
        public static void DelTree(string folder, bool root = false)
        {
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder))
                    File.Delete(file);

                foreach (var dir in Directory.GetDirectories(folder))
                    Directory.Delete(dir, true);

                if (root)
                    Directory.Delete(folder);
            }
        }

        /// <summary>
        /// Удалить из директории файлы с похожим названием
        /// </summary>
        /// <param name="folder">Директория</param>
        /// <param name="filename">Часть имени файла</param>
        public static bool DelLikeFiles(string folder, string filename)
        {
            var deleting = false;
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder))
                {
                    if (file.Replace(folder, "").StartsWith(filename))
                    {
                        var notGood = true;
                        while (notGood)
                        {
                            try
                            {
                                File.Delete(file);
                                notGood = false;
                            }
                            catch (IOException)
                            {

                            }
                        }
                        
                        deleting = true;
                    }
                }
            }
            return deleting;
        }
    }

    [Serializable]
    public class PathException : Exception
    {
        public PathException()
        {
        }

        public PathException(string message) : base(message)
        {
        }

        public PathException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PathException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AutoTestAttribute : Attribute
    {
        public AutoTestAttribute(string id, ProjectOwners owner)
        {
            Id = Guid.Parse(id);
            Owner = owner;
        }

        public Guid Id;
        public ProjectOwners Owner;
    }

    [Serializable]
    [XmlRoot]
    public class Project
    {
        [XmlAttribute(AttributeName = "projectDir")]
        public string ProjectDir { get; set; }

        [XmlAttribute(AttributeName = "testsDir")]
        public string TestsDir { get; set; }

        [XmlAttribute(AttributeName = "projectPath")]
        public string ProjectPath { get; set; }

        [XmlElement(ElementName = "Dir")]
        public TestDir Dir { get; set; }

        public List<TestInfo> GetTests()
        {
            return Dir.GetTests();
        }
    }

    [Serializable]
    public class TestDir
    {
        [XmlElement(ElementName = "Dir")]
        public List<TestDir> Dirs { get; set; }

        [XmlElement(ElementName = "File")]
        public List<TestInfo> Files { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlIgnore]
        private List<TestInfo> _testInfos;

        public List<TestInfo> GetTests()
        {
            if (_testInfos != null)
                return _testInfos;
           
            _testInfos = new List<TestInfo>();

            foreach (var testFile in Files)
                _testInfos.Add(testFile);

            foreach (var testDir in Dirs)
                _testInfos.AddRange(testDir.GetTests());

            return _testInfos;
        }
    }

    [Serializable]
    public class TestInfo : ICloneable
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "className")]
        public string ClassName { get; set; }

        [XmlElement(ElementName = "Guid")]
        public List<TestGuid> Guids { get; set; }

        [XmlElement(ElementName = "Bug")]
        public List<TestBug> Bugs { get; set; }

        [XmlIgnore]
        public string Number;
        [XmlIgnore]
        public ProjectType ProjectType;
        [XmlIgnore]
        public AutoTestAttribute Attr;
        [XmlIgnore]
        public SeleniumCommands ClassObject;
        [XmlIgnore]
        public MethodInfo[] MethodInfos;
        [XmlIgnore]
        public bool IsBeenRunned;

        public List<Guid> GetGuids()
        {
            return Guids.Select(t => t.Value).ToList();
        }

        public string GetBug(int line)
        {
            return Bugs.Any(t => t.Line == line) ? Bugs.First(t => t.Line == line).Href : null;
        }

        public object Clone()
        {
            return new TestInfo
            {
                Name = Name,
                ClassName = ClassName,
                Guids = Guids,
                Bugs = Bugs,
                Number = Number,
                ProjectType = ProjectType,
                Attr = Attr,
                ClassObject = Activator.CreateInstance(ClassObject.GetType()) as SeleniumCommands,
                MethodInfos = MethodInfos,
                IsBeenRunned = IsBeenRunned
            };
        }
    }

    [Serializable]
    public class TestGuid
    {
        [XmlAttribute(AttributeName = "value")]
        public Guid Value { get; set; }
    }

    [Serializable]
    public class TestBug
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "line")]
        public int Line { get; set; }

        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }
    }
}
