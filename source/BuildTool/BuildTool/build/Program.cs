public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .InstallTool(new Uri("nuget:?package=Clowd.Squirrel&version=2.9.42"))
            .Run(args);
    }
}
