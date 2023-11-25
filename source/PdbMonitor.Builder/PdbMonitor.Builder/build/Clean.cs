using Cake.Common.Tools.DotNet.Clean;

public class Clean: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information($"Project: ${context.MakeAbsolute(context.SolutionDirectory)} ");
        var settings = new DotNetCleanSettings
        {
            Configuration = context.BuildConfiguration,
        };
        context.DotNetClean(context.ProjectFile, settings);
    }
}
