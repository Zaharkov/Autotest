using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace AutoTest.helpers.Parameters
{
    public interface IParam<out T>
    {
        string Link { get; }
        T SetCount(int value);
    }

    /// <summary>
    /// Контейнер для поля ввода
    /// </summary>
    public class ParamField : IParam<ParamField>
    {
        public string Link { get { return Field.Link; } }
        /// <summary>
        /// Элемент ввода
        /// </summary>
        public readonly ParamButton Field;
        /// <summary>
        /// Значение для поля ввода
        /// </summary>
        public readonly string Value;

        private readonly Dictionary<int, string> _values;

        /// <summary>
        /// Новый объект поля ввода
        /// </summary>
        /// <param name="link">XPath</param>
        /// <param name="values">Массив значений</param>
        public ParamField(string link, Dictionary<int, string> values = null)
        {
            Field = new ParamButton(link);

            if (values != null && values.Any())
            {
                _values = values;
                Value = _values.First().Value;
            }
        }

        /// <summary>
        /// Задать значение для поля
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Контейнер поля</returns>
        public ParamField SetValue(string value)
        {
            return new ParamField(Field.Link, new Dictionary<int, string> { { 1, value } });
        }

        /// <summary>
        /// Задать порядковый номер для Xpath
        /// </summary>
        /// <param name="value">Номер</param>
        /// <returns>Контейнер поля</returns>
        public ParamField SetCount(int value)
        {
            return new ParamField(ParametersFunctions.GetXPathCount(Field.Link, value), _values);
        }

        /// <summary>
        /// Переключится на дефолтное значение
        /// </summary>
        /// <param name="number">Номер значения</param>
        /// <returns>Контейнер поля</returns>
        public ParamField SwitchToValue(int number)
        {
            if (!_values.ContainsKey(number))
                throw new ArgumentOutOfRangeException("Объект ParamField не содержит значения с номером " + number);

            return SetValue(_values[number]);
        }

        /// <summary>
        /// Вернуть текущее заданное значение поля
        /// </summary>
        /// <returns>Значение</returns>
        public override string ToString()
        {
            return Value;
        }
    }

    /// <summary>
    /// Контейнер для кнопок
    /// </summary>
    public class ParamButton : IParam<ParamButton>
    {
        public string Link { get { return _link; } }
        /// <summary>
        /// Xpath кнопки
        /// </summary>
        private readonly string _link;

        /// <summary>
        /// Новый объект типа кнопки
        /// </summary>
        /// <param name="link"></param>
        public ParamButton(string link)
        {
            _link = link;
        }

        /// <summary>
        /// Возвращает Xpath кнопки
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _link;
        }

        /// <summary>
        /// Задает порядковый номер для Xpath
        /// </summary>
        /// <param name="value">Номер</param>
        /// <returns>Контейнер кнопки</returns>
        public ParamButton SetCount(int value)
        {
            return new ParamButton(ParametersFunctions.GetXPathCount(_link, value));
        }
    }

    /// <summary>
    /// Контейнер для селекта
    /// </summary>
    public class ParamSelect : IParam<ParamSelect>
    {
        public string Link { get { return _link; } }
        private readonly string _link;
        /// <summary>
        /// Значение селекта
        /// </summary>
        public readonly string Value;
        /// <summary>
        /// Кнопка открытия селетка
        /// </summary>
        public readonly ParamButton But;
        /// <summary>
        /// Кнопки выпадающего списка селетка
        /// </summary>
        public readonly ParamButton Item;
        /// <summary>
        /// Кнопка значения выпадающего списка
        /// </summary>
        public readonly ParamButton ItemValue;
        /// <summary>
        /// Кнопка изначального селекта
        /// </summary>
        public readonly ParamButton Select;

        private readonly bool _split;
        private readonly string _notContains;

        private readonly Dictionary<int, string> _values;

        /// <summary>
        /// Создание объекта типа селект
        /// </summary>
        /// <param name="link">XPath</param>
        /// <param name="values">Массив значений</param>
        /// <param name="notContains">Какой текст не должен содержать</param>
        /// <param name="split"></param>
        public ParamSelect(string link, Dictionary<int, string> values = null, string notContains = null, bool split = true)
        {
            _link = link;
            _split = split;
            _notContains = notContains;
            Select = new ParamButton(_link);
            const string pattern = "'\\)\\]";
            const string repBut = ".but')]";
            const string repItems = ".item')]";

            var rgx = new Regex(pattern);
            But = new ParamButton(rgx.Replace(link, repBut));
            Item = new ParamButton(ParametersFunctions.DefaultXPathCount(rgx.Replace(link, repItems)));

            if (values != null && values.Any())
            {
                _values = values;
                Value = _values.First().Value;

                var newXpath = repItems;

                if (_split)
                {
                    var words = Value.Split(' ');
                    words.ToList().ForEach(t => newXpath += "[contains(text(),'" + t + "') or node()[contains(., '" + t + "')]]");

                    if (notContains != null)
                    {
                        words = notContains.Split(' ');
                        words.ToList().ForEach(t => newXpath += "[not(contains(text(),'" + t + "') or node()[contains(., '" + t + "')])]");
                    }
                }
                else
                {
                    newXpath += "[text()='" + Value + "']";
                }

                ItemValue = new ParamButton(ParametersFunctions.DefaultXPathCount(rgx.Replace(link, newXpath)));
            }
        }

        /// <summary>
        /// Задать значени селекта
        /// </summary>
        /// <param name="value">Значение</param>
        /// <returns>Контейнер типа селекта</returns>
        public ParamSelect SetValue(string value)
        {
            return new ParamSelect(_link, new Dictionary<int, string> { { 1, value } }, _notContains, _split);
        }

        public ParamSelect NotContains(string text)
        {
            return new ParamSelect(_link, new Dictionary<int, string> { { 1, Value } }, text, _split);
        }

        public ParamSelect SetSplit(bool split)
        {
            return new ParamSelect(_link, _values, _notContains, split);
        }

        /// <summary>
        /// Задать порядковый номер для Xpath
        /// </summary>
        /// <param name="value">Номер</param>
        /// <returns>Контейнер типа селекта</returns>
        public ParamSelect SetCount(int value)
        {
            return new ParamSelect(ParametersFunctions.GetXPathCount(_link, value), _values, _notContains, _split);
        }

        /// <summary>
        /// Переключится на дефолтное значение селетка
        /// </summary>
        /// <param name="number">Номер</param>
        /// <returns>Контейнер типа селекта</returns>
        public ParamSelect SwitchToValue(int number)
        {
            if (!_values.ContainsKey(number))
                throw new ArgumentOutOfRangeException("Объект ParamSelect не содержит значения с номером " + number);

            return SetValue(_values[number]);
        }

        public Dictionary<int, string> GetValues()
        {
            return _values;
        }

        /// <summary>
        /// Возвращает текущее заданное значение селекта
        /// </summary>
        /// <returns>Значение селекта</returns>
        public override string ToString()
        {
            return Value;
        }
    }

    /// <summary>
    /// Контейнер для таблицы
    /// </summary>
    public class ParamTable : IParam<ParamTable>
    {
        public string Link { get { return _link; } }
        /// <summary>
        /// Xpath таблицы
        /// </summary>
        public readonly string XPath;

        private readonly string _link ;
        public readonly int Row;
        public readonly int Column;
        public readonly TableSwitcher Switcher;

        public readonly ParamButton TableDiv;
        public readonly ParamButton Current;

        public enum TableSwitcher
        {
            [Description("tbody")]
            Tbody,
            [Description("thead")]
            Thead,
            [Description("tfoot")]
            Tfoot
        }

        /// <summary>
        /// Создать объект контейнера таблицы
        /// </summary>
        /// <param name="link">Xpath</param>
        /// <param name="row">Строка таблицы (=1)</param>
        /// <param name="column">Столбец таблицы (=1)</param>
        /// <param name="switcher"></param>
        public ParamTable(string link, int row = 1, int column = 1, TableSwitcher switcher = TableSwitcher.Tbody)
        {
            _link = link;
            TableDiv = new ParamButton(link);
            Switcher = switcher;
            if (switcher == TableSwitcher.Thead)
                XPath = link + "/" + switcher.ToDescription() + "/tr[" + row + "]/th[" + column + "]";
            else
                XPath = link + "/" + switcher.ToDescription() + "/tr[" + row + "]/td[" + column + "]";
            Current = new ParamButton(XPath);
            Row = row;
            Column = column;
        }

        /// <summary>
        /// Xpath текущего значения в таблице
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return XPath;
        }

        /// <summary>
        /// Задать порядковый номер для Xpath
        /// </summary>
        /// <param name="value">Номер</param>
        /// <returns></returns>
        public ParamTable SetCount(int value)
        {
            return new ParamTable(ParametersFunctions.GetXPathCount(_link, value), Row, Column, Switcher);
        }

        /// <summary>
        /// Задать строчку в таблице
        /// </summary>
        /// <param name="row">Номер строки</param>
        /// <returns></returns>
        public ParamTable SetRow(int row)
        {
            return new ParamTable(_link, row, Column, Switcher);
        }

        /// <summary>
        /// Задать столбец в таблице
        /// </summary>
        /// <param name="column">Номер столбца</param>
        /// <returns></returns>
        public ParamTable SetColumn(int column)
        {
            return new ParamTable(_link, Row, column, Switcher);
        }

        /// <summary>
        /// В какой блок таблицы смотреть?
        /// </summary>
        /// <param name="switcher">Тип блока</param>
        /// <returns></returns>
        public ParamTable SetTableSpace(TableSwitcher switcher)
        {
            return new ParamTable(_link, Row, Column, switcher);
        }
    }
}
