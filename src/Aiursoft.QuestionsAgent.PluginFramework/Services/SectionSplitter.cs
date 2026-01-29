using System.Text;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class SectionSplitter
{
    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<SectionSplitter> _logger;

    public SectionSplitter(
        OllamaClient ollamaClient,
        ILogger<SectionSplitter> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public async Task<List<SectionInfo>> AnalyzeSectionsAsync(List<string> lines)
    {
        _logger.LogInformation("Analyzing document structure...");
        
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                sb.AppendLine($"[L{i}] {lines[i]}");
            }
        }

        var documentContent = sb.ToString();

        var prompt = @$"
你是一个文档结构分析师。请阅读下面的【文档内容】（带有行号 [Lx]）。
你的任务是：**将文档划分为不同的题型区域**。

请识别出文档中出现的题型（如：选择题、填空题、名词解释、简答题、连线题、判断题等），并标出它们的【起始行号】和【结束行号】。

【文档内容】：
----------------
{documentContent}
----------------

请返回 JSON 数组，格式如下：
[
    {{ ""type"": ""选择"", ""start_line"": 0, ""end_line"": 40 }},
    {{ ""type"": ""名词解释"", ""start_line"": 41, ""end_line"": 60 }},
    {{ ""type"": ""简答"", ""start_line"": 61, ""end_line"": 100 }}
]

注意：
1. 请确保 start_line 和 end_line 覆盖了该题型的所有题目（包括标题行）。
2. 如果中间夹杂了答案区，尽量把它单独划分为 ""答案"" 类型，或者归入前一个题型。
3. 不要遗漏任何部分。
4. Type 只能是：""选择""、""填空""、""判断""、""名词解释""、""简答""、""连线""、""答案""、""未知""。
";

        try
        {
            var sections = await _ollamaClient.CallOllamaJson<List<SectionInfo>>(prompt);
            
            if (sections != null)
            {
                foreach (var s in sections)
                {
                    if (s.EndLine >= lines.Count) s.EndLine = lines.Count - 1;
                    if (s.StartLine < 0) s.StartLine = 0;
                    _logger.LogInformation("Identified section: [{Type}] L{Start} - L{End}", s.Type, s.StartLine, s.EndLine);
                }
                return sections;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze sections.");
        }

        return new List<SectionInfo> { new SectionInfo { Type = "未知", StartLine = 0, EndLine = lines.Count - 1 } };
    }
}
