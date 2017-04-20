﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "../../../../Dlls/Perf.Utilities/Roslyn.Test.Performance.Utilities.dll"
#r "../../../../Dlls/VisualStudioIntegrationTestUtilities/Microsoft.VisualStudio.IntegrationTest.Utilities.dll"
#r "../../../../UnitTests/VisualStudioIntegrationTests/Roslyn.VisualStudio.IntegrationTests.dll"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Performance.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

class CSharpTyping2 : PerfTest
{
    private const string _rootSuffix = "RoslynPerf";
    private static readonly string _nugetPackagesPath = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
        Path.Combine(System.Environment.GetEnvironmentVariable("UserProfile"), ".nuget", "packages");
    private static readonly string _installerPath = Path.Combine(_nugetPackagesPath, "roslyntools.microsoft.vsixexpinstaller", "0.4.0-beta", "tools", "vsixexpinstaller.exe");

    public override string Name => "CSharp Typing";

    public override string MeasuredProc => null;

    public override bool ProvidesScenarios => false;

    public override string[] GetScenarios() => null;
    public override void Setup()
    {
        DownloadProject("RoslynSolutions", 1, new ConsoleAndFileLogger());
    }

    private void InstallVsixes()
    {
        var vsix1 = Path.Combine(MyBinaries(), "Vsix", "VisualStudioSetup", "Roslyn.VisualStudio.Setup.vsix");
        var vsix2 = Path.Combine(MyBinaries(), "Vsix", "VisualStudioSetup.Next", "Roslyn.VisualStudio.Setup.Next.vsix");

        var rootSuffixArg = $"/rootsuffix:{_rootSuffix}";

        // First uninstall any vsixes in the hive
        ShellOutVital(_installerPath, $"{rootSuffixArg}, /uninstallAll");

        // Then install the RoslynDeployment.vsix we just built
        ShellOutVital(_installerPath, $"{rootSuffixArg} {vsix1}");
        ShellOutVital(_installerPath, $"{rootSuffixArg} {vsix2}");
    }

    public override void Test()
    {
        var factory = new VisualStudioInstanceFactory();
        var instance = factory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);

        var VisualStudio = instance.Instance;

        var roslynSolutions = Path.Combine(TempDirectory, "RoslynSolutions");
        
        VisualStudio.SolutionExplorer.OpenSolution(Path.Combine(roslynSolutions, "Roslyn-CSharp.sln"));
        VisualStudio.ExecuteCommand("File.OpenFile", Path.Combine(roslynSolutions, @"Source\Compilers\CSharp\Source\Parser\LanguageParser.cs"));
        ETWActions.StartETWListener(VisualStudio);
        ETWActions.WaitForSolutionCrawler(VisualStudio);
        ETWActions.ForceGC(VisualStudio);
        VisualStudio.ExecuteCommand("Edit.GoTo", "9524");
        ETWActions.WaitForIdleCPU();
        var typingResults = Path.Combine(TempDirectory, "typingResults");
        Directory.CreateDirectory(typingResults);
        using (DelayTracker.Start(typingResults, Path.Combine("csharp", "typing", "TypingDelayAnalyzer.exe"), "typing"))
        {
            VisualStudio.Editor.PlayBackTyping(Path.Combine("csharp", "typing", "CSharpGoldilocksInput-MultipliedDelay.txt"));
        }
        //System.Windows.Forms.MessageBox.Show("Done waiting!");
        ETWActions.StopETWListener(VisualStudio);
        VisualStudio.Close();
    }
}

TestThisPlease(
    new CSharpTyping2());
