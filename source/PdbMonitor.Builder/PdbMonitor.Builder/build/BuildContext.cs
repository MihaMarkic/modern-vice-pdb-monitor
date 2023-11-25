public class BuildContext : FrostingContext
{
    public ConvertableDirectoryPath RootDirectory { get; set; }
    public ConvertableDirectoryPath TempDirectory { get; set; }
    public ConvertableDirectoryPath SolutionDirectory { get; set; }
    public ConvertableFilePath SolutionFile { get; }
    public ConvertableDirectoryPath ProjectDirectory { get; }
    public ConvertableFilePath ProjectFile { get; }
    public ConvertableDirectoryPath PublishDirectory { get; }
    public ConvertableDirectoryPath PublishStandaloneDirectory { get; }
    public ConvertableDirectoryPath PublishDependantDirectory { get; }
    public string? ProjectVersion { get; set; }
    public string BuildConfiguration => "Release";

    public BuildContext(ICakeContext context)
        : base(context)
    {
        RootDirectory = context.Directory(context.Argument("root-dir", "../../../.."));
        TempDirectory = RootDirectory + context.Directory("temp");
        SolutionDirectory = RootDirectory + context.Directory("source/Modern.Vice.PdbMonitor");
        SolutionFile = SolutionDirectory + context.File("Modern.Vice.PdbMonitor");
        ProjectDirectory = SolutionDirectory + context.Directory("Modern.Vice.PdbMonitor");
        ProjectFile = ProjectDirectory + context.File("Modern.Vice.PdbMonitor.csproj");
        PublishDirectory = RootDirectory + context.Directory("publish");
        PublishStandaloneDirectory = PublishDirectory + context.Directory("standalone");
        PublishDependantDirectory = PublishDirectory + context.Directory("dependant");
    }
}
