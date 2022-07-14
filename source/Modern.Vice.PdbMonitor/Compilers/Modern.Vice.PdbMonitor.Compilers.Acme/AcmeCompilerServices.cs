using Modern.Vice.PdbMonitor.Compilers.Acme.Services.Abstract;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modern.Vice.PdbMonitor.Core.Common;
using Modern.Vice.PdbMonitor.Core.Common.Compiler;
using Modern.Vice.PdbMonitor.Core.Services.Abstract;

namespace Modern.Vice.PdbMonitor.Compilers.Acme;

public class AcmeCompilerServices : ICompilerServices
{
    readonly IAcmePdbParser pdbParser;
    public AcmeCompilerServices(IAcmePdbParser pdbParser)
    {
        this.pdbParser = pdbParser;
    }
    /// <summary>
    /// Report file has name with extension .rep
    /// </summary>
    /// <param name="prgPath"></param>
    /// <returns></returns>
    internal string GetReportFileName(string prgPath) => GetRelatedFileName(prgPath, "report");
    /// <summary>
    /// Labels file has name with extension .lbl
    /// </summary>
    /// <param name="prgPath"></param>
    /// <returns></returns>
    internal string GetLabelsFileName(string prgPath) => GetRelatedFileName(prgPath, "labels");
    internal string GetRelatedFileName(string prgPath, string extension) => $"{Path.GetFileNameWithoutExtension(prgPath)}.{extension}";
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectDirectory">Globals.ProjectDirectory!</param>
    /// <param name="prgPath"></param>
    /// <returns></returns>
    internal DebugFiles GetDebugFilesPath(string projectDirectory, string prgPath)
    {
        string directory = Path.Combine(projectDirectory, Path.GetDirectoryName(prgPath) ?? "");
        return new DebugFiles(
            Path.Combine(directory, GetReportFileName(prgPath)),
            Path.Combine(directory, GetLabelsFileName(prgPath)));
    }
    public async Task<(Pdb? Pdb, string? ErrorMessage)> ParseDebugSymbolsAsync(string projectDirectory, string prgPath, CancellationToken ct)
    {
        var debugFiles = GetDebugFilesPath(projectDirectory, prgPath);
        if (!File.Exists(debugFiles.Report))
        {
            return (null, $"Report file {debugFiles.Report} does not exist");
        }
        if (!File.Exists(debugFiles.Labels))
        {
            return (null, $"Labels file {debugFiles.Labels} does not exist");
        }
        try
        {
            var result = await Task.Run(() => pdbParser.ParseAsync(projectDirectory, debugFiles, ct));
            if (result.Errors.Length > 0)
            {
                string errorMessage = string.Join('\n', result.Errors.Select(e => e.ErrorText));
                return (null, errorMessage);
            }
            else
            {
                return (result.ParsedData, null);
            }
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
