using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using OpenQA.Selenium;

namespace AutoTest.helpers.Selenium
{
    /// <summary>
    /// Контейнер выгруженного элемента
    /// </summary>
    public sealed class SelElement
    {
        /// <summary>
        /// Элемент
        /// </summary>
        public IWebElement Element;
        /// <summary>
        /// Xpath
        /// </summary>
        public string Link;
        /// <summary>
        /// Значение (может быть null)
        /// </summary>
        public string Attr;

        /// <summary>
        /// Создать объект выгруженного элемента
        /// </summary>
        /// <param name="element">элемент</param>
        /// <param name="link">Xpath</param>
        /// <param name="attr">значение (может быть опущено)</param>
        public SelElement(IWebElement element, string link, string attr = null)
        {
            Element = element;
            Link = link;
            Attr = attr;
        }
    }

    /// <summary>
    /// Контейнер для элемента селекта
    /// </summary>
    public sealed class SelSelect
    {
        /// <summary>
        /// Кнопка дропдауна
        /// </summary>
        public SelElement But;
        /// <summary>
        /// Выбранное значение в селекте
        /// </summary>
        public SelElement Item;

        /// <summary>
        /// Создать объект выгруженного селекта
        /// </summary>
        /// <param name="but">Кнопка дропдауна</param>
        /// <param name="item">Кнопка выбранного значения в селекте</param>
        public SelSelect(SelElement but, SelElement item)
        {
            But = but;
            Item = item;
        }
    }

    /// <summary>
    /// Контейнер для выгруженного XML
    /// </summary>
    public sealed class SelXml
    {
        private readonly XmlNode _xml;
        private readonly XmlNamespaceManager _ns;

        /// <summary>
        /// Создать объекта выгруженного XML
        /// </summary>
        /// <param name="xml">XML</param>
        /// <param name="ns">ns</param>
        public SelXml(XmlNode xml, XmlNamespaceManager ns)
        {
            if(xml == null)
                throw new ArgumentNullException("xml");

            _xml = xml;
            _ns = ns;
        }

        public XmlNode SelectSingleNode(string xpath, bool returnNull = false)
        {
            if (_ns != null)
                xpath = xpath.Replace("//", "//ns:");

            var node = _ns == null 
                ? _xml.SelectSingleNode("." + xpath)
                : _xml.SelectSingleNode("." + xpath, _ns);

            if (!returnNull && node == null)
                throw new ArgumentNullException("Не найден узел '" + xpath + "'");

            return node;
        }

        /// <summary>
        /// Получить контейнер для XML более низкого уровня вложенности
        /// </summary>
        /// <param name="xpath">Xpath поиска в XML</param>
        /// <returns>Контейнер для выгруженного XML</returns>
        public SelXml GetNode(string xpath)
        {
            return new SelXml(SelectSingleNode(xpath), _ns);
        }

        /// <summary>
        /// Получить контейнеры для XML более низкого уровня вложенности
        /// </summary>
        /// <param name="xpath">XPath поиска в XML</param>
        /// <returns>Контейнеры выгруженных XML</returns>
        public List<SelXml> GetNodes(string xpath)
        {
            if (_ns != null)
                xpath = xpath.Replace("//", "//ns:");

            var value = _ns == null
                ? _xml.SelectNodes("." + xpath)
                : _xml.SelectNodes("." + xpath, _ns);

            if (value == null)
                throw new ArgumentNullException("Не найден узел '" + xpath + "'");

            var nodes = new List<SelXml>();

            for (var i = 0; i < value.Count; i++)
                nodes.Add(new SelXml(value.Item(i), _ns));

            return nodes;
        }

        /// <summary>
        /// Получить значение из контейнера XML
        /// более низкого уровня вложенности
        /// </summary>
        /// <param name="xpath">XPath для поиска в XML</param>
        /// <returns>Выгруженное значение или null если не найдено</returns>
        public string GetNodeValue(string xpath)
        {
            var value = SelectSingleNode(xpath, true);

            if(value == null)
                return null;

            return value.InnerText;
        }

        /// <summary>
        /// Получить свойство текущего контейнера XML
        /// </summary>
        /// <param name="name">Имя свойства</param>
        /// <returns>Выгруженное свойство или null если не найдено</returns>
        public string GetAttr(string name)
        {
            var value = _xml.Attributes;

            if (value == null)
                return null;

            var attr = value.GetNamedItem(name);

            if (attr == null)
                return null;

            return attr.InnerText;
        }

        /// <summary>
        /// Получить свойство из контейнера XML
        /// более низкого уровня вложенности
        /// </summary>
        /// <param name="xpath">XPath для поиска в XML</param>
        /// <param name="name">Имя свойства</param>
        /// <returns>Выгруженное свойство или null если не найдено</returns>
        public string GetAttr(string xpath, string name)
        {
            var value = SelectSingleNode(xpath, true);

            if (value == null)
                return null;

            var valueAttr = value.Attributes;

            if (valueAttr == null)
                return null;

            var attr = valueAttr.GetNamedItem(name);

            if (attr == null)
                return null;

            return attr.InnerText;
        }

        public override string ToString()
        {
            return XDocument.Parse(_xml.OuterXml).ToString();
        }
    }
}
