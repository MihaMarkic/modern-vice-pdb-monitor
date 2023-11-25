public class BuildContext : FrostingContext
{
    public ConvertableDirectoryPath SolutionDirectory { get; set; }
    public ConvertableFilePath SolutionFile { get; }
    public ConvertableDirectoryPath ProjectDirectory { get; }
    public ConvertableFilePath ProjectFile { get; }
    public string BuildConfiguration => "Release";

    public BuildContext(ICakeContext context)
        : base(context)
    {
        SolutionDirectory = context.Directory(context.Argument("solution-dir", "../../../Modern.Vice.PdbMonitor"));
        SolutionFile = SolutionDirectory + context.File("Modern.Vice.PdbMonitor");
        ProjectDirectory = SolutionDirectory + context.Directory("Modern.Vice.PdbMonitor");
        ProjectFile = ProjectDirectory + context.File("Modern.Vice.PdbMonitor.csproj");
    }
}
