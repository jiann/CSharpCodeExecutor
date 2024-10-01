using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeAnalysisLibStandard21;
using NLog;

namespace CodeExecutor_Framework48
{
    internal class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    throw new ArgumentException("Args length must be equal or more than 1");
                }

                string path = CheckPath(args[0]);

                List<string> refDlls = GetReferencesPath(path);

                _log.Info("Reading file content");

                string fileContent = File.ReadAllText(path);
                string programName = Path.GetFileNameWithoutExtension(path);
                //        string fileContent = @"
                //using System;
                //public class Program
                //{
                //    public void Main()
                //    {
                //        Console.WriteLine(""Hello from dynamically compiled code!"");
                //    }
                //}";
                IDynamicCodeCompiler compiler = new DynamicCodeCompiler(fileContent, programName, refDlls);

                _log.Info("Compiling code");
                var compileResult = compiler.Compile();
                if (!compileResult)
                    throw new Exception("Compilation failed");

                _log.Info("Code compiled successfully");
                
                compiler.Execute();
                _log.Info("Executed");
            }
            catch (Exception e)
            {
                _log.Error(e);
            }

            Console.ReadLine();
        }


        private static List<string> GetReferencesPath(string path)
        {
            string parent = Path.GetDirectoryName(path);
            var dir = Directory.GetDirectories(parent);
            var refDir = dir.FirstOrDefault(w => Path.GetFileName(w).ToLower() == "reference");
            if (refDir == null)
                return new List<string>();

            var files = Directory.GetFiles(refDir);
            var result = files.Where(w => w.EndsWith(".dll")).ToList();
            return result;
        }

        private static string CheckPath(string path)
        {
            return path;
        }
    }
}