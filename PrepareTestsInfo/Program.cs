using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PrepareTestsInfo
{
    static class Program
    {
        private static readonly Regex CommentsMask = new Regex("(/\\*((?!\\*/).|\n)+\\*/)|(//.*)");
        private static readonly Regex GuidMask = new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}");
        private static readonly Regex JiraMask = new Regex(@"// *bug *(https{0,1}\://jira\.actidev\.ru/browse/)([\w]+\-\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex ClassName = new Regex(@"class ([\D]+)(\d+(?(?=_{1})_\d*))");

        static void Main(string[] args)
        {
            var testDir = new DirectoryInfo(args[1]);
            var xProject = new XElement("Project", 
                new XAttribute("projectDir", args[0]), 
                new XAttribute("testsDir", args[1]), 
                new XAttribute("projectPath", args[2])
            );
            var xDoc = GetDirectoryXml(testDir);
            xProject.Add(xDoc);
            var doc = new XDocument(xProject);
            doc.Save(args[0] + "testInfo.xml");
        }

        private static XElement GetDirectoryXml(DirectoryInfo path)
        {
            var xDir = new XElement("Dir", new XAttribute("name", path.Name));

            foreach (var file in path.GetFiles())
            {
                if (file.Name.EndsWith(".cs"))
                {
                    var xFile = new XElement("File", new XAttribute("name", file.Name));
                    AddGuidsAndLines(file.FullName, xFile);
                    xDir.Add(xFile);
                }
            }

            foreach (var subDir in path.GetDirectories())
                xDir.Add(GetDirectoryXml(subDir));

            return xDir;
        }

        private static void AddGuidsAndLines(string filePath, XContainer xFile)
        {
            using (var sr = new StreamReader(filePath))
            {
                var source = sr.ReadToEnd();
                var text = CommentsMask.Replace(source, "");
                var guids = GuidMask.Matches(text);

                var guidsList = new List<string>();

                var matchClassName = ClassName.Match(source);
                var className = matchClassName.Success ? matchClassName.Groups[1].Value + matchClassName.Groups[2].Value : "";
                xFile.Add(new XAttribute("className", className));

                foreach (var guid in guids)
                {
                    var guidUp = guid.ToString().ToUpper();
                    if (!guidsList.Contains(guidUp))
                    {
                        guidsList.Add(guidUp);
                        xFile.Add(new XElement("Guid", new XAttribute("value", guidUp)));
                    }
                }

                var lines = Regex.Split(source, @"\r?\n|\r");

                for (var i = 0; i < lines.Length; i++)
                {
                    var bug = JiraMask.Match(lines[i]);

                    if (bug.Success)
                    {
                        var href = bug.Groups[1].Value + bug.Groups[2].Value;
                        var key = bug.Groups[2].Value;
                        xFile.Add(new XElement("Bug", new XAttribute("href", href), new XAttribute("line", i+1), new XAttribute("key", key)));
                    }
                }
            }
        }
    }
}
