 using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AutoTest.Properties;

namespace AutoTest.helpers
{
    public enum SchetStatusType
    {
        Active,
        Passive,
        ActivePassive
    }

    /// <summary>
    /// Класс для работы с SQL
    /// </summary>
    public sealed class SqlCommands
    {
        private readonly SeleniumCommands _sel;
        private readonly string _connetionString = 
                "Data Source=" + ParametersInit.SqlServer + ";" +
                "User ID=" + ParametersInit.SqlUser + ";" +
                "Password=" + ParametersInit.SqlPass + ";" +
                "Connection Timeout=" + ParametersInit.TimeMax + ";" +
                "MultipleActiveResultSets=true;";

        /// <summary>
        /// Можно использовать без связки с SeleniumCommands
        /// (вывод ошибок будет стандартный)
        /// </summary>
        /// <param name="sel">При указании будет выводить ошибки через SeleniumCommands</param>
        public SqlCommands(SeleniumCommands sel = null)
        {
            _sel = sel;
        }

        /// <summary>
        /// Выполнить запрос к БД
        /// </summary>
        /// <param name="sqlCommand">Сам запрос Sql c параметрами</param>
        /// <param name="notSel"></param>
        /// <param name="isoLvl"></param>
        /// <returns>Результаты запроса ввиде массива из ассоциативных массивов</returns>
        private Dictionary<int, Dictionary<string, string>> ExecuteSql(SqlCommand sqlCommand, bool notSel = false, IsolationLevel isoLvl = IsolationLevel.ReadUncommitted)
        {
            var id = new StackFrame(1).GetMethod().Name;

            using (var cnn = new SqlConnection(_connetionString))
            {
                cnn.Open();
                using (var tran = cnn.BeginTransaction(isoLvl))
                {
                    sqlCommand.Connection = cnn;
                    sqlCommand.Transaction = tran;
                    sqlCommand.CommandTimeout = ParametersInit.TimeMax;
                    var result = new Dictionary<int, Dictionary<string, string>>();

                    try
                    {
                        var time = new Stopwatch();
                        time.Start();

                        var task = Task.Run(() => sqlCommand.ExecuteReader());

                        if (_sel != null && !notSel)
                        {
                            while (!task.IsCompleted && time.Elapsed.TotalSeconds < ParametersInit.TimeMax)
                                _sel.TouchWebDriver();

                            if (time.Elapsed.TotalSeconds > ParametersInit.TimeJ)
                                _sel.BackTrace("SQL запрос " + id + " длился более " + ParametersInit.TimeJ + " секунд",
                                    ErrorType.Timed | ErrorType.Bug);

                            if (time.Elapsed.TotalSeconds > ParametersInit.TimeMax)
                                throw FailingSqlCommands("Время выполнения операции Sql истекло");

                            //if (task.Result.RecordsAffected != 0)
                            //    _sel.Post;
                        }

                        using (var sqlReader = task.Result)
                        {
                            while (sqlReader.Read())
                            {
                                var row = new Dictionary<string, string>();

                                for (var i = 0; i < sqlReader.FieldCount; i++)
                                {
                                    row[sqlReader.GetName(i)] = sqlReader.GetValue(i).ToString();
                                }

                                result[result.Count] = row;
                            }
                        }
                        
                        tran.Commit();
                    }
                    catch (Exception e)
                    {
                        tran.Rollback();
                        if (e is SeleniumFailException || e is PostException)
                            throw;

                        var paramText = sqlCommand.Parameters.Cast<object>().Aggregate("", (current, param) =>
                            current + (param + "=" + sqlCommand.Parameters[param.ToString()].Value + ","));

                        throw FailingSqlCommands("Запрос в базу: " + id + " завершился с ошибкой: " + e.Message
                                                 + Environment.NewLine + paramText, notSel, e);
                    }

                    return result;
                }
            }  
        }

        /// <summary>
        /// Вывод ошибки SQL
        /// </summary>
        /// <param name="text">Информация о падении</param>
        /// <param name="notSel">Если указан будет выполнен без привязки к SeleniumCommands</param>
        /// <param name="e"></param>
        private Exception FailingSqlCommands(string text, bool notSel = false, Exception e = null)
        {
            if (_sel != null && !notSel)
                return _sel.FailingTest("Ошибка в SQL", text, e);

            throw new SqlException(text, e);
        }

        /// <summary>
        /// Время прохода тестов из БД
        /// </summary>
        /// <returns>Время прохода тестов</returns>
        public Dictionary<Guid, double> GetTestDate()
        {
            using (var query = new SqlCommand(Resources.GetTestDate))
            {
                var result = ExecuteSql(query, true);
                return result.ToDictionary(key => Guid.Parse(key.Value["Id"]), value => double.Parse(value.Value["Time"])); 
            }
        }

