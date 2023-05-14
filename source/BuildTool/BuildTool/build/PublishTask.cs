[TaskName("SquirrelPublish")]
[IsDependentOn(typeof(TestTask))]
public sealed class SquirrelPublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.ProjectType == ProjectType.Squirrel;
    public override void Run(BuildContext context)
    {
        context.Log.Information("Squirrel");
    }
}


[TaskName("Publish")]
[IsDependentOn(typeof(SquirrelPublishTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    // Tasks can be asynchronous
    public override void Run(BuildContext context)
    {
        context.Log.Information("Publish");
    }
}
