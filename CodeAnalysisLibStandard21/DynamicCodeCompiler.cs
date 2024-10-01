using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

namespace CodeAnalysisLibStandard21
{
    public class DynamicCodeCompiler : IDynamicCodeCompiler
    {
        private readonly string _codeString;
        private readonly string _programName;
        private readonly List<string> _refDlls = new List<string>();
        private Object _assemblyObject;

        public DynamicCodeCompiler(string code, string programName, List<string> refDlls)
        {
            _codeString = code;
            _programName = programName;
            _refDlls = refDlls;
        }

        public bool Compile()
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateInMemory = true, // Compile in memory
                GenerateExecutable = false // Create a DLL, not an executable
            };

            // Add references
            //parameters.ReferencedAssemblies.Add("System.dll");
            
            string settingValue = ConfigurationManager.AppSettings["BaseReference"];
            if (!string.IsNullOrEmpty(settingValue))
            {
                string[] baseReferences = settingValue.Split(',');
                foreach (var reference in baseReferences)
                {
                    parameters.ReferencedAssemblies.Add(reference.Trim() + ".dll");
                }
            }

            foreach (var dll in _refDlls)
            {
                parameters.ReferencedAssemblies.Add(dll);
            }


            // Compile the code
            CompilerResults compilerResults = codeProvider.CompileAssemblyFromSource(parameters, _codeString);
            if (compilerResults.Errors.HasErrors)
            {
                string errorsText = string.Empty;
                foreach (CompilerError error in compilerResults.Errors)
                {
                    errorsText += "; " + error.ErrorText;
                }

                throw new AggregateException($"Compilation failed. {errorsText}");
            }

            var assembly = compilerResults.CompiledAssembly;
            var dynamicClass = assembly.CreateInstance("Program");

            _assemblyObject = dynamicClass ?? throw new Exception("Class 'Program' not found.");
            return true;
        }

        public void Execute()
        {
            if (_assemblyObject == null)
            {
                throw new InvalidOperationException("Assembly not compiled.");
            }
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), _programName+$"{DateTime.Now:yyyy-MM-dd-HHmmss}.log");
            string output = string.Empty;
            // Capture console output
            using (StreamWriter logWriter = new StreamWriter(logFilePath, true))
            {
                using (StringWriter stringWriter = new StringWriter())
                {
                    Console.SetOut(stringWriter);

                    var method = _assemblyObject.GetType().GetMethod("Main");
                    if (method == null)
                    {
                        throw new InvalidOperationException("Method 'Main' not found.");
                    }
                    AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

                    foreach (var dll in _refDlls)
                    {
                        // Load the DLL into the current context
                        Assembly.LoadFrom(dll);
                    }

                    method.Invoke(_assemblyObject, null);
                    output = stringWriter.ToString();
                    logWriter.Write(output); // Write to log file
                }
            }

            // Reset console output
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            Console.WriteLine(output); // Write to console
        }

        // Method to manually resolve and load assemblies
        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";
            string assemblyPath = _refDlls.FirstOrDefault(dll => Path.GetFileName(dll).Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

            if (assemblyPath != null && File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null; // If assembly is not found, return null
        }
    }
}