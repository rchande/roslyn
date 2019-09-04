using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Xunit;

namespace NetCoreScriptingTests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var loader = new InteractiveAssemblyLoader();
            var options = ScriptOptions.Default;
            options = options.AddReferences(MetadataReference.CreateFromFile(@"E:\testdll\bin\Debug\netstandard2.0\testdll.dll"));
            var script = CSharpScript.Create("", options, assemblyLoader: loader);
            var state = await script.RunAsync().ConfigureAwait(false);
            //var state2 = await state.ContinueWithAsync("#r \"C:\\Users\\rchande\\.nuget\\packages\\microsoft.ml.dataview\\1.3.1\\lib\\netstandard2.0\\Microsoft.ML.DataView.dll \"").ConfigureAwait(false);
            //var state3 = await state2.ContinueWithAsync("//using Microsoft.ML;").ConfigureAwait(false);
            var state4 = await state.ContinueWithAsync("void Do( testdll.Class1 dv) {}").ConfigureAwait(false);
            var state5 = await state4.ContinueWithAsync(" testdll.Class1 dv = null; Do(dv);").ConfigureAwait(false);

        }
    }
}
