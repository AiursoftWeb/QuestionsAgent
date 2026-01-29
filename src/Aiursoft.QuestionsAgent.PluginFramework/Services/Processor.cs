using System.Text.RegularExpressions;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class Processor
{
    private readonly TextCleaner _textCleaner;
    private readonly SectionSplitter _sectionSplitter;
    private readonly Extractor _extractor;
    private readonly Matcher _matcher;
    private readonly ResultSaver _resultSaver;
    private readonly ILogger<Processor> _logger;
    private const int AnswerContextLength = 3000;

    public Processor(
        TextCleaner textCleaner,
        SectionSplitter sectionSplitter,
        Extractor extractor,
        Matcher matcher,
        ResultSaver resultSaver,
        ILogger<Processor> logger)
    {
        _textCleaner = textCleaner;
        _sectionSplitter = sectionSplitter;
        _extractor = extractor;
        _matcher = matcher;
        _resultSaver = resultSaver;
        _logger = logger;
    }

    public async Task RunAsync(string inputFile, string outputDir)
    {
        _logger.LogInformation("Starting ETL pipeline for {InputFile}...", inputFile);

        if (!File.Exists(inputFile))
        {
            _logger.LogError("Input file {InputFile} not found.", inputFile);
            return;
        }

        var rawContent = await File.ReadAllTextAsync(inputFile);
        var papers = SplitToPapers(rawContent);
        _logger.LogInformation("Identified {Count} papers/documents.", papers.Count);

        var finalResults = new List<QuestionItem>();

        foreach (var paper in papers)
        {
            _logger.LogInformation("Processing: {FileName}", paper.FileName);

            var normalizedLines = _textCleaner.NormalizeText(paper.Content);
            if (normalizedLines.Count == 0) continue;

            var sections = await _sectionSplitter.AnalyzeSectionsAsync(normalizedLines);

            var fileQuestions = new List<QuestionItem>();
            foreach (var section in sections)
            {
                var sectionQuestions = await _extractor.ExtractSectionAsync(normalizedLines, section, paper.FileName);
                fileQuestions.AddRange(sectionQuestions);
            }

            _logger.LogInformation("Found {Count} questions in {FileName}", fileQuestions.Count, paper.FileName);
            if (fileQuestions.Count == 0) continue;

            var footerContext = paper.Content.Length > AnswerContextLength 
                ? paper.Content.Substring(paper.Content.Length - AnswerContextLength) 
                : paper.Content;

            await _matcher.FillAnswersAsync(fileQuestions, footerContext);

            finalResults.AddRange(fileQuestions);
            await _resultSaver.SaveQuestionsAsync(fileQuestions, outputDir);
        }

        _logger.LogInformation("ETL process completed. Total questions: {Count}", finalResults.Count);
    }

    private List<PaperContext> SplitToPapers(string raw)
    {
        var list = new List<PaperContext>();
        var parts = Regex.Split(raw, @"(?=# 来源文件：)");
        foreach (var p in parts)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            var match = Regex.Match(p, @"# 来源文件：(.+)");
            var name = match.Success ? match.Groups[1].Value.Trim() : "Unknown";
            list.Add(new PaperContext { FileName = name, Content = p });
        }
        return list;
    }
}
