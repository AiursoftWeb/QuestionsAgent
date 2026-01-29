using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class Matcher
{
    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<Matcher> _logger;

    public Matcher(
        OllamaClient ollamaClient,
        ILogger<Matcher> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public async Task FillAnswersAsync(List<QuestionItem> questions, string footerText)
    {
        _logger.LogInformation("Starting to match answers for {Count} questions...", questions.Count);

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var indexStr = $"[{i + 1}/{questions.Count}]";
            var shortQuestion = q.Question.Length > 50 ? q.Question.Substring(0, 50) + "..." : q.Question;

            _logger.LogInformation("{Index} Matching: {Question}", indexStr, shortQuestion);

            var prompt = $@"
你是一个评分助手。
请根据下面的【参考资料】（通常是试卷末尾的答案部分），找到【目标题目】的正确答案。

注意：
1. 优先根据题干内容在参考资料中查找匹配项。
2. 如果参考资料里只有 ""1. A, 2. B"" 这种格式，请尝试根据题目顺序或题号推断（但这通常不准，请优先寻找语义匹配）。
3. 如果找不到答案，请诚实地返回 ""未知""。

【目标题目】：
题目：{q.Question}
选项：{string.Join(" ", q.Options)}
题型：{q.Type}

【参考资料】：
----------------
{footerText}
----------------

请返回单一的 JSON 对象：
{{ ""answer"": ""选项字母或内容"", ""analysis"": ""简短解析（如果在资料里有的话）"" }}
";

            try
            {
                var result = await _ollamaClient.CallOllamaJson<AnswerDTO>(prompt);
                if (result != null)
                {
                    q.Answer = result.answer;
                    q.Analysis = result.analysis;
                    _logger.LogInformation("Result: {Answer}", q.Answer);
                }
                else
                {
                    q.Answer = "未知";
                    _logger.LogWarning("Null response for question.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching answer.");
                q.Answer = "Error";
            }
        }
    }
    
    private class AnswerDTO 
    { 
        public string answer { get; set; } = string.Empty;
        public string analysis { get; set; } = string.Empty;
    }
}
