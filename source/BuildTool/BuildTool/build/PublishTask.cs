[TaskName("Publish")]
[IsDependentOn(typeof(TestTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    // Tasks can be asynchronous
    public override void Run(BuildContext context)
    {
        context.Log.Information("World");
    }
}