        /// <summary>
        /// Записать данные прохода теста
        /// </summary>
        /// <param name="id">GUID теста</param>
        /// <param name="name">Номер теста</param>
        /// <param name="time">Время прохода</param>
        /// <param name="criticalError">Была ли критическая ошибка</param>
        /// <param name="simpleError">Были ли ли мелкие ошибки</param>
        /// <param name="isTestValid">Валидный ли тест</param>
        /// <param name="endTime">Время окончания</param>
        /// <param name="owner">Чей тест</param>
        public void InsertTestData(Guid id, string name, double time, bool criticalError, bool simpleError, bool isTestValid, DateTime endTime, ProjectOwners owner)
        {
            using (var query = new SqlCommand(Resources.InsertTestData))
            {
                query.Parameters.AddWithValue("Id", id);
                query.Parameters.AddWithValue("Name", name);
                query.Parameters.AddWithValue("Time", time);
                query.Parameters.AddWithValue("CriticalError", criticalError);
                query.Parameters.AddWithValue("SimpleError", simpleError);
                query.Parameters.AddWithValue("IsTestValid", isTestValid);
                query.Parameters.AddWithValue("EndTime", endTime);
                query.Parameters.AddWithValue("Owner", owner.ToString());

                ExecuteSql(query, true);
            } 
        }

        public void SetDefaultTestData(Guid id, string name, ProjectOwners owner)
        {
            using (var query = new SqlCommand(Resources.SetDefaultTestData))
            {
                query.Parameters.AddWithValue("Id", id);
                query.Parameters.AddWithValue("Name", name);
                query.Parameters.AddWithValue("Owner", owner.ToString());

                ExecuteSql(query, true);
            }
        }

        /// <summary>
        /// Добавление запуска в базу логов
        /// </summary>
        /// <param name="address">адрес</param>
        /// <param name="screenPath">путь к скринам</param>
        /// <param name="testsCount">кол. тестов</param>
        /// <param name="timeStart">время начала</param>
        public Guid AddParallelLog(string address, DateTime timeStart, string screenPath, int testsCount)
        {
            using (var query = new SqlCommand(Resources.AddParallelLog))
            {
                var id = Guid.NewGuid();
                query.Parameters.AddWithValue("Id", id);
                query.Parameters.AddWithValue("Address", address);
                query.Parameters.AddWithValue("TimeStart", timeStart);
                query.Parameters.AddWithValue("ScreenPath", screenPath);
                query.Parameters.AddWithValue("TestsCount", testsCount);

                ExecuteSql(query, true);

                return id; 
            }
        }

        public void SetParallelLogTimeEnd(Guid id, DateTime timeEnd)
        {
            using (var query = new SqlCommand(Resources.SetParallelLogTimeEnd))
            {
                query.Parameters.AddWithValue("Id", id);
                query.Parameters.AddWithValue("TimeEnd", timeEnd);

                ExecuteSql(query, true);
            }
        }

        public void AddTestLog(Guid testId, Guid parallelId, DateTime timeStart, string login)
        {
            using (var query = new SqlCommand(Resources.AddTestLog))
            {
                query.Parameters.AddWithValue("TestId", testId);
                query.Parameters.AddWithValue("ParallelId", parallelId);
                query.Parameters.AddWithValue("TimeStart", timeStart);
                query.Parameters.AddWithValue("Login", login);

                ExecuteSql(query, true);
            } 
        }

        public void SetTestLogTimeEnd(Guid testId, Guid parallelId, DateTime timeEnd)
        {
            using (var query = new SqlCommand(Resources.SetTestLogTimeEnd))
            {
                query.Parameters.AddWithValue("TestId", testId);
                query.Parameters.AddWithValue("ParallelId", parallelId);
                query.Parameters.AddWithValue("TimeEnd", timeEnd);

                ExecuteSql(query, true);
            } 
        }

        public void AddErrorLog(Guid testId, Guid parallelId, string text, string type, int? line,
            DateTime time, string bug, string screenPath, bool isChecked, string guidsText)
        {
            using (var query = new SqlCommand(Resources.AddErrorLog))
            {
                query.Parameters.AddWithValue("TestId", testId);
                query.Parameters.AddWithValue("ParallelId", parallelId);
                query.Parameters.AddWithValue("Text", text);
                query.Parameters.AddWithValue("Type", string.IsNullOrEmpty(type) ? DBNull.Value : (object)type);
                query.Parameters.AddWithValue("Line", (object)line ?? DBNull.Value);
                query.Parameters.AddWithValue("Time", time);
                query.Parameters.AddWithValue("Bug", string.IsNullOrEmpty(bug) ? DBNull.Value : (object)bug);
                query.Parameters.AddWithValue("ScreenPath", string.IsNullOrEmpty(screenPath) ? DBNull.Value : (object)screenPath);
                query.Parameters.AddWithValue("Checked", isChecked);
                query.Parameters.AddWithValue("GuidText", guidsText);

                ExecuteSql(query, true); 
            }  
        }

        public void DeleteTestLog(Guid testId, Guid parallelId)
        {
            using (var query = new SqlCommand(Resources.DeleteTestLog))
            {
                query.Parameters.AddWithValue("TestId", testId);
                query.Parameters.AddWithValue("ParallelId", parallelId);

                ExecuteSql(query, true);
            }
        }

        public List<string> GetFailedTest(Guid parallelId)
        {
            using (var query = new SqlCommand(Resources.GetFailedTests))
            {
                query.Parameters.AddWithValue("ParallelId", parallelId);

                var result = ExecuteSql(query, true);
                return result.Select(t => t.Value["Name"]).ToList();
            } 
        }
    }

    [Serializable]
    public class SqlException : ApplicationException
    {
        public SqlException()
        {
        }

        public SqlException(string message) : base(message)
        {
        }

        public SqlException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SqlException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
