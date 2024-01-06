using System.Collections.Immutable;
using Compiler.Oscar64.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Compiler.Oscar64.Services.Implementation;
public class Oscar64DbjParser
{
    readonly ILogger<Oscar64DbjParser> logger;
    readonly AsmParser asmParser;
    public Oscar64DbjParser(ILogger<Oscar64DbjParser> logger, AsmParser asmParser)
    {
        this.logger = logger;
        this.asmParser = asmParser;
    }

    public async Task<DebugFile?> LoadFileAsync(string path, CancellationToken ct = default)
    {
        DebugFile? result = null;
        if (File.Exists(path))
        {
            string content;
            try
            {
                content = await File.ReadAllTextAsync(path, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load Oscar64 debug file {path}");
                return null;
            }
            result = await LoadContentAsync(content, ct);
        }
        return result;

    }
    public Task<DebugFile?> LoadContentAsync(string content, CancellationToken ct = default)
    {
        try
        {
            var result = JsonConvert.DeserializeObject<DebugFile>(content);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to parser Oscar64 debug content");
            throw;
        }
    }
    public Task<ImmutableArray<AssemblyFunction>?> LoadAssemblyFileAsync(
        string path, CancellationToken ct = default)
    {
        if (File.Exists(path))
        {
            try
            {
                var lines = File.ReadLines(path).ToImmutableArray();
                ImmutableArray<AssemblyFunction>? result = asmParser.Parse(lines);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load Oscar64 assembly program file {path}");
            }
        }
        return Task.FromResult<ImmutableArray<AssemblyFunction>?>(null);
    }
}
