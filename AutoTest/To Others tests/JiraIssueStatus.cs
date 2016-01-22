using System;
using System.Collections.Generic;
using System.Linq;
using AutoTest.helpers;
using NUnit.Framework;
using TechTalk.JiraRestClient;

namespace AutoTest.To_Others_Tests
{
    internal class JiraStatus
    {
        [Test]
        public void Test()
        {
            GetStatus();
        }

        private string GetIssueStatus(string task)
        {
            var jira = new JiraClient<IssueFields>("", "", "");
            var issue = new IssueRef { key = task };
            var response = jira.LoadIssue(issue);

            return response.fields.status.name;
        }

        private static IEnumerable<string> GetTask()
        {
            var testsInfo = PathCommands.GetTests();
            var result = new List<string>();
            foreach (var test in testsInfo)
            {
                foreach (var bug in test.Bugs)
                {
                    if (!result.Contains(bug.Key))
                        result.Add(bug.Key);
                }
            }

            return result;
        }

        public void GetStatus()
        {
            GetTask().AsParallel().All(t =>
            {
                var status = GetIssueStatus(t);
                Console.WriteLine(@"Task {0} has status {1}", t, status);
                return true;
            });
        }
    }
}
