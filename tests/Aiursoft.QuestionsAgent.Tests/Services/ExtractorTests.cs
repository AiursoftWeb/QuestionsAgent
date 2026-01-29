using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class ExtractorTests
{
    private readonly Mock<OllamaClient> _mockOllamaClient;
    private readonly Mock<ILogger<Extractor>> _mockLogger;
    private readonly Extractor _extractor;

    public ExtractorTests()
    {
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockOllamaLogger = new Mock<ILogger<OllamaClient>>();
        var mockOptions = new Mock<IOptions<OllamaOptions>>();
        mockOptions.Setup(x => x.Value).Returns(new OllamaOptions
        {
            Instance = "http://localhost:11434",
            Model = "test-model",
            Token = "test-token"
        });

        _mockOllamaClient = new Mock<OllamaClient>(
            mockHttpClientFactory.Object,
            mockOllamaLogger.Object,
            mockOptions.Object);

        _mockLogger = new Mock<ILogger<Extractor>>();
        _extractor = new Extractor(_mockOllamaClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExtractSectionAsync_SkipsAnswerSection()
    {
        // Arrange
        var lines = new List<string> { "Answer 1", "Answer 2" };
        var section = new SectionInfo { Type = "答案", StartLine = 0, EndLine = 1 };

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
        _mockOllamaClient.Verify(
            x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ExtractSectionAsync_SkipsUnknownSection()
    {
        // Arrange
        var lines = new List<string> { "Unknown content" };
        var section = new SectionInfo { Type = "未知", StartLine = 0, EndLine = 0 };

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_SkipsMatchingSection()
    {
        // Arrange
        var lines = new List<string> { "Match A to B" };
        var section = new SectionInfo { Type = "连线", StartLine = 0, EndLine = 0 };

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_ExtractsChoiceQuestions()
    {
        // Arrange
        var lines = new List<string>
        {
            "1. What is AI?",
            "A. Artificial Intelligence",
            "B. Automated Internet",
            "2. What is ML?"
        };
        var section = new SectionInfo { Type = "选择", StartLine = 0, EndLine = 3 };

        // Mock extraction result for single-item mode
        var extractionResult = new
        {
            found = true,
            end_line_index = 2,
            data = new List<QuestionItem>
            {
                new QuestionItem
                {
                    Question = "What is AI?",
                    Options = new List<string> { "A. Artificial Intelligence", "B. Automated Internet" }
                }
            }
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync(extractionResult);

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal("选择", result[0].Type);
        Assert.Equal("test.md", result[0].OriginalFilename);
    }

    [Fact]
    public async Task ExtractSectionAsync_HandlesExtractionError()
    {
        // Arrange
        var lines = new List<string> { "Question text" };
        var section = new SectionInfo { Type = "简答", StartLine = 0, EndLine = 0 };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Extraction failed"));

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        // Should continue and return empty (cursor increments and continues)
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_HandlesNotFoundResult()
    {
        // Arrange
        var lines = new List<string> { "Not a question" };
        var section = new SectionInfo { Type = "简答", StartLine = 0, EndLine = 0 };

        var extractionResult = new
        {
            found = false,
            end_line_index = 0,
            data = new List<QuestionItem>()
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync(extractionResult);

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_SetsTypeAndFilename()
    {
        // Arrange
        var lines = new List<string> { "1. Simple question" };
        var section = new SectionInfo { Type = "填空", StartLine = 0, EndLine = 0 };

        var extractionResult = new
        {
            found = true,
            end_line_index = 0,
            data = new List<QuestionItem>
            {
                new QuestionItem { Question = "Simple question" }
            }
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync(extractionResult);

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "exam.md");

        // Assert
        Assert.Single(result);
        Assert.Equal("填空", result[0].Type);
        Assert.Equal("exam.md", result[0].OriginalFilename);
    }

    [Fact]
    public async Task ExtractSectionAsync_CorrectsBoundaryViolatingEndIndex()
    {
        // Arrange
        var lines = new List<string> { "Q1", "Q2", "Q3" };
        var section = new SectionInfo { Type = "简答", StartLine = 0, EndLine = 2 };

        var extractionResult = new
        {
            found = true,
            end_line_index = 100, // Exceeds window size
            data = new List<QuestionItem>
            {
                new QuestionItem { Question = "Test" }
            }
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync(extractionResult);

        // Act
        var result = await _extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert - Should not throw and handle boundary correction
        Assert.NotEmpty(result);
    }
}
