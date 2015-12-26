using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace AutoTest.helpers
{
    /// <summary>
    /// Класс для работы с чек-листом в GoogleDocs
    /// </summary>
    public static class GoogleCommands
    {
        /// <summary>
        /// Список вкладок чек-листа
        /// </summary>
        private static readonly List<WorksheetEntry> WorkFeed = new List<WorksheetEntry>();
        /// <summary>
        /// Сервис работы с созданием файлов-таблиц и вкладок
        /// </summary>
        private static DriveService _driveService;
        /// <summary>
        /// Сервис работы с изменениями в самой таблице
        /// </summary>
        private static SpreadsheetEntry _spreadsheet;
        /// <summary>
        /// Объект сертификата для авторизации
        /// </summary>
        private static readonly X509Certificate2 Certificate = new X509Certificate2(
                PathCommands.IsolateFolder + ParametersInit.GoogleServiceAccountKey, "notasecret",
                X509KeyStorageFlags.Exportable);
        /// <summary>
        /// Задача на создание колонки в чек-листе
        /// </summary>
        private static Task _makeColTask;
        /// <summary>
        /// Задача на получение списка: гуид проверки + описание проверки
        /// </summary>
        private static Task<Dictionary<Guid, string>> _guidTexts;
        /// <summary>
        /// Флаг - нужно ли подгружать список _guidTexts
        /// </summary>
        public static bool LoadingGuidTexts = false;
        /// <summary>
        /// Максимальное число колонок в чек-листе (5 стандартные, 5 - колонки с прогонами)
        /// </summary>
        private const uint MaxColumnInCheckList = 10;
        /// <summary>
        /// Номер колонки, где проставлять exists для существующих в тестах проверок
        /// </summary>
        private const uint ColumnForExists = 1;
        /// <summary>
        /// Колонка в которой записана проверка
        /// </summary>
        private const uint ColumnForCheck = 2;
        /// <summary>
        /// Колонка в которой записан ожидаемый результат
        /// </summary>
        private const uint ColumnForResult = 3;
        /// <summary>
        /// Колонка для номера проверок (GUID)
        /// </summary>
        private const uint ColumnForGulds = 4;
        /// <summary>
        /// Колонка в которую записывать результаты тестирования
        /// </summary>
        private const uint ColumnForCheckResult = 6;

        /// <summary>
        /// Строчка в которой записана дата начала тестов
        /// </summary>
        private const uint RowForHeader = 1;
        /// <summary>
        /// Строчка в которой записано общее число успешных проверок
        /// </summary>
        private const uint RowForPassed = 2;
        /// <summary>
        /// Строчка в которой записано общее число неуспешных проверок
        /// </summary>
        private const uint RowForFailed = 3;
        /// <summary>
        /// Строчка в которой записано общее число неизвестных проверок
        /// </summary>
        private const uint RowForUnknown = 4;
        /// <summary>
        /// Строчка в которой записана дата окончания тестов
        /// </summary>
        private const uint RowForDateEnd = 5;
        /// <summary>
        /// Паттерн для гуидов
        /// </summary>
        private const string GuldPattern = @"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}";
        /// <summary>
        /// Нужен "алфавит" чтобы указать диапазон колонок/строк в таблице
        /// </summary>
        private readonly static char[] Alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        /// <summary>
        /// Свойство для получения заавторизованного сервиса работы с таблицей
        /// </summary>
        private static SpreadsheetsService Service
        {
            get
            {
                var credentialInit = new ServiceAccountCredential.Initializer(ParametersInit.GoogleServiceAccountEmail)
                {
                    Scopes = new[] { "https://spreadsheets.google.com/feeds" }
                }.FromCertificate(Certificate);

                var credential = new ServiceAccountCredential(credentialInit);
                credential.RequestAccessTokenAsync(CancellationToken.None).Wait();
                var spreadsheetsService = new SpreadsheetsService(null);
                var requestFactory = new GDataRequestFactory(null);
                requestFactory.CustomHeaders.Add(string.Format("Authorization: Bearer {0}", credential.Token.AccessToken));
                spreadsheetsService.RequestFactory = requestFactory;
                return spreadsheetsService;
            }
        }

        static GoogleCommands()
        {
            //получем название чек-листа 
            var spreadSheetName = ParametersInit.SpreadSheetName;
            //получаем список вкладок в чек-листе, с которыми нам нужно работать
            var workSheetName = ParametersInit.WorkSheetName;

            var query = new SpreadsheetQuery {Title = spreadSheetName};
            var spreadsheetfeed = Service.Query(query);

            if (!spreadsheetfeed.Entries.Any())
                throw new ArgumentException("Чек-лист не найден");

            var spreadsheet = (SpreadsheetEntry) spreadsheetfeed.Entries.First();
            var wsFeed = spreadsheet.Worksheets;
            var worksheets = wsFeed.Entries.Where(x => workSheetName.Contains(x.Title.Text)).ToList();

            //в итоге получаем список объектов-вкладок чек-листа
            foreach (var worksheet2 in worksheets.Cast<WorksheetEntry>())
                WorkFeed.Add(worksheet2);

            //инициализируем сервис для работы с созданием новых файлов/вкладок
            //нужно для проекта LoadTest
            InitDriveService();
        }

        /// <summary>
        /// Метод для получения списка ячеек по запросу
        /// </summary>
        /// <param name="cellQuery"></param>
        /// <returns></returns>
        private static EntitesList Query(CellQuery cellQuery)
        {
            var cellFeed = Service.Query(cellQuery);
            return new EntitesList(cellFeed);
        }

        /// <summary>
        /// Создание новой, чистой колонки в чек-листе (во всех указанных вкладках)
        /// Делается через таск, так как операция очень долгая
        /// </summary>
        /// <param name="columnHeader">название столбца для этой колонки</param>
        /// <returns></returns>
        public static Task MakeNewCol(string columnHeader)
        {
            _makeColTask = Task.Run(() =>
            {
                //для каждой вкладки...
                foreach (var worksheetEntry in WorkFeed)
                {
                    //задаем максимальное число колонок
                    worksheetEntry.Cols = MaxColumnInCheckList;
                    worksheetEntry.Update();
                    //получаем список всех ячеек, где записаны результаты тестов
                    var entitesList = Query(new CellQuery(worksheetEntry.CellFeedLink)
                    {
                        MinimumColumn = ColumnForCheckResult,
                        ReturnEmpty = ReturnEmptyCells.yes,
                    });

                    var cellForBatch = new List<CellForBatch>();
                    foreach (var entry in entitesList)
                    {
                        //делаем смещение всех колонок на одну вправо
                        if (entry.Column < MaxColumnInCheckList)
                            cellForBatch.Add(new CellForBatch(entry.Row, entry.Column + 1, entry.InputValue));

                        //первую колонку из списка полностью очищаем и задаем ей название, а так же делаем ссылки на суммы проверок (passed, failed, unknown)
                        if (entry.Column == ColumnForCheckResult)
                        {
                            string value;
                            var charName = Alpha[ColumnForCheckResult - 1];
                            switch (entry.Row)
                            {
                                case RowForHeader: value = columnHeader; break;
                                case RowForPassed: value = "=COUNTIF({" + charName + ColumnForCheckResult + ":" + charName + "},\"passed*\")"; break;
                                case RowForFailed: value = "=COUNTIF({" + charName + ColumnForCheckResult + ":" + charName + "},\"failed*\")"; break;
                                case RowForUnknown: value = "=COUNTIF({" + charName + ColumnForCheckResult + ":" + charName + "},\"unknown*\")"; break;
                                default: value = ""; break;
                            }

                            cellForBatch.Add(new CellForBatch(entry.Row, entry.Column, value));
                        }
                    }
                    //отправляем изменения на сервер
                    MakeBatchRequest(entitesList, cellForBatch);
                }
            });

            return _makeColTask;
        }

        /// <summary>
        /// Заменяем все строчки "needID" на новые GUID
        /// </summary>
        public static void SetNewId()
        {
            //для каждой вкладки...
            foreach (var workSheet in WorkFeed)
            {
                //получаем все ячейки в колонке для гуидов
                var entitesList = Query(new CellQuery(workSheet.CellFeedLink)
                {
                    MinimumColumn = ColumnForGulds,
                    MaximumColumn = ColumnForGulds,
                });

                //если в какой-то из них задан "needID" - меняем его на гуид
                var cellForBatch = entitesList
                    .Where(x => x.Content.Content == "needID")
                    .Select(entry => new CellForBatch(entry.Row, entry.Column, Guid.NewGuid().ToString().ToUpper()))
                    .ToList();

                if (!cellForBatch.Any()) 
                    continue;

                //отправляем изменения на сервер
                MakeBatchRequest(entitesList, cellForBatch);
            }
            Console.WriteLine(@"needID в чеклисте заменены новыми GUID'ами");
        }
        /// <summary>
        /// Отфильтровывает ячейки, которые есть в "выгруженном" списке, но нет в результатах автотестов 
        /// (типа такая проверка не запускалась в тестах - её не нужно учитывать)
        /// </summary>
        /// <param name="runResults"></param>
        /// <param name="entitesList"></param>
        /// <returns></returns>
        private static List<CellEntry> FindCellEntries(Dictionary<Guid, string> runResults, IEnumerable<CellEntry> entitesList)
        {
            var result = new List<CellEntry>();
            result.AddRange(entitesList.Where(cellEntry =>
            {
                Guid guidResult;
                var guid = Guid.TryParse(cellEntry.Content.Content, out guidResult);
                return guid && runResults.ContainsKey(guidResult);
            }));
            return result;
        }
        /// <summary>
        /// Сохраняем результаты автотестов в чек-лист
        /// </summary>
        /// <param name="runResults">результаты тестов</param>
        /// <param name="withCol">нужно ли было создавать колонку?</param>
        /// <returns></returns>
        public static Dictionary<Guid, string> SaveRun(Dictionary<Guid, string> runResults, bool withCol = true)
        {
            //если запуска создания колонки не было - делаем это тут и ждем.
            if (withCol)
            {
                if (_makeColTask == null)
                    MakeNewCol(string.Format("{0} {1}{2}{3}", DateTime.Now.ToShortDateString(),
                        DateTime.Now.ToShortTimeString(), Environment.NewLine, "MakeNewCol has notRuned"));

                if (_makeColTask != null)
                    _makeColTask.Wait();
            }

            //для каждой вкладки...
            foreach (var workSheet in WorkFeed)
            {
                //получаем список ячеек с гуидами и результатом проверки
                var entitesList = Query(new CellQuery(workSheet.CellFeedLink)
                {
                    MinimumColumn = ColumnForGulds,
                    MaximumColumn = ColumnForCheckResult,
                    ReturnEmpty = ReturnEmptyCells.yes
                });

                //фильтруем
                var pick = FindCellEntries(runResults, entitesList);
                if (!pick.Any()) 
                    continue;

                //дата завершения тестирования
                var cellForBatch = new List<CellForBatch>
                {
                    new CellForBatch(RowForDateEnd, ColumnForCheckResult, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString())
                };
                //ещё какая-то сортировка...
                var cellEntrys = pick.Where(cellEntry =>
                {
                    Guid guidResult;
                    var guid = Guid.TryParse(cellEntry.Content.Content, out guidResult);
                    return guid && runResults.ContainsKey(guidResult);  
                });
                //заполняем колонку результатов результатом %)
                foreach (var cellEntry in cellEntrys)
                {
                    var guid = Guid.Parse(cellEntry.Content.Content);
                    cellForBatch.Add(new CellForBatch(cellEntry.Row, ColumnForCheckResult, runResults[guid]));
                    runResults.Remove(guid);
                }
                //отправляем изменения на сервер
                MakeBatchRequest(entitesList, cellForBatch);
            }

            return runResults;
        }
        /// <summary>
        /// Класс для более удобных махинаций с ячейками
        /// </summary>
        sealed class CellForBatch
        {
            public readonly string IdString;
            public readonly string Value;
            public readonly uint Row;
            public readonly uint Column;

            public CellForBatch(uint row, uint col, string value)
            {
                Row = row;
                Column = col;
                Value = value;
                IdString = string.Format("R{0}C{1}", row, col);
            }
        }
        /// <summary>
        /// Выполнение "пула" изменений одним запросом
        /// </summary>
        /// <param name="entitesList"></param>
        /// <param name="cellForBatch"></param>
        private static void MakeBatchRequest(EntitesList entitesList, IEnumerable<CellForBatch> cellForBatch)
        {
            var batchRequest = new CellFeed(entitesList.Self, Service);
            foreach (var cell in cellForBatch)
            {
                var batchEntry = entitesList[cell.Row, cell.Column];
                batchEntry.InputValue = cell.Value;
                batchEntry.BatchData = new GDataBatchEntryData(cell.IdString, GDataBatchOperationType.update);
                batchRequest.Entries.Add(batchEntry);
            }

            var batchResponse = ExecuteBatch(batchRequest, entitesList.Batch);

            if (batchResponse.Entries.Any(atomEntry => atomEntry.BatchData.Status.Code != 200))
                throw new GDataBatchRequestException(batchResponse);

            Console.WriteLine(@"Saving batch to Checklist done. Worksheet {0}", entitesList.Title);
        }
        /// <summary>
        /// Проверка покрытия чек-листа проверкам
        /// </summary>
        /// <param name="guidsInTests"></param>
        public static void CheckCoverage(List<Guid> guidsInTests)
        {
            var rgx = new Regex(GuldPattern);

            var guidsInChecklist = new List<string>();
            //для всех вкладок...
            foreach (var workSheet in WorkFeed)
            {
                //получаем список всех ячеек от первого столбца до столбца гуидов
                var list = Query(new CellQuery(workSheet.CellFeedLink)
                {
                    MinimumColumn = ColumnForExists,
                    MaximumColumn = ColumnForGulds,
                    ReturnEmpty = ReturnEmptyCells.yes
                });

                var guidExist = new List<CellForBatch>();
                foreach (var entry in list[ColumnForGulds]) //Перебор всех ячеек страницы чеклиста
                {
                    var guidMatch = rgx.Match(entry.Value.Content.Content); //Проверка, что в ячейке - GUID, выход, если нет
                    if (!guidMatch.Success) 
                        continue;

                    var guid = Guid.Parse(guidMatch.Value);
                    if (guidsInChecklist.Contains(guidMatch.Value)) //Проверка, что такого GUID'а не дублирующийся
                        Console.WriteLine(@"В чеклисте дублирующийся GUID {0}", guidMatch.Value);
                    else
                    {
                        guidsInChecklist.Add(guidMatch.Value);
                        if (!guidsInTests.Contains(guid)) //Проверка, есть ли GUID в тестах, выход, если нет
                            continue;

                        //Проверка, какое стоит ли уже exist для этого GUID'а, выход, если да
                        var aEntry = list[entry.Key, ColumnForExists];
                        if (aEntry.Content.Content == "exist") 
                            continue;

                        //Добавляю в батч-запрос новую ячейку для записи exist
                        guidExist.Add(new CellForBatch(entry.Key, ColumnForExists, "exist"));
                    }
                }
                if (guidExist.Any()) MakeBatchRequest(list, guidExist);
            }

            Console.WriteLine(@"В чеклисте {0} проверок", guidsInChecklist.Count);
            Console.WriteLine(@"В тестах {0} проверок", guidsInTests.Count);
            Console.WriteLine(@"Процент покрытия {0}%", Math.Round(double.Parse(guidsInTests.Count.ToString()) / double.Parse(guidsInChecklist.Count.ToString()) * 100, 2));
        }
        /// <summary>
        /// Задача на получение списка: гуид проверки + текст проверки
        /// Длеается через таск, чтобы съекономить время - выполняется достаточно долго
        /// </summary>
        /// <returns></returns>
        public static Task<Dictionary<Guid, string>> LoadGuidsText()
        {
            //если список вообще не нужен возвращаем пустой
            if(!LoadingGuidTexts)
                return Task.Run(() => new Dictionary<Guid, string>());

            //если таск уже запущен - возвращаем его
            if (_guidTexts != null)
                return _guidTexts;

            _guidTexts = Task.Run(() =>
            {
                var guidTexts = new Dictionary<Guid, string>();
                var existingGuids = new List<Guid>();

                var rgx = new Regex(GuldPattern);

                //для каждой вкладки...
                foreach (var workSheet in WorkFeed)
                {
                    //получаем список текста проверок и их гуидов
                    var list = Query(new CellQuery(workSheet.CellFeedLink)
                    {
                        MinimumColumn = ColumnForCheck,
                        MaximumColumn = ColumnForGulds,
                        ReturnEmpty = ReturnEmptyCells.yes
                    });

                    foreach (var entry in list[ColumnForGulds]) //Перебор всех ячеек страницы чеклиста
                    {
                        var guidMatch = rgx.Match(entry.Value.Content.Content);

                        //Проверка, что в ячейке - GUID, выход, если нет
                        if (!guidMatch.Success)
                            continue;

                        //текста проверки
                        var check = list[entry.Key, ColumnForCheck];
                        //текст результата
                        var result = list[entry.Key, ColumnForResult];

                        string text;
                        if (check == null || result == null)
                            text = "Не нашел текста для данного гуида";
                        else
                            text = "Проверка: " + check.Content.Content + Environment.NewLine + "Ожидание: " +
                                   result.Content.Content;

                        var guid = Guid.Parse(guidMatch.Value);

                        if (!guidTexts.ContainsKey(guid))
                            guidTexts.Add(guid, text);
                        else
                        {
                            existingGuids.Add(guid);
                        }
                    }
                }

                //если вдруг есть повторяющиеся гуиды (случайный копипаст)
                //тогда не понятно что делать - роняем этот таск с исключением.
                if (existingGuids.Any())
                {
                    var str = existingGuids.Aggregate("",
                        (current, existingGuid) => current + (existingGuid + Environment.NewLine));
                    throw new ArgumentException("Гуиды уже есть в списке:" + Environment.NewLine + str);
                }

                return guidTexts;
            });

            return _guidTexts;
        }

        /// <summary>
        /// Данная часть ниже чуток заброшена...
        /// Использовалась в LoadTest, но почти не рефакторилась
        /// Хоть и рабочая, но как именно рабочая хз =)
        /// </summary>

        private static void InitDriveService()
        {
            var initializer =
                new ServiceAccountCredential.Initializer(ParametersInit.GoogleServiceAccountEmail)
                {
                    Scopes = new[] { DriveService.Scope.Drive, "" },
                    User = ParametersInit.GoogleLogin
                }.FromCertificate(Certificate);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new ServiceAccountCredential(initializer),
                ApplicationName = "MyApp",
            });
        }

        /// <summary>
        /// Создание файла реализованно через копирование уже существующего
        /// это нужно чтобы так же скопировать "права" на управление файлом
        /// </summary>
        /// <returns></returns>
        private static string CopyFile()
        {
            var b = Execute(_driveService.Files.List());

            var file =
                b.Items.First(
                    x => x.Title == "LoadTest" && (x.ExplicitlyTrashed == false || x.ExplicitlyTrashed == null));

            var perm = Execute(_driveService.Permissions.List(file.Id)).Items.First(t => t.EmailAddress == ParametersInit.GoogleServiceAccountEmail);

            Execute(_driveService.Files.Copy(file, file.Id));

            b = Execute(_driveService.Files.List());

            var newFile =
                b.Items.First(
                    x => x.Title == "LoadTest" && (x.ExplicitlyTrashed == false || x.ExplicitlyTrashed == null));
            newFile.Title = "LoadTestRun " + DateTime.Today.ToShortDateString();
            newFile.Permissions = new[] { perm };

            Execute(_driveService.Files.Update(newFile, newFile.Id));
            Execute(_driveService.Permissions.Insert(perm, newFile.Id));

            return newFile.Title;
        }

        /// <summary>
        /// Создание таблицы
        /// </summary>
        /// <param name="fileName"></param>
        private static void InitSpreadsheet(string fileName)
        {
            var query = new SpreadsheetQuery { Title = fileName };
            var spreadsheetfeed = Service.Query(query);

            if (spreadsheetfeed.Entries.Count == 0)
                throw new ArgumentException("Файл " + fileName + " не найден");

            _spreadsheet = (SpreadsheetEntry)spreadsheetfeed.Entries.First();
        }

        /// <summary>
        /// Выполнение batch запроса в цикле, до тех пор, пока он не выполнится
        /// (бывают косяки, что он отваливается по странным причинам)
        /// </summary>
        /// <param name="feed"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static AtomFeed ExecuteBatch(AtomFeed feed, Uri uri)
        {
            var bo = false;
            AtomFeed result = null;
            var ex = new Exception();

            for (var i = 0; i < 10; i++)
            {
                if (bo)
                    break;

                try
                {
                    result = Service.Batch(feed, uri);
                    bo = true;
                }
                catch (Exception e)
                {
                    ex = e;
                }
            }

            if (bo)
                return result;

            throw ex;
        }

        /// <summary>
        /// Аналогично ExecuteBatch
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="executer"></param>
        /// <returns></returns>
        private static T Execute<T>(IClientServiceRequest<T> executer)
        {
            var bo = false;
            var result = default(T);
            var ex = new Exception();

            for (var i = 0; i < 10; i++)
            {
                if (bo)
                    break;

                try
                {
                    result = executer.Execute();
                    bo = true;
                }

                catch (Exception e)
                {
                    ex = e;
                }
            }

            if (bo)
                return result;

            throw ex;
        }

        /// <summary>
        /// Создание вкладки
        /// </summary>
        /// <param name="name"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static WorksheetEntry AddWorkSheet(string name, int count)
        {
            if (name == "MainPage")
                return (WorksheetEntry)_spreadsheet.Worksheets.Entries.First();

            return Service.Insert(_spreadsheet.Worksheets,
                new WorksheetEntry { Title = { Text = name }, Cols = 14, Rows = (uint)count + 10 });
        }

        /// <summary>
        /// Сохранение результатов нагрузочного тестирования
        /// </summary>
        /// <param name="result"></param>
        public static void SaveResult(List<OutPutClass> result)
        {
            InitSpreadsheet(CopyFile());
            SaveMainPage(result);

            while (result.Count > 0)
            {
                var newList = result.FindAll(x => x.Name == result[0].Name);
                SaveRun(AddWorkSheet(newList.First().Name, newList.Count), newList);
                result.RemoveAll(x => x.Name == newList.First().Name);
            }
        }

        private static void SaveRun(WorksheetEntry worksheet, List<OutPutClass> result)
        {
            var entitesList = Query(new CellQuery(worksheet.CellFeedLink)
            {
                ReturnEmpty = ReturnEmptyCells.yes
            });

            var cellForBatch = new List<CellForBatch>
            {
                new CellForBatch(1, 1, "TestName"),
                new CellForBatch(1, 2, "ThreadNumber"),
                new CellForBatch(1, 3, "StartTime"),
                new CellForBatch(1, 4, "Duration"),
            };

            for (uint row = 0; row < result.Count; row++)
            {
                cellForBatch.Add(new CellForBatch(row + 2, 1, result[(int)row].Name));
                cellForBatch.Add(new CellForBatch(row + 2, 2, result[(int)row].TestNumber.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 3, result[(int)row].StartTime.ToLongTimeString()));
                cellForBatch.Add(new CellForBatch(row + 2, 4, result[(int)row].Duration.ToString()));
            }

            MakeBatchRequest(entitesList, cellForBatch);
        }

        private static void SaveMainPage(List<OutPutClass> tempresult)
        {
            var result = tempresult.FindAll(x => true);

            var mathList = new List<MathClass>();
            while (result.Count > 0)
            {
                var newList = result.FindAll(x => x.Name == result[0].Name && x.TestNumber == result[0].TestNumber);
                result.RemoveAll(x => x.Name == newList.First().Name && x.TestNumber == newList.First().TestNumber);
                mathList.Add(DoMath(newList));
            }

            var worksheet = AddWorkSheet("MainPage", result.Count + 2);
            var entitesList = Query(new CellQuery(worksheet.CellFeedLink)
            {
                ReturnEmpty = ReturnEmptyCells.yes
            });

            var cellForBatch = new List<CellForBatch>
            {
                new CellForBatch(1, 1, "Name"),
                new CellForBatch(1, 2, "TestNumber"),
                new CellForBatch(1, 3, "ThreadNumber"),
                new CellForBatch(1, 4, "Mean"),
                new CellForBatch(1, 5, "Standard Deviation"),
                new CellForBatch(1, 6, "Sigma1"),
                new CellForBatch(1, 7, "Sigma2"),
                new CellForBatch(1, 8, "Sigma3"),
            };

            for (uint row = 0; row < mathList.Count; row++)
            {
                cellForBatch.Add(new CellForBatch(row + 2, 1, mathList[(int)row].Name));
                cellForBatch.Add(new CellForBatch(row + 2, 2, mathList[(int)row].TestNumber));
                cellForBatch.Add(new CellForBatch(row + 2, 3, mathList[(int)row].ThreadNumber.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 4, mathList[(int)row].Mean.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 5, mathList[(int)row].Sd.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 6, mathList[(int)row].Sigma1.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 7, mathList[(int)row].Sigma2.ToString()));
                cellForBatch.Add(new CellForBatch(row + 2, 8, mathList[(int)row].Sigma3.ToString()));
            }

            MakeBatchRequest(entitesList, cellForBatch);
        }

        private static MathClass DoMath(List<OutPutClass> result)
        {
            double mean = 0;
            result.ForEach(x => { mean += x.Duration; });
            mean = mean / result.Count;

            double sd = 0;
            result.ForEach(x => { sd += Math.Pow(mean - x.Duration, 2); });
            sd = Math.Sqrt(sd / result.Count);

            double sigma1 = result.Count(x => (mean - 1 * sd <= x.Duration) && (x.Duration <= mean + 1 * sd));
            sigma1 /= result.Count;
            double sigma2 = result.Count(x => (mean - 2 * sd <= x.Duration) && (x.Duration <= mean + 2 * sd));
            sigma2 /= result.Count;
            double sigma3 = result.Count(x => (mean - 3 * sd <= x.Duration) && (x.Duration <= mean + 3 * sd));
            sigma3 /= result.Count;

            return new MathClass { Name = result[0].Name, TestNumber = result[0].TestNumber.ToString(), ThreadNumber = result.Count, Mean = mean, Sd = sd, Sigma1 = sigma1, Sigma2 = sigma2, Sigma3 = sigma3 };
        }
    }

    public class MathClass
    {
        public string Name;
        public string TestNumber;
        public int ThreadNumber;
        public double Mean;
        public double Sd;
        public double Sigma1;
        public double Sigma2;
        public double Sigma3;
    }

    public class OutPutClass
    {
        public string Name;
        public int TestNumber;
        public long Duration;
        public DateTime StartTime;
        public string ErrorText;

        public OutPutClass(string name, long duration, int testNumber, DateTime startTime, string errorText = null)
        {
            Name = name;
            TestNumber = testNumber;
            StartTime = startTime;
            Duration = duration;
            ErrorText = errorText;
        }
    }

    /// <summary>
    /// Класс для более удобного хранения списка ячеек
    /// Реализованно перечисление и индексатор
    /// </summary>
    public class EntitesList : IEnumerable<CellEntry>
    {
        private readonly Dictionary<uint, Dictionary<uint, CellEntry>> _array = new Dictionary<uint, Dictionary<uint, CellEntry>>();
        private readonly IEnumerable<CellEntry> _entites;

        public Uri Self;
        public Uri Batch;
        public string Title;

        public EntitesList(CellFeed cellFeed)
        {
            Self = new Uri(cellFeed.Self);
            Batch = new Uri(cellFeed.Batch);
            Title = cellFeed.Title.Text;
            _entites = cellFeed.Entries.Cast<CellEntry>();

            foreach (var entite in _entites)
            {
                if (_array.ContainsKey(entite.Column))
                {
                    if (_array[entite.Column].ContainsKey(entite.Row))
                        _array[entite.Column][entite.Row] = entite;
                    else
                        _array[entite.Column].Add(entite.Row, entite);
                }
                else
                    _array.Add(entite.Column, new Dictionary<uint, CellEntry> { { entite.Row, entite } });
            }
        }

        public IEnumerator<CellEntry> GetEnumerator()
        {
            return _entites.GetEnumerator();
        }

        public CellEntry this[uint i, uint j]
        {
            get { return _array[j][i]; }
        }

        public Dictionary<uint, CellEntry> this[uint i]
        {
            get { return _array[i]; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entites.GetEnumerator();
        }
    }
}
