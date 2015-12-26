using System.Collections.Generic;

namespace AutoTest.helpers
{
    public enum ProjectType
    {
        None = 0
    }

    public enum ProjectOwners
    {
        Zaharkov
    }

    sealed class AllProjects
    {
        public static Dictionary<ProjectType, ParametersProject> List = new Dictionary<ProjectType, ParametersProject>
        {
            {ProjectType.None, new ParametersProject(ProjectType.None)}
        };

        public static Dictionary<ProjectOwners, string> Mails = new Dictionary<ProjectOwners, string>
        {
            {ProjectOwners.Zaharkov ,"zaharkov@test.ru"}
        };
    }

    public class ParametersProject
    {
        /// <summary>
        /// Массив тестов, подлежащих запуску по умолчанию
        /// Все тесты должны быть в папке Tests и принадлежать к папке с номером из списка
        /// Так же все тесты должны иметь в начале порядковый номер совпадающий с номером класса внутри
        /// </summary>
        public TestList TestList;

        public ParametersProject(ProjectType type)
        {
            switch (type)
            {
                case ProjectType.None:
                    TestList = new TestList("99 For tests", null);
                    break;
                default:
                    TestList = null;
                    break;
            }
        }
    }

    public class TestList
    {
        private readonly Dictionary<string, TestList> _testLists = new Dictionary<string, TestList>();

        public TestList()
        {

        }

        public TestList(string name, TestList list)
        {
            _testLists.Add(name, list);
        }

        public TestList Add(string name, TestList list)
        {
            _testLists.Add(name, list);
            return this;
        }

        public bool IsPathInList(string path, out TestList list)
        {
            foreach (var testList in _testLists)
            {
                if (path.StartsWith(testList.Key))
                {
                    list = testList.Value;
                    return true;
                }
            }

            list = null;
            return false;
        }
    }
}
