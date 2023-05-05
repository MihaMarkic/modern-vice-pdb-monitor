using Compiler.Oscar64.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Compiler.Oscar64.Services.Implementation;
public class Oscar64DbjParser
{
    readonly ILogger<Oscar64DbjParser> logger;
    public Oscar64DbjParser(ILogger<Oscar64DbjParser> logger)
    {
        this.logger = logger;
    }

    public async Task<DebugFile?> LoadFileAsync(string path, CancellationToken ct = default)
    {
        DebugFile? result = null;
        if (File.Exists(path))
        {
            string content;
            try
            {
                content = File.ReadAllText(path);
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
}
