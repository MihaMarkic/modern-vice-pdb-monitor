public class BuildContext : FrostingContext
{
    public const string AppName = "Modern.Vice.PdbMonitor";
    public readonly string AppConfiguration = "Release";
    public ProjectType ProjectType { get; }
    public ConvertableDirectoryPath Root { get; }
    public ConvertableDirectoryPath AppSolutionDir { get; }
    public ConvertableFilePath AppSolution { get; }
    public ConvertableDirectoryPath AppProjectDir { get; }
    public ConvertableFilePath AppProject { get; }
    public ConvertableDirectoryPath TestDir { get; }
    public ConvertableDirectoryPath TestCompilersDir { get; }
    public ConvertableDirectoryPath PublishDir { get; }
    public ConvertableDirectoryPath PublishSquirrelDir { get; }
    public string? Version { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Root = context.Directory(context.Argument("root", "../../../.."));
        AppSolutionDir = Root + context.Directory("source") + context.Directory(AppName);
        AppSolution = AppSolutionDir + context.File($"{AppName}.sln");
        AppProjectDir = AppSolutionDir + context.Directory(AppName);
        AppProject = AppProjectDir + context.File($"{AppName}.csproj");
        TestDir = AppSolutionDir + context.Directory("Test");
        TestCompilersDir = TestDir + context.Directory("Compilers");
        ProjectType = context.Argument("project-type", ProjectType.Squirrel);
        Version = context.Argument<string?>("app-version", null);
        PublishDir = Root + context.Directory("publish");
        PublishSquirrelDir = PublishDir + context.Directory("squirrel");

        context.Log.Information($"Root: {Root.Path.MakeAbsolute(context.Environment)}");
        context.Log.Information($"Version: {Version}");
    }
}

public enum ProjectType
{
    Squirrel
}