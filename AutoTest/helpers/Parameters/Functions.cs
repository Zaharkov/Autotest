using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using LingvoNET;

namespace AutoTest.helpers
{
    public enum MoneyStyle
    {
        Default,
        WithZeroKop,
        WithNamed,
        WithNamedAndZeroKop
    }

    public class ParametersFunctions
    {
        /// <summary>
        /// Проверка является ли строка GUID'ом
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>Да/Нет</returns>
        public static bool IsValidGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return false;

            return IsGuid.IsMatch(guid);
        }

        private static readonly Regex IsGuid = new Regex(@"^(\{{0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}\}{0,1})$");

        /// <summary>
        /// Вернуть текущую дату
        /// </summary>
        /// <param name="days">Сколько добавить дней к дате</param>
        /// <param name="months">Сколько добавить месяцев к дате</param>
        /// <param name="years">Сколько добавить лет к дате</param>
        /// <returns>Сегодняшняя дата с учетом добавленных дней/месяцев/лет</returns>
        public DateTime Today(int days = 0, int months = 0, int years = 0)
        {
            var datetime = DateTime.Now.Date;
            datetime = datetime.AddYears(years);
            datetime = datetime.AddMonths(months);
            datetime = datetime.AddDays(days);
            return datetime;
        }

        /// <summary>
        /// Первый день квартала
        /// к которому принадлежит дата
        /// </summary>
        /// <param name="date">Дата</param>
        /// <returns>Первый день квартала</returns>
        public DateTime GetFirstDayOfKvartal(DateTime date)
        {
            return GetFirstDay(date);
        }

        /// <summary>
        /// Первый день среднего месяца квартала (2,5,8,11)
        /// к которому принадлежит дата
        /// </summary>
        /// <param name="date">Дата</param>
        /// <returns>Первый день среднего месяца квартала</returns>
        public DateTime GetMiddleDayOfKvartal(DateTime date)
        {
            return new DateTime(date.Year, date.AddMonths(
                    (date.Month % 3 == 0) ?
                        -1 : (date.Month % 3 == 1) ?
                            1 : 0
                ).Month, 1);
        }

        /// <summary>
        /// Первый день года 
        /// к которому принадлежит дата
        /// </summary>
        /// <param name="date">Дата</param>
        /// <returns>Первый день года</returns>
        public DateTime GetFirstDayOfYear(DateTime date)
        {
            return GetFirstDay(date, "year");
        }

        /// <summary>
        /// Первый день месяца
        /// к которому принадлежит дата
        /// </summary>
        /// <param name="date">Дата</param>
        /// <returns>Первый день месяца</returns>
        public DateTime GetFirstDayOfMonth(DateTime date)
        {
            return GetFirstDay(date, "month");
        }

        public string GetCompanyName(string nomer)
        {
            var name = "";
            nomer.ToList().ForEach(x =>
            {
                int i;
                if(int.TryParse(x.ToString(), out i))
                    name += IntToString(x) + " ";
            });
            return name.Substring(0, name.Length - 1);
        }

        /// <summary>
        /// Вспомогательная функция
        /// для получения первых дней
        /// </summary>
        /// <param name="date">Дата</param>
        /// <param name="of"></param>
        /// <returns></returns>
        private static DateTime GetFirstDay(DateTime date, string of = "quarter")
        {
            var month = date.Month;
            var day = date.Day;

            switch (of)
            {
                case "month": return date.AddDays(1 - day);
                case "quarter": return date.AddDays(1 - day).AddMonths(1 - ((month % 3) != 0 ? (month % 3) : 3));
                case "year": return date.AddDays(1 - day).AddMonths(1 - month);
                default:
                    return date;
            }
        }

        /// <summary>
        /// Список названий месяцев на русском
        /// </summary>
        private readonly Dictionary<int, string> _monthsName =
            new Dictionary<int, string>
            {
                {1, "январь"},   
                {2, "февраль"},  
                {3, "март"},     
                {4, "апрель"},   
                {5, "май"},      
                {6, "июнь"},     
                {7, "июль"},     
                {8, "август"},   
                {9, "сентябрь"}, 
                {10, "октябрь"}, 
                {11, "ноябрь"},  
                {12, "декабрь"}, 
            };

