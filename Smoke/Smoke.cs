using System;
using System.Collections.Generic;
using AutoTest.helpers;
using NUnit.Framework;

namespace Smoke
{
    public class Smoke : AutoTest.Parallel
    {
        public override HubHolder GetHubHolder()
        {
            var hubHolder = new HubHolder(ParametersInit.GetAppConfig("Host"));
            return hubHolder;
        }

        public override List<ParametersProject> GetProjects()
        {
            var projects = new List<ParametersProject>{new ParametersProject(ProjectType.Smoke)};
            LoadUbuntuProfile();
            Param.Smoke = true;

            var address = Environment.GetEnvironmentVariable("testAddress");
            if (!string.IsNullOrEmpty(address))
                SetAddress(address);

            return projects;
        }

        public override void PrepareProcess(HubHolder hubHolder)
        {

        }

        public override void ThrowException()
        {
            throw new SeleniumFailException("Smoke тесты выявили ошибку");
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
