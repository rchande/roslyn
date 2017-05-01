// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpETW : AbstractIntegrationTest
    {

        public CSharpETW(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        void ETW()
        {
            var text = @"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace F1TestNamespace
{
    #region TaoRegion
    abstract class ShapesClass { }

    class Program$$
    {
        public static void Main()
        {
        }

        public IEnumerable<int> Linq1()
        {
            int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
            int i = numbers.First();
            int j = Array.IndexOf(numbers, 1);

            var lowNums1 =
                from n in numbers
                orderby n ascending
                where n < 5
                select n;

            var numberGroups =
              from n in numbers
              let m = 1
              join p in numbers on i equals p
              group n by n % 5 into g
              select new { Remainder = g.Key, Numbers = g };

            foreach (int element in numbers) yield return i;
        }

    }
    #endregion TaoRegion
}";
            ETWActions.StartETWListener(VisualStudio);
            //SetUpEditor(text);
            ETWActions.WaitForSolutionCrawler(VisualStudio);

            VisualStudio.ExecuteCommand("Tools.ForceGC");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        void ETWTyping()
        {
            VisualStudio.SolutionExplorer.OpenSolution(@"C:\rs\RoslynSolutions\Roslyn-CSharp.sln");
            ETWActions.ForceGC(VisualStudio);
            //VisualStudio.SolutionExplorer.OpenFile(new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(@"CSharpCompiler.mod"), "LanguageParser.cs");
            //VisualStudio.Editor.NavigateToSendKeys("LanguageParser.cs");
            VisualStudio.ExecuteCommand("File.OpenFile", @"C:\rs\RoslynSolutions\Source\Compilers\CSharp\Source\Parser\LanguageParser.cs");
            //ETWActions.StartETWListener(VisualStudio);
            //ETWActions.WaitForSolutionCrawler(VisualStudio);
            //ETWActions.ForceGC(VisualStudio);
            //ETWActions.WaitForIdleCPU();
            VisualStudio.ExecuteCommand("Edit.GoTo", "9524");
            //ETWActions.WaitForSolutionCrawler(VisualStudio);

            //using (DelayTracker.Start(@"C:\typingResults\", @"C:\roslyn-internal\open\binaries\release\dlls\PerformanceTestUtilities\TypingDelayAnalyzer.exe", "typing"))
            //{
            VisualStudio.Editor.PlayBackTyping(@"C:\roslyn-internal\Closed\Test\PerformanceTests\Perf\tests\CSharp\TypingInputs\CSharpGoldilocksInput-MultipliedDelay.txt");
            //}

            //ETWActions.StopETWListener();

            System.Threading.Thread.Sleep(30000);
        }


            private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }
    }
}