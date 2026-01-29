using System.Text;
using System.Text.Json.Serialization;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class Extractor
{
    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<Extractor> _logger;
    private const int ContextWindowSize = 40;

    public Extractor(
        OllamaClient ollamaClient,
        ILogger<Extractor> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public async Task<List<QuestionItem>> ExtractSectionAsync(List<string> allLines, SectionInfo section, string sourceFile)
    {
        var sectionLines = allLines.Skip(section.StartLine).Take(section.EndLine - section.StartLine + 1).ToList();

        _logger.LogInformation("Processing section: {Type} (L{Start}-L{End}, {Count} lines)",
            section.Type, section.StartLine, section.EndLine, sectionLines.Count);

        if (section.Type == "答案" || section.Type == "未知") return new List<QuestionItem>();
        if (section.Type == "连线")
        {
            _logger.LogInformation("Skipping matching questions section.");
            return new List<QuestionItem>();
        }

        var singleItemMode = section.Type == "选择";
        return await RunExtractionLoop(sectionLines, section.Type, sourceFile, singleItemMode);
    }

    private async Task<List<QuestionItem>> RunExtractionLoop(List<string> lines, string type, string sourceFile, bool singleItemMode)
    {
        var results = new List<QuestionItem>();
        var cursor = 0;
        var totalLines = lines.Count;

        while (cursor < totalLines)
        {
            var windowLines = lines.Skip(cursor).Take(ContextWindowSize).ToList();
            if (windowLines.All(string.IsNullOrWhiteSpace)) break;

            var sb = new StringBuilder();
            var hasContent = false;
            for (var i = 0; i < windowLines.Count; i++)
            {
                sb.AppendLine($"[L{i}] {windowLines[i]}");
                if (!string.IsNullOrWhiteSpace(windowLines[i])) hasContent = true;
            }
            if (!hasContent) { cursor++; continue; }

            var contextText = sb.ToString();
            var instruction = singleItemMode
                ? $"从 [L0] 开始，提取**第一道**完整的【{type}】。"
                : $"从 [L0] 开始，提取紧接着的所有【{type}】（可能有多道，例如 1. xx 2. xx）。";

            var prompt = @$"
你是一个专业的格式化专家。当前任务：**提取{type}**。

【文本片段】：
----------------
{contextText}
----------------

{instruction}

请返回 JSON 格式：
{{
    ""found"": true,
    ""data"": [
        {{ 
            ""type"": ""{type}"", 
            ""question"": ""题目内容"", 
            ""options"": [] 
        }}
    ],
    ""end_line_index"": 0  // 这些题目在片段中的最后一行是 [L?] ？
}}

注意：
1. 如果 [L0] 不是题目（是空行或无关文字），返回 found: false。
2. 即使是名词解释，如果这一行写着 ""1. 悲剧 2. 喜剧""，请在 data 数组里返回两个对象！
3. end_line_index 必须准确，指示处理到了哪里。
";

            try
            {
                var result = await _ollamaClient.CallOllamaJson<ExtractionResult>(prompt);
                if (result != null && result.Found && result.Data.Count > 0)
                {
                    var endIndex = result.EndLineIndex;
                    if (endIndex >= windowLines.Count) endIndex = windowLines.Count - 1;
                    if (endIndex < 0) endIndex = 0;
                    var jump = endIndex + 1;

                    foreach (var q in result.Data)
                    {
                        q.Type = type;
                        q.OriginalFilename = sourceFile;
                        results.Add(q);

                        var safeQ = q.Question.Length > 20 ? q.Question.Substring(0, 20) + "..." : q.Question;
                        _logger.LogInformation("Found: {Question}", safeQ);
                    }

                    cursor += jump;
                }
                else
                {
                    cursor++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extraction error at line {Cursor}", cursor);
                cursor++;
            }
        }
        return results;
    }


}
