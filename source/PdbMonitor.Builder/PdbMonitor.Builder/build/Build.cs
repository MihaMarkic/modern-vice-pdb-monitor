using Cake.Common.Tools.DotNet.Build;

[IsDependentOn(typeof(Clean))]
public class Build: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetBuildSettings
        {
            Configuration = context.BuildConfiguration,
            
        };
        context.DotNetBuild(context.ProjectFile, settings);

    }
}
