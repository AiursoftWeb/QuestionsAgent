using System.Text.Encodings.Web;
using System.Text.Json;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class ResultSaver
{
    private readonly ILogger<ResultSaver> _logger;

    public ResultSaver(ILogger<ResultSaver> logger)
    {
        _logger = logger;
    }

    public async Task SaveQuestionsAsync(List<QuestionItem> items, string outputDir)
    {
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var groups = items.GroupBy(x => x.Type);
        foreach (var g in groups)
        {
            var fileName = $"{g.Key}.json".Replace("/", "_");
            var path = Path.Combine(outputDir, fileName);
            
            var existing = new List<QuestionItem>();

            if (File.Exists(path))
            {
                try 
                {
                    var oldJson = await File.ReadAllTextAsync(path);
                    if (!string.IsNullOrWhiteSpace(oldJson))
                    {
                        existing = JsonSerializer.Deserialize<List<QuestionItem>>(oldJson) ?? new List<QuestionItem>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read existing file {Path}. Creating new.", path);
                }
            }

            existing.AddRange(g);

            var json = JsonSerializer.Serialize(existing, new JsonSerializerOptions 
            { 
                WriteIndented = true, 
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
            });
            await File.WriteAllTextAsync(path, json);
            _logger.LogInformation("Saved {Count} items to {Path}", g.Count(), path);
        }
    }
}
