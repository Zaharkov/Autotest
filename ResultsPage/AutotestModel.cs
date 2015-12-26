using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace BOL.Site.Models
{
    public class Autotest : BaseModel
    {
        public List<ParallelInfo> GetParallelsInfo()
        {
            using (var sql = new QaSql())
            {
                return sql.GetParallelsInfo();
            }
        }

        public ParallelInfo GetParallelInfo(Guid parallelId)
        {
            using (var sql = new QaSql())
            {
                return sql.GetParallelInfo(parallelId);
            }
        }

        public List<ErrorInfo> GetErrorsInfo(Guid parallelId, Guid testId)
        {
            using (var slq = new QaSql())
            {
                return slq.GetErrorsInfo(parallelId, testId);
            }
        }

        public List<ErrorInfo> GetErrorsInfo2(Guid parallelId)
        {
            using (var slq = new QaSql())
            {
                return slq.GetErrorsInfo2(parallelId);
            }
        }

        public List<TestInfo> GetTestsInfo(Guid parallelId)
        {
            using (var sql = new QaSql())
            {
                return sql.GetTestsInfo(parallelId);
            }
        }

        public void DeleteParallelInfo(Guid parallelId)
        {
            using (var sql = new QaSql())
            {
                sql.DeleteParallelInfo(parallelId);
            }
        }

        public void DeleteTestInfo(Guid parallelId, Guid testId)
        {
            using (var sql = new QaSql())
            {
                sql.DeleteTestInfo(parallelId, testId);
            }
        }

        public void DeleteErrorInfo(Guid errorId)
        {
            using (var sql = new QaSql())
            {
                sql.DeleteErrorInfo(errorId);
            }
        }

        public void CheckTestInfo(Guid parallelId, Guid testId)
        {
            using (var sql = new QaSql())
            {
                sql.CheckTestInfo(parallelId, testId);
            }
        }

        public void CheckErrorInfo(Guid errorId)
        {
            using (var sql = new QaSql())
            {
                sql.CheckErrorInfo(errorId);
            }
        }

        public void UnCheckErrorInfo(Guid errorId)
        {
            using (var sql = new QaSql())
            {
                sql.UnCheckErrorInfo(errorId);
            }
        }

        public void ChangeParallelScreenPath(Guid parallelId, string screenPath)
        {
            using (var sql = new QaSql())
            {
                sql.ChangeParallelScreenPath(parallelId, screenPath);
            }
        }
    }

    public class ParallelInfo
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string ScreenPath { get; set; }
        public int TestsCount { get; set; }

        public ParallelInfo(Guid id, string address, DateTime timeStart,
             DateTime? timeEnd, string screenPath, int testsCount)
        {
            Id = id;
            Address = address;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
            ScreenPath = screenPath;
            TestsCount = testsCount;
        }
    }

    public class TestInfo
    {
        public Guid TestId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string LoginName { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }

        public TestInfo(Guid testId, string name, string owner, string loginName, DateTime timeStart, DateTime? timeEnd)
        {
            TestId = testId;
            Name = name;
            Owner = owner;
            LoginName = loginName;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }
    }

    public class ErrorInfo
    {
        public Guid ErrorId { get; set; }
        public Guid TestId { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
        public DateTime Time { get; set; }
        public string Bug { get; set; }
        public string ScreenPath { get; set; }
        public bool IsChecked { get; set; }
        public string GuidText { get; set; }

        public ErrorInfo(Guid errorId, Guid testId, string text, string type, int line, 
            DateTime time, string bug, string screenPath, bool check, string guidText)
        {
            ErrorId = errorId;
            TestId = testId;
            Text = text;
            Type = type;
            Line = line;
            Time = time;
            Bug = bug;
            ScreenPath = screenPath;
            IsChecked = check;
            GuidText = guidText;
        }
    }

    public class QaSql : IDisposable
    {
        private readonly string _cnnStr;
        private SqlConnection _cnn;

        public QaSql()
        {
            var conf = WebConfigurationManager.OpenWebConfiguration("~");
            var conStr = conf.ConnectionStrings.ConnectionStrings["LogConnection"];
            _cnnStr = conStr.ConnectionString;
        }

        private void ConnectServer()
        {
            _cnn = new SqlConnection(_cnnStr);
            _cnn.Open();
        }

        public void Dispose()
        {
            _cnn.Close();
        }

        /// <summary>
        /// Выполнить запрос к БД
        /// </summary>
        /// <param name="sqlCommand">Сам запрос Sql c параметрами</param>
        /// <returns>Результаты запроса ввиде массива из ассоциативных массивов</returns>
        private SqlDataReader ExecuteSql(SqlCommand sqlCommand)
        {
            ConnectServer();

            sqlCommand.Connection = _cnn;
            sqlCommand.CommandTimeout = 120;
            sqlCommand.CommandType = CommandType.StoredProcedure;

            try
            {
                return sqlCommand.ExecuteReader();
            }
            catch (Exception e)
            {
                var paramText = sqlCommand.Parameters.Cast<object>().Aggregate("", (current, param) =>
                    current + (param + "=" + sqlCommand.Parameters[param.ToString()].Value + ","));
                throw new Exception("Запрос в базу завершился с ошибкой: " + e.Message + Environment.NewLine + paramText, e);
            }
        }

        public List<ParallelInfo> GetParallelsInfo()
        {
            var query = new SqlCommand("dbo.GetParallelsInfo");
            var result = ExecuteSql(query);

            var list = new List<ParallelInfo>();

            while (result.Read())
            {
                var parInfo = new ParallelInfo
                (
                    Guid.Parse(result["Id"].ToString()),
                    result["Address"].ToString(),
                    DateTime.Parse(result["TimeStart"].ToString()),
                    Convert.IsDBNull(result["TimeEnd"]) ? (DateTime?)null : DateTime.Parse(result["TimeEnd"].ToString()),
                    result["ScreenPath"].ToString(),
                    int.Parse(result["TestsCount"].ToString())
                );

                list.Add(parInfo);
            }

            return list;
        }

        public ParallelInfo GetParallelInfo(Guid parallelId)
        {
            var query = new SqlCommand("dbo.GetParallelInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            var result = ExecuteSql(query);

            ParallelInfo parInfo;
            if(result.Read())
            {
                parInfo = new ParallelInfo
                (
                    Guid.Parse(result["Id"].ToString()),
                    result["Address"].ToString(),
                    DateTime.Parse(result["TimeStart"].ToString()),
                    Convert.IsDBNull(result["TimeEnd"]) ? (DateTime?)null : DateTime.Parse(result["TimeEnd"].ToString()),
                    result["ScreenPath"].ToString(),
                    int.Parse(result["TestsCount"].ToString())
                );
            }
            else
                throw new ArgumentException("Не найден GUID " + parallelId + " в таблице логов Parallels");

            return parInfo;
        }

        public List<TestInfo> GetTestsInfo(Guid parallelId)
        {
            var query = new SqlCommand("dbo.GetTestsInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            var result = ExecuteSql(query);

            var list = new List<TestInfo>();

            while (result.Read())
            {
                var parInfo = new TestInfo
                (
                    Guid.Parse(result["TestId"].ToString()),
                    result["Name"].ToString(),
                    result["Owner"].ToString(),
                    result["LoginName"].ToString(),
                    DateTime.Parse(result["TimeStart"].ToString()),
                    Convert.IsDBNull(result["TimeEnd"]) ? (DateTime?)null : DateTime.Parse(result["TimeEnd"].ToString())
                );

                list.Add(parInfo);
            }

            return list;
        }

        public List<ErrorInfo> GetErrorsInfo(Guid parallelId, Guid testId)
        {
            var query = new SqlCommand("dbo.GetErrorsInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            query.Parameters.AddWithValue("testId", testId);
            var result = ExecuteSql(query);

            var list = new List<ErrorInfo>();

            while (result.Read())
            {
                var parInfo = new ErrorInfo
                (
                    Guid.Parse(result["Id"].ToString()),
                    Guid.Parse(result["TestId"].ToString()),
                    result["Text"].ToString(),
                    Convert.IsDBNull(result["Type"]) ? null : result["Type"].ToString(),
                    Convert.IsDBNull(result["Line"]) ? 0 : int.Parse(result["Line"].ToString()),
                    DateTime.Parse(result["Time"].ToString()),
                    Convert.IsDBNull(result["Bug"]) ? null : result["Bug"].ToString(),
                    Convert.IsDBNull(result["ScreenPath"]) ? null : result["ScreenPath"].ToString(),
                    bool.Parse(result["Checked"].ToString()),
                    result["GuidsText"].ToString()
                );

                list.Add(parInfo);
            }

            return list;
        }

        public List<ErrorInfo> GetErrorsInfo2(Guid parallelId)
        {
            var query = new SqlCommand("dbo.GetErrorsInfo2");
            query.Parameters.AddWithValue("parallelId", parallelId);
            var result = ExecuteSql(query);

            var list = new List<ErrorInfo>();

            while (result.Read())
            {
                var parInfo = new ErrorInfo
                (
                    Guid.Parse(result["Id"].ToString()),
                    Guid.Parse(result["TestId"].ToString()),
                    result["Text"].ToString(),
                    Convert.IsDBNull(result["Type"]) ? null : result["Type"].ToString(),
                    Convert.IsDBNull(result["Line"]) ? 0 : int.Parse(result["Line"].ToString()),
                    DateTime.Parse(result["Time"].ToString()),
                    Convert.IsDBNull(result["Bug"]) ? null : result["Bug"].ToString(),
                    Convert.IsDBNull(result["ScreenPath"]) ? null : result["ScreenPath"].ToString(),
                    bool.Parse(result["Checked"].ToString()),
                    result["GuidsText"].ToString()
                );

                list.Add(parInfo);
            }

            return list;
        }

        public void DeleteParallelInfo(Guid parallelId)
        {
            var query = new SqlCommand("dbo.DeleteParallelInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            var result = ExecuteSql(query);

            if(result.RecordsAffected == 0)
                throw new ArgumentException("Не найден GUID " + parallelId + " в таблице логов Parallels");
        }

        public void DeleteTestInfo(Guid parallelId, Guid testId)
        {
            var query = new SqlCommand("dbo.DeleteTestInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            query.Parameters.AddWithValue("testId", testId);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найдена пара GUIDов " + parallelId + " и " + testId + " в таблице логов Tests");
        }

        public void DeleteErrorInfo(Guid errorId)
        {
            var query = new SqlCommand("dbo.DeleteErrorInfo");
            query.Parameters.AddWithValue("errorId", errorId);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найден GUID " + errorId + " в таблице логов Errors");
        }

        public void CheckTestInfo(Guid parallelId, Guid testId)
        {
            var query = new SqlCommand("dbo.CheckTestInfo");
            query.Parameters.AddWithValue("parallelId", parallelId);
            query.Parameters.AddWithValue("testId", testId);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найдена пара GUIDов " + parallelId + " и " + testId + " в таблице логов Tests");
        }

        public void CheckErrorInfo(Guid errorId)
        {
            var query = new SqlCommand("dbo.CheckErrorInfo");
            query.Parameters.AddWithValue("errorId", errorId);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найден GUID " + errorId + " в таблице логов Errors");
        }
        
        public void UnCheckErrorInfo(Guid errorId)
        {
            var query = new SqlCommand("dbo.UnCheckErrorInfo");
            query.Parameters.AddWithValue("errorId", errorId);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найден GUID " + errorId + " в таблице логов Errors");
        }

        public void ChangeParallelScreenPath(Guid parallelId, string screenPath)
        {
            var query = new SqlCommand("dbo.ChangeParallelScreenPath");
            query.Parameters.AddWithValue("parallelId", parallelId);
            query.Parameters.AddWithValue("screenPath", screenPath);
            var result = ExecuteSql(query);

            if (result.RecordsAffected == 0)
                throw new ArgumentException("Не найден GUID " + parallelId + " в таблице логов Parallels");
        }
    }
}