using Cake.Common.Tools.DotNet.Test;

[IsDependentOn(typeof(Build))]
public class UnitTest : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetTestSettings
        {
            Configuration = context.BuildConfiguration,
            NoRestore = true,
        };
        var path = context.MakeAbsolute(context.SolutionDirectory + context.File("**/*.Test.csproj")).FullPath;
        var projects = context.GetFiles(path);
        if (!projects.Any())
        {
            throw new Exception($"Couldn't find any test project using glob {path}");
        }
        foreach (var project in projects)
        {
            context.DotNetTest(project.FullPath, settings);
        }
    }
}
