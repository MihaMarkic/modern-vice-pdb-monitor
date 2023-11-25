using Cake.Common.Tools.DotNet.Publish;

public abstract class PublishCore : FrostingTask<BuildContext>
{
    protected void PublishRuntime(BuildContext context, string runtime, bool selfContained, 
        ConvertableDirectoryPath outputDirectory)
    {
        Directory.CreateDirectory(context.TempDirectory);
        Directory.CreateDirectory(outputDirectory);
        Directory.Delete(context.TempDirectory, true);
        var settings = new DotNetPublishSettings
        {
            Configuration = context.BuildConfiguration,
            SelfContained = selfContained,
            Runtime = runtime,
            OutputDirectory = context.TempDirectory,
        };
        context.DotNetPublish(context.ProjectFile, settings);
        string versionNumber = context.ProjectVersion?.Replace(".", "_") ?? throw new Exception("No version");
        string runtimeText = runtime.Replace("-", "_");
        var singleFile = outputDirectory + context.File($"modern_pdbdebugger_{runtimeText}_{versionNumber}.zip");
        context.Zip(context.TempDirectory, singleFile);
    }
}