using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class ProcessorTests
{
    private readonly Mock<TextCleaner> _mockTextCleaner;
    private readonly Mock<SectionSplitter> _mockSectionSplitter;
    private readonly Mock<Extractor> _mockExtractor;
    private readonly Mock<Matcher> _mockMatcher;
    private readonly Mock<ResultSaver> _mockResultSaver;
    private readonly Mock<ILogger<Processor>> _mockLogger;
    private readonly Processor _processor;
    private readonly string _testInputFile;
    private readonly string _testOutputDir;

    public ProcessorTests()
    {
        _mockTextCleaner = new Mock<TextCleaner>();
        
        // Setup mocks for dependencies of mocked services
        var mockOllamaClient = CreateMockOllamaClient();
        var mockSectionLogger = new Mock<ILogger<SectionSplitter>>();
        var mockExtractorLogger = new Mock<ILogger<Extractor>>();
        var mockMatcherLogger = new Mock<ILogger<Matcher>>();
        var mockSaverLogger = new Mock<ILogger<ResultSaver>>();

        _mockSectionSplitter = new Mock<SectionSplitter>(mockOllamaClient, mockSectionLogger.Object);
        _mockExtractor = new Mock<Extractor>(mockOllamaClient, mockExtractorLogger.Object);
        _mockMatcher = new Mock<Matcher>(mockOllamaClient, mockMatcherLogger.Object);
        _mockResultSaver = new Mock<ResultSaver>(mockSaverLogger.Object);
        _mockLogger = new Mock<ILogger<Processor>>();

        _processor = new Processor(
            _mockTextCleaner.Object,
            _mockSectionSplitter.Object,
            _mockExtractor.Object,
            _mockMatcher.Object,
            _mockResultSaver.Object,
            _mockLogger.Object);

        _testInputFile = Path.Combine(Path.GetTempPath(), $"test_input_{Guid.NewGuid()}.md");
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"test_output_{Guid.NewGuid()}");
    }

    private OllamaClient CreateMockOllamaClient()
    {
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<OllamaClient>>();
        var mockOptions = new Mock<IOptions<OllamaOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new OllamaOptions());
        return new OllamaClient(mockHttpClientFactory.Object, mockLogger.Object, mockOptions.Object);
    }

    [Fact]
    public async Task RunAsync_HandlesNonExistentFile()
    {
        // Arrange
        var nonExistentFile = "/path/to/nonexistent/file.md";

        // Act
        await _processor.RunAsync(nonExistentFile, _testOutputDir);

        // Assert - Should log error and return without throwing
        _mockTextCleaner.Verify(
            x => x.NormalizeText(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_ProcessesSinglePaper()
    {
        // Arrange
        var content = "# 来源文件：test.md\n1. Question 1\nA. Option A";
        await File.WriteAllTextAsync(_testInputFile, content);

        var normalizedLines = new List<string> { "1. Question 1", "A. Option A" };
        var sections = new List<SectionInfo>
        {
            new SectionInfo { Type = "选择", StartLine = 0, EndLine = 1 }
        };
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Question = "Question 1", Type = "选择" }
        };

        _mockTextCleaner
            .Setup(x => x.NormalizeText(It.IsAny<string>()))
            .Returns(normalizedLines);

        _mockSectionSplitter
            .Setup(x => x.AnalyzeSectionsAsync(normalizedLines))
            .ReturnsAsync(sections);

        _mockExtractor
            .Setup(x => x.ExtractSectionAsync(normalizedLines, It.IsAny<SectionInfo>(), It.IsAny<string>()))
            .ReturnsAsync(questions);

        _mockMatcher
            .Setup(x => x.FillAnswersAsync(questions, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockResultSaver
            .Setup(x => x.SaveQuestionsAsync(questions, _testOutputDir))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.RunAsync(_testInputFile, _testOutputDir);

        // Assert
        _mockTextCleaner.Verify(x => x.NormalizeText(It.IsAny<string>()), Times.Once);
        _mockSectionSplitter.Verify(x => x.AnalyzeSectionsAsync(normalizedLines), Times.Once);
        _mockExtractor.Verify(
            x => x.ExtractSectionAsync(normalizedLines, It.IsAny<SectionInfo>(), "test.md"),
            Times.Once);
        _mockMatcher.Verify(x => x.FillAnswersAsync(questions, It.IsAny<string>()), Times.Once);
        _mockResultSaver.Verify(x => x.SaveQuestionsAsync(questions, _testOutputDir), Times.Once);

        // Cleanup
        File.Delete(_testInputFile);
    }

    [Fact]
    public async Task RunAsync_ProcessesMultiplePapers()
    {
        // Arrange
        var content = @"# 来源文件：paper1.md
Question from paper 1
# 来源文件：paper2.md
Question from paper 2";
        await File.WriteAllTextAsync(_testInputFile, content);

        var normalizedLines = new List<string> { "Question" };
        var sections = new List<SectionInfo>
        {
            new SectionInfo { Type = "简答", StartLine = 0, EndLine = 0 }
        };
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Question = "Test", Type = "简答" }
        };

        _mockTextCleaner
            .Setup(x => x.NormalizeText(It.IsAny<string>()))
            .Returns(normalizedLines);

        _mockSectionSplitter
            .Setup(x => x.AnalyzeSectionsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(sections);

        _mockExtractor
            .Setup(x => x.ExtractSectionAsync(It.IsAny<List<string>>(), It.IsAny<SectionInfo>(), It.IsAny<string>()))
            .ReturnsAsync(questions);

        _mockMatcher
            .Setup(x => x.FillAnswersAsync(It.IsAny<List<QuestionItem>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockResultSaver
            .Setup(x => x.SaveQuestionsAsync(It.IsAny<List<QuestionItem>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.RunAsync(_testInputFile, _testOutputDir);

        // Assert - Should process both papers
        _mockTextCleaner.Verify(x => x.NormalizeText(It.IsAny<string>()), Times.Exactly(2));
        _mockResultSaver.Verify(
            x => x.SaveQuestionsAsync(It.IsAny<List<QuestionItem>>(), _testOutputDir),
            Times.Exactly(2));

        // Cleanup
        File.Delete(_testInputFile);
    }

    [Fact]
    public async Task RunAsync_SkipsEmptyNormalizedContent()
    {
        // Arrange
        var content = "# 来源文件：empty.md\n\n\n   ";
        await File.WriteAllTextAsync(_testInputFile, content);

        _mockTextCleaner
            .Setup(x => x.NormalizeText(It.IsAny<string>()))
            .Returns(new List<string>()); // Empty after normalization

        // Act
        await _processor.RunAsync(_testInputFile, _testOutputDir);

        // Assert - Should skip section analysis for empty content
        _mockSectionSplitter.Verify(
            x => x.AnalyzeSectionsAsync(It.IsAny<List<string>>()),
            Times.Never);

        // Cleanup
        File.Delete(_testInputFile);
    }

    [Fact]
    public async Task RunAsync_SkipsWhenNoQuestionsFound()
    {
        // Arrange
        var content = "# 来源文件：noquestions.md\nSome content";
        await File.WriteAllTextAsync(_testInputFile, content);

        var normalizedLines = new List<string> { "Some content" };
        var sections = new List<SectionInfo>
        {
            new SectionInfo { Type = "简答", StartLine = 0, EndLine = 0 }
        };

        _mockTextCleaner
            .Setup(x => x.NormalizeText(It.IsAny<string>()))
            .Returns(normalizedLines);

        _mockSectionSplitter
            .Setup(x => x.AnalyzeSectionsAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(sections);

        _mockExtractor
            .Setup(x => x.ExtractSectionAsync(It.IsAny<List<string>>(), It.IsAny<SectionInfo>(), It.IsAny<string>()))
            .ReturnsAsync(new List<QuestionItem>()); // No questions

        // Act
        await _processor.RunAsync(_testInputFile, _testOutputDir);

        // Assert - Should not call matcher or saver when no questions found
        _mockMatcher.Verify(
            x => x.FillAnswersAsync(It.IsAny<List<QuestionItem>>(), It.IsAny<string>()),
            Times.Never);
        _mockResultSaver.Verify(
            x => x.SaveQuestionsAsync(It.IsAny<List<QuestionItem>>(), It.IsAny<string>()),
            Times.Never);

        // Cleanup
        File.Delete(_testInputFile);
    }
}
