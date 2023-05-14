using Cake.Common;
using Cake.Common.IO;
using Cake.Common.IO.Paths;

public class BuildContext : FrostingContext
{
    public const string AppName = "Modern.Vice.PdbMonitor";
    public ProjectType ProjectType { get; }
    public ConvertableDirectoryPath Root { get; }
    public ConvertableDirectoryPath AppSolutionDir { get; }
    public ConvertableFilePath AppSolution { get; }
    public ConvertableDirectoryPath TestDir { get; }
    public ConvertableDirectoryPath TestCompilersDir { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Root = context.Directory(context.Argument("root", "../../.."));
        AppSolutionDir = Root + context.Directory(AppName);
        AppSolution = AppSolutionDir + context.File($"{AppName}.sln");
        TestDir = AppSolutionDir + context.Directory("Test");
        TestCompilersDir = TestDir + context.Directory("Compilers");
        ProjectType = context.Argument("project-type", ProjectType.Squirrel);
    }
}

public enum ProjectType
{
    Squirrel
}