        /// <summary>
        /// Получить название месяца на русском
        /// </summary>
        /// <param name="number">Номер месяца</param>
        /// <param name="low">Заглавная ли буква (по умолчанию маленькая)</param>
        /// <param name="padej">Какой падеж (по умолчанию именительный)</param>
        /// <returns>Название месяца на русском</returns>
        public string MonthsName(int number, bool low = true, Case padej = Case.Nominative)
        {
            var month = Nouns.FindOne(_monthsName[number])[padej];
            month = low ? month : month.Substring(0, 1).ToUpper() + month.Remove(0, 1);

            return month;
        }

        /// <summary>
        /// Получить дату ввиде 'месяц yyyy'
        /// </summary>
        /// <param name="date">Дата</param>
        /// <param name="low">Месяц с маленьком буквы или нет</param>
        /// <param name="yearShow">Показывать ли год</param>
        /// <returns>Дата ввиде 'месяц yyyy'</returns>
        public string GetPeriod(DateTime date, bool low = false, bool yearShow = true)
        {
            var month = date.Month;
            var year = date.Year;

            var strMonth = MonthsName(month, low);

            return strMonth + (yearShow ? " " + year : "");
        }

        /// <summary>
        /// Получить XPath из текста
        /// </summary>
        /// <param name="text">Текст</param>
        /// <param name="chained">Текст</param>
        /// <returns>XPath</returns>
        public static string GetXPath(string text, string chained = null)
        {
            return "//*[(translate(@data-qa, '\xA0', ' ')='" + text + "')]" + (chained ?? "");
        }

        public ParamButton GetButton(string text, string chained = null)
        {
            return new ParamButton("//*[(translate(@data-qa, '\xA0', ' ')='" + text + "')]" + (chained ?? ""));
        }

        /// <summary>
        /// Получить XPath с номером
        /// </summary>
        /// <param name="link">Входной XPath</param>
        /// <param name="count">Номер</param>
        /// <returns>XPath</returns>
        public static string GetXPathCount(string link, int count)
        {
            const string pattern = "\\[\\d+\\]$";
            var rep = "[" + count + "]";
            var rgx = new Regex(pattern);
            
            if (rgx.IsMatch(link))
                return rgx.Replace(link, rep, 1);

            return "(" + link + ")[" + count + "]";
        }

        public static string DefaultXPathCount(string link)
        {
            const string pattern = "\\)\\[\\d+\\]$";
            var rgx = new Regex(pattern);

            if (rgx.IsMatch(link))
                return rgx.Replace(link, "", 1).Remove(0, 1);

            return link;
        }

        /// <summary>
        /// Опиши это Настя
        /// </summary>
        /// <param name="date"></param>
        /// <param name="count"></param>
        /// <param name="spryagenie"></param>
        /// <param name="fullname"></param>
        /// <returns></returns>
        public string MonthName(DateTime date, int count = 0, bool spryagenie = false, bool fullname = false)
        {
            var month = date.Month;
            var day = date.Day;

            var newMonth = month + count;

            if (newMonth > 12)
                while (newMonth > 12)
                    newMonth = newMonth - 12;

            if (newMonth <= 0)
                while (newMonth <= 0)
                    newMonth = newMonth + 12;

            if (!fullname)
                return !spryagenie ? MonthsName(newMonth) : MonthsName(newMonth, true, Case.Genitive);

            return day + " " + (!spryagenie ? MonthsName(newMonth) : MonthsName(newMonth, true, Case.Genitive));
        }

        /// <summary>
        /// Перевод русской строчки в транслит
        /// </summary>
        /// <param name="value">Строчка на русском</param>
        /// <returns>Транслит</returns>
        public string Rus2Translit(string value)
        {
            return Transliteration.Front(value).ToLower();
        }

