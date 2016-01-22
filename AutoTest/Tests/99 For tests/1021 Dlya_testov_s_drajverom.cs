using System;
using AutoTest.helpers;
using NUnit.Framework;
using AutoTest.helpers.Parameters;
using AutoTest.helpers.Selenium;

namespace AutoTest.Tests._99_For_tests
{
    [AutoTest("183931EF-3075-4BDD-A9AE-7EA6ECD00463", ProjectOwners.Zaharkov)]
    class Prototype1021 : SeleniumCommands
    {
        [Test]
        public void Test()
        {
            Start();

            //Console.WriteLine(Get(new ParamButton("//test"), "class").Element.GetCssValue("color"));

        }
    }
}
