using System.Text.RegularExpressions;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class TextCleaner
{
    public List<string> NormalizeText(string content)
    {
        // 1. Remove Markdown images
        var text = Regex.Replace(content, @"!\[.*?\]\(.*?\)", "");
        
        // 2. Ensure line breaks before question numbers and options
        text = Regex.Replace(text, @"(\s)(\d+\.)", "\n$2");
        text = Regex.Replace(text, @"([ABCD]\.)", "\n$1");

        // 3. Split by lines and filter empty ones
        return text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }
}
