using System;
using System.Collections.Generic;
using AutoTest.helpers;
using NUnit.Framework;

namespace Mts
{
    public class Mts : AutoTest.Parallel
    {
        public override HubHolder GetHubHolder()
        {
            var hubHolder = new HubHolder(ParametersInit.GetAppConfig("Host"));
            return hubHolder;
        }

        public override List<ParametersProject> GetProjects()
        {
            var projects = new List<ParametersProject> { new ParametersProject(ProjectType.Mts) };
            LoadUbuntuProfile();
            Param.Smoke = true;

            var address = Environment.GetEnvironmentVariable("testAddress");
            if (!string.IsNullOrEmpty(address))
            {
                Param.NotProd = true;
                SetAddress(address);
            }

            return projects;
        }

        public override void PrepareProcess(HubHolder hubHolder)
        {

        }

        public override void ThrowException()
        {
            throw new SeleniumFailException("Mts тесты выявили ошибку");
        }

        public override void CreateColumnAndMailToStart()
        {

        }

        public override void SendResultToMailAndGoogle(string saveDir)
        {

        }

        public override void DefaultMailAndFolder(string saveDir)
        {

        }

        public override void ChangeLogin(ParametersInit param, LoginInfo login)
        {
            var post = new PostCommands(param);
            var action = post.Login();
            action.CompanySelect();
            action.RemoveAllCompanies();
            action.CreateCompany("Первая").NewEmployeeLabor("Первый чел епт", EmployeeType.Tdo);
        }

        public override void StartParallelLog(string address, DateTime timeStart, string screenPath, int testsCount)
        {

        }

        public override void EndParallelLog(Guid id, DateTime timeEnd)
        {

        }

        [Test]
        public override void ParallelInFor()
        {
            var folder = Environment.GetEnvironmentVariable("testFolder");

            InitParam();
            TestInParallel(folder);
        }
    }
}
