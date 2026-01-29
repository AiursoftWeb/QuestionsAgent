namespace Aiursoft.QuestionsAgent.PluginFramework.Models;

public class QuestionItem
{
    public string Type { get; set; } = "选择"; // 选择, 填空, 判断, 简答, 名词解释
    public string Question { get; set; } = string.Empty;
    // ReSharper disable once CollectionNeverUpdated.Local
    public List<string> Options { get; set; } = new();
    public string Answer { get; set; } = "未知";
    public string Analysis { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
}

public class PaperContext
{
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class SectionInfo
{
    public string Type { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("start_line")]
    public int StartLine { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("end_line")]
    public int EndLine { get; set; }
}
