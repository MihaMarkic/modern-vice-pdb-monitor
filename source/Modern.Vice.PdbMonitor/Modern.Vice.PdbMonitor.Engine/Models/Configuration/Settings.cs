using System.Collections.ObjectModel;
using Modern.Vice.PdbMonitor.Core;
using Newtonsoft.Json;

namespace Modern.Vice.PdbMonitor.Engine.Models.Configuration;

public class Settings : NotifiableObject
{
    public const int MaxRecentProjects = 10;
    public string? VicePath { get; set; }
    public bool ResetOnStop { get; set; }
    public ObservableCollection<string> RecentProjects { get; } = new ObservableCollection<string>();
    [JsonIgnore]
    public string? LastAccessedDirectory => RecentProjects.Count > 0 ? RecentProjects[0] : null;
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
