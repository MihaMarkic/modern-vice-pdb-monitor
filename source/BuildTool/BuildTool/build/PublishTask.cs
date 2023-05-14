using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;

[TaskName("SquirrelPublish")]
//[IsDependentOn(typeof(TestTask))]
public sealed class SquirrelPublishTask : FrostingTask<BuildContext>
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


[TaskName("Publish")]
[IsDependentOn(typeof(SquirrelPublishTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    // Tasks can be asynchronous
    public override void Run(BuildContext context)
    {
    }
}
