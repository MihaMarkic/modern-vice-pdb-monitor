using Cake.Incubator.Project;

public class GetProjectVersion : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var project = context.ParseProject(context.ProjectFile, configuration: context.BuildConfiguration);
        var versionElement = project.ProjectXml?.Root?.Elements("PropertyGroup").Elements("Version").SingleOrDefault();
        if (versionElement is null)
        {
            throw new Exception("Couldn't find project version in .csproj");
        }
        context.ProjectVersion = versionElement.Value;
        context.Information($"Project version is {context.ProjectVersion}");
    }
}