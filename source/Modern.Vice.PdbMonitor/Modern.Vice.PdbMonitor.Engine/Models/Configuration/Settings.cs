using System.Text.Json.Serialization;
using Modern.Vice.PdbMonitor.Core;

namespace Modern.Vice.PdbMonitor.Engine.Models.Configuration;

public class Settings : NotifiableObject
{
    public const int MaxRecentProjects = 10;
    /// <summary>
    /// User selected path to VICE files.
    /// </summary>
    public string? VicePath { get; set; }
    
    /// <summary>
    /// Signals that VICE files are contaiend in ./bin sub directory.
    /// True by default because of legacy.
    /// </summary>
    public bool ViceFilesInBinDirectory { get; set; } = true;
    public bool ResetOnStop { get; set; }
    public bool IsAutoUpdateEnabled { get; set; }
    public ObservableCollection<string> RecentProjects { get; set; } = new ObservableCollection<string>();
    [JsonIgnore]
    public string? LastAccessedDirectory => RecentProjects.Count > 0 ? RecentProjects[0] : null;
    /// <summary>
    /// Depending on VICE type, exe can be in either root directory or bin sub directory.
    /// This property returns the correct path.
    /// </summary>
    public string? RealVicePath
    {
        get
        {
            if (VicePath is not null)
            {
                return ViceFilesInBinDirectory ? Path.Combine(VicePath, "bin") : VicePath;
            }
            return null;
        }
    }
    public string BinaryMonitorArgument => "-binarymonitor";
    public void AddRecentProject(string path)
    {
        if (!RecentProjects.Contains(path))
        {
            RecentProjects.Insert(0, path);
            if (RecentProjects.Count > 10)
            {
                RecentProjects.RemoveAt(RecentProjects.Count - 1);
            }
        }
        else
        {
            int index = RecentProjects.IndexOf(path);
            if (index > 0)
            {
                string temp = RecentProjects[0];
                RecentProjects[0] = path;
                RecentProjects[index] = temp;
            }
        }
    }
}
