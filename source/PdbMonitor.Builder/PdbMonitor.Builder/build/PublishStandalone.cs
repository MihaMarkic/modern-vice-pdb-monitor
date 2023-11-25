[IsDependentOn(typeof(UnitTest))]
[IsDependentOn(typeof(GetProjectVersion))]
public class PublishStandalone: PublishCore
{
    public override void Run(BuildContext context)
    {
        PublishRuntime(context, "win-x64", selfContained: true, context.PublishStandaloneDirectory);
        PublishRuntime(context, "osx-arm64", selfContained: true, context.PublishStandaloneDirectory);
        PublishRuntime(context, "linux-x64", selfContained: true, context.PublishStandaloneDirectory);
    }
}
