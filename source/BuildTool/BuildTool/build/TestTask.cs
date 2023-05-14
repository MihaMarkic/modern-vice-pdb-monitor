using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;

[TaskName("Test")]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Log.Information(context.Root.Path.MakeAbsolute(context.Environment));
        var testProjects = ImmutableArray<string>.Empty
            .Add("Modern.Vice.PdbMonitor.Core.Test")
            .Add("Modern.Vice.PdbMonitor.Test")
            .Add("Modern.Vice.PdbMonitor.Engine.Test");
        var settings = new DotNetTestSettings
        {
            Configuration = "Release",
        };
        foreach (var p in testProjects)
        {
            var csproj = context.TestDir + context.Directory(p) + context.File($"{p}.csproj");
            context.DotNetTest(csproj.Path.MakeAbsolute(context.Environment).FullPath, settings);
        }
        var compilersRestProjects = ImmutableArray<string>.Empty
            .Add("CC65.DebugDataParser.Test")
            .Add("Compiler.Oscar64.Test")
            .Add("Modern.Vice.PdbMonitor.Compilers.Acme.Test");
        foreach (var p in compilersRestProjects)
        {
            var csproj = context.TestCompilersDir + context.Directory(p) + context.File($"{p}.csproj");
            context.DotNetTest(csproj.Path.MakeAbsolute(context.Environment).FullPath, settings);
        }
    }
}
