using Autodesk.AutoCAD.ApplicationServices;
using autocad_mcp_plugin.Models;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Reflection;

namespace autocad_mcp_plugin.Commands
{
    /// <summary>
    /// Executes dynamic C# code in the AutoCAD context.
    /// Equivalent of send_code_to_revit in the Revit plugin.
    ///
    /// Parameters:
    ///   code (string) — C# code to execute. The code should define a static method:
    ///     public static object Execute(Document doc) { ... }
    ///
    /// Security note: only enable in trusted/development environments.
    /// </summary>
    public class SendCodeToAutoCADCommand : DocumentContextCommandBase
    {
        public override string CommandName => "send_code_to_autocad";
        protected override int TimeoutMs => 120000;

        protected override object ExecuteInDocumentContext(Document doc, JObject parameters)
        {
            string code = parameters?["code"]?.ToString();
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Parameter 'code' is required.");

            // Wrap code in a compilable class
            string fullCode = $@"
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

public class DynamicScript
{{
    public static object Execute(Autodesk.AutoCAD.ApplicationServices.Document doc)
    {{
        {code}
    }}
}}";

            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options  = new CompilerParameters
            {
                GenerateInMemory = true,
                ReferencedAssemblies =
                {
                    "System.dll",
                    typeof(Document).Assembly.Location,     // acmgd.dll
                    typeof(Autodesk.AutoCAD.DatabaseServices.Database).Assembly.Location // acdbmgd.dll
                }
            };

            var results = provider.CompileAssemblyFromSource(options, fullCode);

            if (results.Errors.HasErrors)
            {
                var errors = "";
                foreach (CompilerError err in results.Errors)
                    errors += $"  Line {err.Line}: {err.ErrorText}\n";
                throw new Exception($"Compilation errors:\n{errors}");
            }

            var type   = results.CompiledAssembly.GetType("DynamicScript");
            var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            var result = method.Invoke(null, new object[] { doc });

            return new AIResult<object>
            {
                Success  = true,
                Message  = "Dynamic code executed successfully.",
                Response = result
            };
        }
    }
}
