using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class MatcherTests
{
    private readonly Mock<OllamaClient> _mockOllamaClient;
    private readonly Mock<ILogger<Matcher>> _mockLogger;
    private readonly Matcher _matcher;

    public MatcherTests()
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

        _mockLogger = new Mock<ILogger<Matcher>>();
        _matcher = new Matcher(_mockOllamaClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task FillAnswersAsync_FillsAnswersSuccessfully()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Question = "Test question 1", Type = "选择" },
            new QuestionItem { Question = "Test question 2", Type = "选择" }
        };

        var footerText = "参考答案: 1. A 2. B";

        // Setup mock to return different answers
        var callCount = 0;
        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? (object)new { answer = "A", analysis = "Analysis 1" }
                    : (object)new { answer = "B", analysis = "Analysis 2" };
            });

        // Act
        await _matcher.FillAnswersAsync(questions, footerText);

        // Assert
        Assert.Equal("A", questions[0].Answer);
        Assert.Equal("Analysis 1", questions[0].Analysis);
        Assert.Equal("B", questions[1].Answer);
        Assert.Equal("Analysis 2", questions[1].Analysis);
    }

    [Fact]
    public async Task FillAnswersAsync_HandlesNullResponse()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Question = "Test question", Type = "选择" }
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ReturnsAsync((object?)null);

        // Act
        await _matcher.FillAnswersAsync(questions, "footer");

        // Assert
        Assert.Equal("未知", questions[0].Answer);
    }

    [Fact]
    public async Task FillAnswersAsync_HandlesException()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Question = "Test question", Type = "选择" }
        };

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI error"));

        // Act
        await _matcher.FillAnswersAsync(questions, "footer");

        // Assert
        Assert.Equal("Error", questions[0].Answer);
    }

    [Fact]
    public async Task FillAnswersAsync_ProcessesEmptyList()
    {
        // Arrange
        var questions = new List<QuestionItem>();

        // Act & Assert - Should not throw
        await _matcher.FillAnswersAsync(questions, "footer");
        
        _mockOllamaClient.Verify(
            x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task FillAnswersAsync_IncludesQuestionDetailsInPrompt()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem
            {
                Question = "What is the capital?",
                Options = new List<string> { "A. Paris", "B. London" },
                Type = "选择"
            }
        };

        var footerText = "Answers here";
        string? capturedPrompt = null;

        _mockOllamaClient
            .Setup(x => x.CallOllamaJson<It.IsAnyType>(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync(new { answer = "A", analysis = "" });

        // Act
        await _matcher.FillAnswersAsync(questions, footerText);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("What is the capital?", capturedPrompt);
        Assert.Contains("A. Paris", capturedPrompt);
        Assert.Contains("B. London", capturedPrompt);
        Assert.Contains("选择", capturedPrompt);
        Assert.Contains("Answers here", capturedPrompt);
    }
}
