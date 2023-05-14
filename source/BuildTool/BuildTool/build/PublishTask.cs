using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;

[TaskName("Publish")]
[IsDependentOn(typeof(SquirrelPublishTask))]
public sealed class PublishTask : FrostingTask<BuildContext>
{
    // Tasks can be asynchronous
    public override void Run(BuildContext context)
    {
    }
}
