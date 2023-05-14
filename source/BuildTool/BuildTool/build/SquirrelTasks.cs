using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core.IO;
using System.IO;

[TaskName("DotNetPublishForSquirrel")]
//[IsDependentOn(typeof(TestTask))]
public sealed class DotNetPublishForSquirrelTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.ProjectType == ProjectType.Squirrel;
    public override void Run(BuildContext context)
    {
        var settings = new DotNetPublishSettings
        {
            Framework = "net7.0",
            Configuration = context.AppConfiguration,
            SelfContained = true,
            Runtime = "win-x64",
        };
        context.DotNetPublish(context.AppProject, settings);
    }
}

[TaskName("Squirrel")]
[IsDependentOn(typeof(DotNetPublishForSquirrelTask))]
public sealed class SquirrelPublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.ProjectType == ProjectType.Squirrel;
    public override void Run(BuildContext context)
    {
        if (context.Version is null)
        {
            throw new Exception("Version argument has to be set");
        }
        var publishDir = context.AppProjectDir + context.Directory("bin") + context.Directory(context.AppConfiguration)
            + context.Directory("net7.0") + context.Directory("win-x64") + context.Directory("publish");
        var squirrelPath = context.Tools.Resolve("squirrel.exe");
        var arguments = new ProcessArgumentBuilder()
            .Append("pack")
            .Append($"--packId \"Modern.Vice.Debugger\"")
            .Append($"--packVersion \"{context.Version}\"")
            .Append($"--packAuthors \"Righthand\"")
            .Append($"--packDirectory \"{publishDir.Path.MakeAbsolute(context.Environment).FullPath}\"")
            .Append($"--packTitle  \"Modern VICE debugger\"")
            .Append($"--releaseDir  \"{context.PublishSquirrelDir.Path.MakeAbsolute(context.Environment).FullPath}\"")
            .Append($"--noDelta");
        var settings = new ProcessSettings
        {
            Arguments = arguments
        };
        var result = context.StartProcess(squirrelPath.MakeAbsolute(context.Environment), settings);
        if (result != 0)
        {
            throw new Exception("Failed running build tools");
        }
    }
}