        /// <summary>
        /// Является ли год високосным
        /// </summary>
        /// <param name="year">Год</param>
        /// <returns>Да/Нет</returns>
        public bool VisYear(int year)
        {
            if (year % 4 == 0)
            {
                if (year % 100 == 0)
                {
                    if (year % 400 == 0)
                        return true;

                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Получить последний день месяца заданной даты
        /// </summary>
        /// <param name="date">Дата</param>
        /// <returns>Последний день месяца</returns>
        public DateTime GetEndDayOfMonth(DateTime date)
        {
            date = date.AddMonths(1);
            date = date.AddDays(-date.Day);

            return date;
        }

        /// <summary>
        /// Дни ввиде 'N дней' с учетом спряжения
        /// </summary>
        /// <param name="number">Число дней</param>
        /// <param name="addWord">Подставить строку 'N "строка" дней' с учетом спряжения</param>
        /// <param name="dayReturn"></param>
        /// <returns>'N ("строка") дней' с учетом спряжения</returns>
        public string DaysInWords(int number, string addWord = "", bool dayReturn = true)
        {
            //http://yeaahcode.blogspot.ru/2015/01/lingvonet.html

            var lastDay = number % 10;
            var word = Nouns.FindOne("день");
            var text = Adjectives.FindOne(addWord);

            var numberText1 = word[Case.Nominative];
            var wordText1 = text != null ? text[Case.Nominative, Gender.M] : null;

            var numberText2 = word[Case.Genitive];
            var wordText2 = text != null ? text[Case.Genitive, Gender.P] : null;

            var numberText3 = word[Case.Genitive, Number.Plural];
            var wordText3 = text != null ? text[Case.Genitive, Gender.P] : null;

            string outputNumber;
            string outputWord;

            if (lastDay == 1)
            {
                if (number != 11)
                {
                    outputNumber = numberText1;
                    outputWord = wordText1;
                }
                else
                {
                    outputNumber = numberText3;
                    outputWord = wordText3;
                }
            }
            else if (lastDay > 1 && lastDay < 5)
            {
                if (number < 12 || number > 14)
                {
                    outputNumber = numberText2;
                    outputWord = wordText2;
                }
                else
                {
                    outputNumber = numberText3;
                    outputWord = wordText3;
                }
            }
            else
            {
                outputNumber = numberText3;
                outputWord = wordText3;
            }

            outputNumber = dayReturn ? " " + outputNumber : "";

            return number + (addWord == "" ? outputNumber : (" " + outputWord + outputNumber));
        }

        /// <summary>
        /// Вернуть более ранюю дату из двух
        /// </summary>
        /// <param name="date1">Первая дата</param>
        /// <param name="date2">Вторая дата</param>
        /// <param name="invert">Вернуть более позднюю дату</param>
        /// <returns>Раняя/Поздняя дата из двух</returns>
        public DateTime GetLaterDate(DateTime date1, DateTime date2, bool invert = true)
        {
            return date2.Subtract(date1).Days > 0 ? (invert ? date2 : date1) : (invert ? date1 : date2);
        }

        public T GetParam<T>(string name) where T : class
        {
            var type = Parameters.Instance().GetType().InvokeMember(name, BindingFlags.GetField, null, this, new object[] { }) as T;

            if (type == null)
                throw new NullReferenceException("Не смогло получить " + typeof(T).Name + " с названием '" + name + "'");

            return type;
        }

        public string IntToString(char b)
        {
            switch (b)
            {
                case '0':
                    return "Ноль";
                case '1':
                    return "Один";
                case '2':
                    return "Два";
                case '3':
                    return "Три";
                case '4':
                    return "Четыре";
                case '5':
                    return "Пять";
                case '6':
                    return "Шесть";
                case '7':
                    return "Семь";
                case '8':
                    return "Восемь";
                case '9':
                    return "Девять";
                default:
                    return null;
            }
        }

        public string IntToString(int b)
        {
            var array = b.ToString().ToCharArray();
            var result = "";

            foreach (var charInt in array)
            {
                result += IntToString(charInt) + " ";
            }

            return result.Remove(result.Count() - 1);
        }
    }

    /// <summary>
    /// Типы ошибок
    /// </summary>
    [Flags]
    public enum ErrorType
    {
        Ok = 0x0001,
        NotOk = 0x0002,
        Timed = 0x0004,
        NoScreen = 0x0008,
        Typo = 0x0010,
        Failed = 0x0020,
        Exception = 0x0040,
        TestFunc = 0x0080,
        CatchEx = 0x0100,
        Bug = 0x0200
    }

    /// <summary>
    /// Класс ошибок
    /// Содержит всю информацию о проходе тестов
    /// </summary>
    public static class ErrorInfo
    {
        private static readonly Stopwatch SWatch = new Stopwatch();

        //private static readonly object LockObj = new object();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ForError, ErrorType>> ErrorTextHolder =
            new ConcurrentDictionary<string, ConcurrentDictionary<ForError, ErrorType>>();
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, GuidFlags>> GuidErrors =
            new ConcurrentDictionary<Guid, ConcurrentDictionary<string, GuidFlags>>(); 

        static ErrorInfo()
        {
            SWatch.Start();
        }

        public static void DefaultFiles(string path)
        {
            if (File.Exists(path + "errors.log"))
                File.Delete(path + "errors.log");
            if (File.Exists(path + "errors_bug.log"))
                File.Delete(path + "errors_bug.log");
            if (File.Exists(path + "Click.csv"))
                File.Delete(path + "Click.csv");
        }

        /// <summary>
        /// Добавление ошибки
        /// </summary>
        /// <param name="classMethodName">Имя класса и метода в котором произошла ошибка</param>
        /// <param name="text">Текст ошибки (может быть null)</param>
        /// <param name="typo">Тип ошибки</param>
        public static void AddErrorText(string classMethodName, string text, ErrorType typo)
        {
            var conDic = new ConcurrentDictionary<ForError, ErrorType>();
            conDic.AddOrUpdate(new ForError(text, DateTime.Now), typo, (key, value) => typo);

            ErrorTextHolder.AddOrUpdate(classMethodName, conDic, (key, value) =>
            {
                value.AddOrUpdate(new ForError(text, DateTime.Now), typo, (key2, value2) => typo); 
                return value; 
            });
        }

        public static void DefaultClassError(string classMethodName)
        {
            ConcurrentDictionary<ForError, ErrorType> conDir;
            ErrorTextHolder.TryRemove(classMethodName, out conDir);

            foreach (var guidError in GuidErrors)
            {
                foreach (var classError in guidError.Value)
                {
                    GuidFlags flag;
                    if (classMethodName == classError.Key)
                        guidError.Value.TryRemove(classError.Key, out flag);
                } 
            }
        }

        public static bool IsHaveNoError(string className = null)
        {
            var result = true;

            foreach (var classError in ErrorTextHolder)
            {
                if (className == null)
                {
                    foreach (var error in classError.Value)
                    {
                        if (!error.Value.HasFlag(ErrorType.Bug) &&
                            (error.Value.HasFlag(ErrorType.Failed) ||
                             error.Value.HasFlag(ErrorType.Exception) ||
                             error.Value.HasFlag(ErrorType.Typo)))
                            result = false;
                    }
                }
                else if (classError.Key.Contains(className))
                {
                    foreach (var error in classError.Value)
                    {
                        if (!error.Value.HasFlag(ErrorType.Bug) &&
                            (error.Value.HasFlag(ErrorType.Failed) ||
                             error.Value.HasFlag(ErrorType.Exception) ||
                             error.Value.HasFlag(ErrorType.Typo)))
                            result = false;
                    }
                }
            }

            return result;
        }

        public static bool IsBugInFail(string className)
        {
            var result = false;

            foreach (var classError in ErrorTextHolder)
            {
                if (classError.Key.Contains(className))
                {
                    foreach (var error in classError.Value)
                    {
                        if (error.Value.HasFlag(ErrorType.Bug) &&
                            error.Value.HasFlag(ErrorType.Failed))
                            result = true;
                    }
                }
            }

            return result;
        }

        public static string AddErrorFromCatch(Exception e, string path, string logFile, string time) //TODO переделать это нада...
        {
            var stackTrace = Environment.StackTrace;

            var str = "в ";
            var start = stackTrace.IndexOf(str + "AutoTest.", StringComparison.CurrentCulture);

            if (start == -1)
            {
                str = "at ";
                start = stackTrace.IndexOf(str + "AutoTest.", StringComparison.CurrentCulture);
            }

            var end = stackTrace.IndexOf(str + "System.RuntimeMethodHandle.InvokeMethod", StringComparison.CurrentCulture);

            if(end == -1)
                end = stackTrace.IndexOf(str + "System.Threading.ExecutionContext", StringComparison.CurrentCulture);

            stackTrace = end != -1 ? stackTrace.Substring(start, end - start) : stackTrace.Substring(start);

            var error = e.Message + "(" + DateTime.Now.ToShortTimeString() + ")" +
                Environment.NewLine + stackTrace.Replace(path.Replace("C:\\", "c:\\"), "") + Environment.NewLine;

            return error;
        }

        /// <summary>
        /// Получить текст всех ошибок без BUGs
        /// или ошибок конкретного теста
        /// </summary>
        /// <returns>Текст всех ошибок</returns>
        public static string GetErrorText(ErrorResultType type, string className = null)
        {
            var text = "";

            foreach (var classError in OrderConcurrentDictionary())
            {
                if (className != null && !classError.Key.Contains(className))
                    continue;

                var bugInFail = false;
                foreach (var error in classError.Value)
                {
                    if (!bugInFail)
                        bugInFail = error.Value.HasFlag(ErrorType.Failed | ErrorType.Bug);
                }

                foreach (var error in classError.Value)
                {
                    if (!error.Value.HasFlag(ErrorType.Ok))
                    {
                        var erText = error.Key.Text + Environment.NewLine + Environment.NewLine;
                        switch (type)
                        {
                            case ErrorResultType.All:
                                text += erText;
                                break;
                            case ErrorResultType.WithOutBug:
                            {
                                if (error.Value.HasFlag(ErrorType.NotOk) && bugInFail)
                                    continue;

                                text += error.Value.HasFlag(ErrorType.Bug) ? "" : erText;

                                break;
                            }
                            case ErrorResultType.WithBug:
                            {
                                if (error.Value.HasFlag(ErrorType.NotOk) && bugInFail)
                                    text += erText;
                                else
                                    text += !error.Value.HasFlag(ErrorType.Bug) ? "" : erText;

                                break;
                            }
                            default:
                                text += erText;
                                break;
                        }
                    }
                }
            }

            return text;
        }

        public enum ErrorResultType
        {
            All,
            WithBug,
            WithOutBug
        }

        public class ForError
        {
            public string Text;
            public DateTime Time;
            public ForError(string text, DateTime time)
            {
                Text = text;
                Time = time;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        private static Dictionary<string, Dictionary<ForError, ErrorType>> OrderConcurrentDictionary()
        {
            return ErrorTextHolder
                .OrderBy(t => t.Key)
                .ToDictionary(classError => classError.Key, 
                    classError => classError.Value
                        .OrderBy(t => t.Key.Time)
                        .ToDictionary(t => t.Key, t => t.Value)
                );
        }

        private static Dictionary<string, string> GetErrorsFromArray(Dictionary<string, string> testMails)
        {
            var result = new Dictionary<string, string>();

            foreach (var classError in OrderConcurrentDictionary())
            {
                var bugInFail = false;
                foreach (var error in classError.Value)
                {
                    if (!bugInFail)
                        bugInFail = error.Value.HasFlag(ErrorType.Failed | ErrorType.Bug);
                }

                foreach (var error in classError.Value)
                {
                    if ((error.Value.HasFlag(ErrorType.NotOk) && bugInFail) ||
                        error.Value.HasFlag(ErrorType.Bug))
                        continue;

                    foreach (var testMail in testMails)
                    {
                        if (classError.Key.Contains(testMail.Key) && !error.Value.HasFlag(ErrorType.Ok))
                        {
                            if (result.ContainsKey(testMail.Value))
                                result[testMail.Value] += error.Key.Text + Environment.NewLine + Environment.NewLine;
                            else
                                result.Add(testMail.Value, error.Key.Text + Environment.NewLine + Environment.NewLine);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получить результаты прохода тестов
        /// </summary>
        /// <returns>Отчет о проходе тестов</returns>
        private static string GetResult()
        {
            var ok = 0;
            var notOk = 0;
            var timed = 0;
            var screen = 0;
            var typo = 0;
            var failed = 0;
            var exception = 0;
            var testFunc = 0;
            var bug = 0;

            foreach (var classError in ErrorTextHolder)
            {
                foreach (var error in classError.Value)
                {
                    if (error.Value.HasFlag(ErrorType.Ok)) ok++;
                    if (error.Value.HasFlag(ErrorType.NotOk)) notOk++;
                    if (error.Value.HasFlag(ErrorType.Timed)) timed++;
                    if (error.Value.HasFlag(ErrorType.NoScreen)) screen++;
                    if (error.Value.HasFlag(ErrorType.Typo)) typo++;
                    if (error.Value.HasFlag(ErrorType.Failed)) failed++;
                    if (error.Value.HasFlag(ErrorType.Exception)) exception++;
                    if (error.Value.HasFlag(ErrorType.TestFunc)) testFunc++;
                    if (error.Value.HasFlag(ErrorType.Bug) && !error.Value.HasFlag(ErrorType.Timed)) bug++;
                }
            }

            var text = new StringBuilder()
                .Append(Environment.NewLine + "Стартовало тестов: " + (ok + notOk))
                .Append(Environment.NewLine + "Из них прошло до конца: " + ok + ". Не прошло до конца: " + notOk + " (ошибок webDriver'а - " + failed + ")")
                .Append(exception > 0 ? Environment.NewLine + "Ексепшенов: " + exception : "")
                .Append(typo > 0 ? Environment.NewLine + "Мелких ошибок (не приводящие к падению): " + typo : "")
                .Append(bug > 0 ? Environment.NewLine + "BUG: " + bug : "")
                .Append(timed > 0 ? Environment.NewLine + "Загрузка страницы превысила " + ParametersInit.TimeOutForLog + " сек: " + timed : "")
                .Append(screen > 0 ? Environment.NewLine + "Не смогло сделать скрин: " + screen : "")
                .Append(testFunc > 0 ? Environment.NewLine + "Ошибки связанные с функционалом тестирования: " + testFunc : "")
                .Append(Environment.NewLine + "Время прохождения тестов: " + SWatch.Elapsed.Hours + ":" + SWatch.Elapsed.Minutes + ":" + SWatch.Elapsed.Seconds);

            return text.ToString();
        }

        /// <summary>
        /// Печать ошибок и результатов в файл errors.log (перезапись файла)
        /// </summary>
        /// <param name="path">Путь где будет храниться файл</param>
        public static void PrintErrorsAndResult(string path)
        {
            Console.WriteLine(@"Результат" + Environment.NewLine);
            var text = GetErrorText(ErrorResultType.WithOutBug) + GetResult();
            Console.WriteLine(text);
            PathCommands.CreateDir(path);
            File.WriteAllText(path + "errors.log", text);
            var textBug = GetErrorText(ErrorResultType.WithBug);
            File.WriteAllText(path + "errors_bug.log", textBug);
        }

        /// <summary>
        /// Печать ошибок в консоль и в файл errors.log (добавление записи)
        /// </summary>
        /// <param name="path">Путь где будет храниться файл</param>
        public static void PrintErrorsTest(string path)
        {
            var text = Environment.NewLine + GetErrorText(ErrorResultType.All);
            Console.WriteLine(text);
            PathCommands.CreateDir(path);
            File.AppendAllText(path + "errors.log", text);
        }

        public static List<Guid> GuidCheck(string classMethodName)
        {
            var errors = new List<Guid>();

            foreach (var guidError in GuidErrors)
            {
                foreach (var classError in guidError.Value)
                {
                    if (classError.Key == classMethodName)
                    {
                        if (!classError.Value.HasFlag(GuidFlags.End))
                        {
                            errors.Add(guidError.Key);
                            GuidErrors[guidError.Key][classError.Key] |= GuidFlags.Failed;
                        }
                    }
                }
            }

            return errors;
        }

        public static string FailGuidsToStr(List<Guid> list)
        {
            var errors = "";

            if (list == null)
                return errors;

            foreach (var error in list)
            {
                if(errors == "")
                    errors = Environment.NewLine + "Фейл проверок(";

                errors += error + ", ";
            }

            if (!string.IsNullOrEmpty(errors))
                errors = errors.Substring(0, errors.Count() - 2) + ")";

            return errors;
        }

        public static string FailGuidsToHtml(List<Guid> list)
        {
            var errors = "";

            if (list == null)
                return errors;

            var guidsErrorTask = GoogleCommands.LoadGuidsText();
            guidsErrorTask.Wait();

            var guidsError = guidsErrorTask.IsFaulted 
                ? new Dictionary<Guid, string>() 
                : guidsErrorTask.Result;

            foreach (var error in list)
            {
                if (errors == "")
                    errors = Environment.NewLine + "Фейл проверок(";

                if (guidsError.ContainsKey(error))
                    errors += "<span title=\"" + guidsError[error].Replace("\"", "'") + "\">" + error + "</span>, ";
                else
                    errors += error + ", ";
            }

            if (!string.IsNullOrEmpty(errors))
                errors = errors.Substring(0, errors.Count() - 2) + ")";

            return errors;
        }

        public static string GuidStart(Guid guid, string classMethodName)
        {
            if (GuidErrors.ContainsKey(guid))
            {
                if (GuidErrors[guid].ContainsKey(classMethodName))
                {
                    if (!GuidErrors[guid][classMethodName].HasFlag(GuidFlags.End))
                        return ("Гуид '" + guid + "' уже был начат и при этом не был закончен");

                    if (GuidErrors[guid][classMethodName].HasFlag(GuidFlags.Failed))
                        GuidErrors[guid][classMethodName] = GuidFlags.Start | GuidFlags.Failed;
                    else
                        GuidErrors[guid][classMethodName] = GuidFlags.Start;
                }
                else
                    GuidErrors[guid].AddOrUpdate(classMethodName, GuidFlags.Start, (key, value) => GuidFlags.Start);
            }
            else
            {
                var conDic = new ConcurrentDictionary<string, GuidFlags>();
                conDic.AddOrUpdate(classMethodName, GuidFlags.Start, (key, value) => GuidFlags.Start);
                GuidErrors.AddOrUpdate(guid, conDic, (key, value) => conDic);
            }

            return null;
        }

        public static string GuidEnd(Guid guid, string classMethodName)
        {
            if (GuidErrors.ContainsKey(guid))
            {
                if (GuidErrors[guid].ContainsKey(classMethodName))
                {
                    if (!GuidErrors[guid][classMethodName].HasFlag(GuidFlags.Failed))
                        GuidErrors[guid][classMethodName] |= GuidFlags.Passed;

                    GuidErrors[guid][classMethodName] |= GuidFlags.End;
                }
                else
                    return ("Нет начала гуида '" + guid + "' для конца проверки");
            }
            else
                return ("Нет начала гуида '" + guid + "' для конца проверки");

            return null;
        }

        public static string GuidEnd(string classMethodName)
        {
            foreach (var guidError in GuidErrors)
            {
                foreach (var classError in guidError.Value)
                {
                    if (classError.Key == classMethodName)
                    {
                        if (!classError.Value.HasFlag(GuidFlags.Failed))
                            GuidErrors[guidError.Key][classError.Key] |= GuidFlags.Passed;

                        GuidErrors[guidError.Key][classError.Key] |= GuidFlags.End;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Получить результаты прохода проверок по GUID'ам
        /// </summary>
        /// <param name="infoTests">Список проверяемых тестов</param>
        /// <returns>Список "гуид => результ"</returns>
        private static Dictionary<Guid, string> GetGuidsResult(IEnumerable<TestInfo> infoTests)
        {
            /*Console.WriteLine("new Dictionary<string, Dictionary<string, GuidFlags>>\r\n{");
            GuidErrors.ToList().ForEach(
                t =>
                {
                    Console.WriteLine("    {\r\n        \"" + t.Key + "\", new Dictionary<string, GuidFlags>\r\n        {");
                    t.Value.ToList().ForEach(k => Console.WriteLine("            { \"" + k.Key + "\", GuidFlags." + k.Value + " }" + (t.Value.Last().Equals(k) ? "" : ",")));
                    Console.WriteLine("        }");
                    Console.WriteLine("    }" + (GuidErrors.Last().Equals(t) ? "" : ","));
                });
            Console.WriteLine("};");*/

            foreach (var guidError in GuidErrors)
            {
                foreach (var classError in guidError.Value)
                {
                    if (!classError.Value.HasFlag(GuidFlags.End))
                    {
                        var text = "Был найден гуид '" + guidError.Key + "' в тесте '" + classError.Key + "' который не был завершен";
                        Console.WriteLine(text);
                    }
                }
            }

            foreach (var infoTest in infoTests)
            {
                foreach (var guid in infoTest.GetGuids())
                {
                    foreach (var method in infoTest.MethodInfos)
                    {
                        var classMethodName = infoTest.ClassName + "." + method.Name;

                        if (GuidErrors.ContainsKey(guid))
                        {
                            if (!GuidErrors[guid].ContainsKey(classMethodName))
                                GuidErrors[guid].AddOrUpdate(classMethodName, GuidFlags.Unknown, (key, value) => GuidFlags.Unknown);
                        }
                        else
                        {
                            var conDic = new ConcurrentDictionary<string, GuidFlags>();
                            conDic.AddOrUpdate(classMethodName, GuidFlags.Unknown, (key, value) => GuidFlags.Unknown);
                            GuidErrors.AddOrUpdate(guid, conDic, (key, value) => conDic);
                        }
                    }
                }
            }

            var guidsError = new Dictionary<Guid, Dictionary<GuidFlags, int>>();

            foreach (var guidError in GuidErrors)
            {
                foreach (var classError in guidError.Value)
                {
                    if (guidsError.ContainsKey(guidError.Key))
                    {
                        if (!guidsError[guidError.Key].ContainsKey(GuidFlags.Failed)) 
                            guidsError[guidError.Key].Add(GuidFlags.Failed, 0);
                    }
                    else
                        guidsError.Add(guidError.Key, new Dictionary<GuidFlags, int>{{GuidFlags.Failed, 0}});

                    if (guidsError.ContainsKey(guidError.Key))
                    {
                        if (!guidsError[guidError.Key].ContainsKey(GuidFlags.Passed))
                            guidsError[guidError.Key].Add(GuidFlags.Passed, 0);
                    }
                    else
                        guidsError.Add(guidError.Key, new Dictionary<GuidFlags, int> { { GuidFlags.Passed, 0 } });

                    if (guidsError.ContainsKey(guidError.Key))
                    {
                        if (!guidsError[guidError.Key].ContainsKey(GuidFlags.Unknown))
                            guidsError[guidError.Key].Add(GuidFlags.Unknown, 0);
                    }
                    else
                        guidsError.Add(guidError.Key, new Dictionary<GuidFlags, int> { { GuidFlags.Unknown, 0 } });

                    if (classError.Value.HasFlag(GuidFlags.Failed))
                        guidsError[guidError.Key][GuidFlags.Failed]++;

                    if (classError.Value.HasFlag(GuidFlags.Passed))
                        guidsError[guidError.Key][GuidFlags.Passed]++;

                    if (classError.Value.HasFlag(GuidFlags.Unknown))
                        guidsError[guidError.Key][GuidFlags.Unknown]++;
                }
            }

            var resultGuids = new Dictionary<Guid, string>();

            foreach (var guidError in guidsError)
            {
                string resultName;
                if (guidError.Value[GuidFlags.Failed] == 0 && guidError.Value[GuidFlags.Passed] == 0)
                    resultName = "unknown";
                else if (guidError.Value[GuidFlags.Failed] == 0)
                    resultName = "passed";
                else
                    resultName = "failed";

                resultGuids.Add(guidError.Key, resultName 
                    + " (" + guidError.Value[GuidFlags.Passed]
                    + "/" + guidError.Value[GuidFlags.Failed]
                    + "/" + guidError.Value[GuidFlags.Unknown] + ")");
            }

            return resultGuids;
        }

        public static void SaveGuidResult(List<TestInfo> testInfos, string saveDir)
        {
            var guidResults = GetGuidsResult(testInfos);
            try
            {
                var result = GoogleCommands.SaveRun(guidResults);

                if (result.Any())
                {
                    Console.WriteLine(@"В чек-листе не найдены следующие гуиды");
                    foreach (var res in result)
                        Console.WriteLine(res.Key);
                }
            }
            catch (Exception)
            {
                if (File.Exists(saveDir + "guids.xml"))
                    File.Delete(saveDir + "guids.xml");

                using (var file = File.OpenWrite(saveDir + "guids.xml"))
                {
                    var serializer = new XmlSerializer(typeof (GuidsResult[]),
                        new XmlRootAttribute {ElementName = "items"});
                    serializer.Serialize(file,
                        guidResults.Select(kv => new GuidsResult {Guid = kv.Key, Value = kv.Value}).ToArray());
                }
                throw;
            }
        }

        public static void SaveGuidResultFromXml(string saveDir)
        {
            var guidResults = new Dictionary<Guid, string>();
            using (var file = File.OpenRead(saveDir + "guids.xml"))
            {
                var serializer = new XmlSerializer(typeof(GuidsResult[]),
                    new XmlRootAttribute { ElementName = "items" });
                var xml = (GuidsResult[])serializer.Deserialize(file);
                foreach (var guidsResult in xml)
                    guidResults.Add(guidsResult.Guid, guidsResult.Value);
            }

            var result = GoogleCommands.SaveRun(guidResults, false);

            if (result.Any())
            {
                Console.WriteLine(@"В чек-листе не найдены следующие гуиды");
                foreach (var res in result)
                    Console.WriteLine(res.Key);
            }
        }

        public static void SaveMailResult()
        {
            var errorsMail = new Dictionary<string, string>();

            var errors = GetErrorsFromArray(PathCommands.GetInfoMails());

            foreach (var error in errors)
            {
                if (errorsMail.ContainsKey(error.Key))
                    errorsMail[error.Key] += string.Format("{0}{1}{1}", error.Value, Environment.NewLine);
                else
                    errorsMail.Add(error.Key, string.Format("{0}{1}{1}", error.Value, Environment.NewLine));
            }

            foreach (var errorMail in errorsMail)
                MailCommands.Action.SendTestsResult(errorMail.Key, errorMail.Value);
        }
    }
}
