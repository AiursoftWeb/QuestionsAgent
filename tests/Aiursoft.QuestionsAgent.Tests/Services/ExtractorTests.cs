using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class ExtractorTests
{
    private Mock<OllamaClient> CreateMockOllamaClient<T>(T? returnValue) where T : class
    {
        var mockClient = new Mock<OllamaClient>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OllamaClient>>(),
            Options.Create(new OllamaOptions()));

        mockClient.Setup(x => x.CallOllamaJson<T>(It.IsAny<string>()))
            .ReturnsAsync(returnValue);

        return mockClient;
    }

    private Mock<OllamaClient> CreateMockOllamaClientWithException<T>(Exception ex) where T : class
    {
        var mockClient = new Mock<OllamaClient>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OllamaClient>>(),
            Options.Create(new OllamaOptions()));

        mockClient.Setup(x => x.CallOllamaJson<T>(It.IsAny<string>()))
            .ThrowsAsync(ex);

        return mockClient;
    }


    [Fact]
    public async Task ExtractSectionAsync_SkipsAnswerSection()
    {
        // Arrange
        var lines = new List<string> { "Answer 1", "Answer 2" };
        var section = new SectionInfo { Type = "答案", StartLine = 0, EndLine = 1 };

        var mockClient = CreateMockOllamaClient<ExtractionResult>(null);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
        mockClient.Verify(
            x => x.CallOllamaJson<ExtractionResult>(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ExtractSectionAsync_SkipsUnknownSection()
    {
        // Arrange
        var lines = new List<string> { "Unknown content" };
        var section = new SectionInfo { Type = "未知", StartLine = 0, EndLine = 0 };

        var mockClient = CreateMockOllamaClient<ExtractionResult>(null);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_SkipsMatchingSection()
    {
        // Arrange
        var lines = new List<string> { "Match A to B" };
        var section = new SectionInfo { Type = "连线", StartLine = 0, EndLine = 0 };

        var mockClient = CreateMockOllamaClient<ExtractionResult>(null);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

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
        var extractionResult = new ExtractionResult
        {
            Found = true,
            EndLineIndex = 2,
            Data = new List<QuestionItem>
            {
                new QuestionItem
                {
                    Question = "What is AI?",
                    Options = new List<string> { "A. Artificial Intelligence", "B. Automated Internet" }
                }
            }
        };

        var mockClient = CreateMockOllamaClient(extractionResult);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

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

        var mockClient = CreateMockOllamaClientWithException<ExtractionResult>(new Exception("Extraction failed"));
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

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

        var extractionResult = new ExtractionResult
        {
            Found = false,
            EndLineIndex = 0,
            Data = new List<QuestionItem>()
        };

        var mockClient = CreateMockOllamaClient(extractionResult);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractSectionAsync_SetsTypeAndFilename()
    {
        // Arrange
        var lines = new List<string> { "1. Simple question" };
        var section = new SectionInfo { Type = "填空", StartLine = 0, EndLine = 0 };

        var extractionResult = new ExtractionResult
        {
            Found = true,
            EndLineIndex = 0,
            Data = new List<QuestionItem>
            {
                new QuestionItem { Question = "Simple question" }
            }
        };

        var mockClient = CreateMockOllamaClient(extractionResult);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "exam.md");

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

        var extractionResult = new ExtractionResult
        {
            Found = true,
            EndLineIndex = 100, // Exceeds window size
            Data = new List<QuestionItem>
            {
                new QuestionItem { Question = "Test" }
            }
        };

        var mockClient = CreateMockOllamaClient(extractionResult);
        var mockLogger = new Mock<ILogger<Extractor>>();
        var extractor = new Extractor(mockClient.Object, mockLogger.Object);

        // Act
        var result = await extractor.ExtractSectionAsync(lines, section, "test.md");

        // Assert - Should not throw and handle boundary correction
        Assert.NotEmpty(result);
    }
}
