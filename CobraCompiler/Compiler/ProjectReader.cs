using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CobraCompiler.Parse.Scopes;
using CobraCompiler.Scanning;

namespace CobraCompiler.Compiler
{
    class ProjectReader
    {
        public readonly string Path;
        public readonly string ProjectFilePath;
        private const string ProjectRoot = "Project";
        private const string ProjectName = "name";
        private const string SystemSet = "SystemSet";
        private const string System = "System";
        private const string SystemName = "name";
        private const string SystemPath = "path";
        private const string Module = "Module";
        private const string ModuleName = "name";
        private const string ModulePath = "path";

        public ProjectReader(string projectPath)
        {
            Path = projectPath;
            string[] projectFiles = Directory.GetFiles(projectPath, "*.cobraproj");

            if (projectFiles.Length == 0)
                throw new ArgumentException("No project file exists at given path");
            if (projectFiles.Length > 1)
                throw new ArgumentException("Multiple project files at given path");

            ProjectFilePath = projectFiles[0];

        }

        public Project ReadProject()
        {
            XmlDocument document = new XmlDocument();
            document.Load(ProjectFilePath);

            XmlNode root = document.SelectSingleNode(ProjectRoot);

            string projectName = root.Attributes[ProjectName].Value;

            List<System> systems = new List<System>();

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlNode child = root.ChildNodes.Item(i) ?? throw new IndexOutOfRangeException();
                switch (child.Name)
                {
                    case SystemSet:
                        systems.AddRange(ReadSystemSet(child, projectName));
                        break;
                }
            }

            return new Project(projectName, systems);
        }

        public IEnumerable<System> ReadSystemSet(XmlNode node, string projectName)
        {
            if (node == null)
                throw new NullReferenceException();

            List<System> systems = new List<System>();

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode child = node.ChildNodes.Item(i) ?? throw new IndexOutOfRangeException();

                switch (child.Name)
                {
                    case System:
                        systems.Add(ReadSystem(child, projectName));
                        break;
                }
            }

            return systems;
        }

        public System ReadSystem(XmlNode node, string systemPath)
        {
            if (node == null)
                throw new NullReferenceException();

            string systemName = node.Attributes[SystemName].Value;

            List<System> subSystems = new List<System>();
            List<Module> modules = new List<Module>();

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode child = node.ChildNodes.Item(i) ?? throw new IndexOutOfRangeException();

                switch (child.Name)
                {
                    case System:
                        subSystems.Add(ReadSystem(child, systemPath + CobraCompiler.Compiler.System.SystemPathDelimiter + systemName));
                        break;
                    case Module:
                        modules.Add(ReadModule(child, systemPath + CobraCompiler.Compiler.System.SystemPathDelimiter + systemName));
                        break;
                }
            }

            return new System(systemName, systemPath, subSystems, modules);
        }

        public Module ReadModule(XmlNode node, string systemPath)
        {
            return new Module(node.Attributes[ModuleName].Value, systemPath, new FileReader(Path + "\\" + node.Attributes[ModulePath].Value));
        }
    }
}
