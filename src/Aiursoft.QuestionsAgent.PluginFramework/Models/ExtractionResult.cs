using System.Text.Json.Serialization;

namespace Aiursoft.QuestionsAgent.PluginFramework.Models;

public class ExtractionResult
{
    public bool Found { get; set; }
    
    [JsonPropertyName("end_line_index")]
    public int EndLineIndex { get; set; }
    
    public List<QuestionItem> Data { get; set; } = new();
}
