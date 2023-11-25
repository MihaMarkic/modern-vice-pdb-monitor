[IsDependentOn(typeof(UnitTest))]
[IsDependentOn(typeof(GetProjectVersion))]
public class PublishDependant : PublishCore
{
    public override void Run(BuildContext context)
    {
        PublishRuntime(context, "win-x64", selfContained: false, context.PublishDependantDirectory);
        PublishRuntime(context, "osx-arm64", selfContained: false, context.PublishDependantDirectory);
        PublishRuntime(context, "linux-x64", selfContained: false, context.PublishDependantDirectory);
    }
}
