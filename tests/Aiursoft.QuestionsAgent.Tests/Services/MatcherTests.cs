using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class MatcherTests
{
    private Mock<OllamaClient> CreateMockOllamaClient()
    {
        return new Mock<OllamaClient>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OllamaClient>>(),
            Options.Create(new OllamaOptions()));
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
        var mockClient = CreateMockOllamaClient();
        var callCount = 0;
        
        mockClient.Setup(x => x.CallOllamaJson<AnswerDTO>(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new AnswerDTO { answer = "A", analysis = "Analysis 1" }
                    : new AnswerDTO { answer = "B", analysis = "Analysis 2" };
            });

        var mockLogger = new Mock<ILogger<Matcher>>();
        var matcher = new Matcher(mockClient.Object, mockLogger.Object);

        // Act
        await matcher.FillAnswersAsync(questions, footerText);

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

        var mockClient = CreateMockOllamaClient();
        mockClient.Setup(x => x.CallOllamaJson<AnswerDTO>(It.IsAny<string>()))
            .ReturnsAsync((AnswerDTO?)null);

        var mockLogger = new Mock<ILogger<Matcher>>();
        var matcher = new Matcher(mockClient.Object, mockLogger.Object);

        // Act
        await matcher.FillAnswersAsync(questions, "footer");

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

        var mockClient = CreateMockOllamaClient();
        mockClient.Setup(x => x.CallOllamaJson<AnswerDTO>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI error"));

        var mockLogger = new Mock<ILogger<Matcher>>();
        var matcher = new Matcher(mockClient.Object, mockLogger.Object);

        // Act
        await matcher.FillAnswersAsync(questions, "footer");

        // Assert
        Assert.Equal("Error", questions[0].Answer);
    }

    [Fact]
    public async Task FillAnswersAsync_ProcessesEmptyList()
    {
        // Arrange
        var questions = new List<QuestionItem>();
        var mockClient = CreateMockOllamaClient();
        var mockLogger = new Mock<ILogger<Matcher>>();
        var matcher = new Matcher(mockClient.Object, mockLogger.Object);

        // Act & Assert - Should not throw
        await matcher.FillAnswersAsync(questions, "footer");
        
        mockClient.Verify(
            x => x.CallOllamaJson<AnswerDTO>(It.IsAny<string>()),
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

        var mockClient = CreateMockOllamaClient();
        mockClient.Setup(x => x.CallOllamaJson<AnswerDTO>(It.IsAny<string>()))
            .Callback<string>(prompt => capturedPrompt = prompt)
            .ReturnsAsync(new AnswerDTO { answer = "A", analysis = "" });

        var mockLogger = new Mock<ILogger<Matcher>>();
        var matcher = new Matcher(mockClient.Object, mockLogger.Object);

        // Act
        await matcher.FillAnswersAsync(questions, footerText);

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("What is the capital?", capturedPrompt);
        Assert.Contains("A. Paris", capturedPrompt);
        Assert.Contains("B. London", capturedPrompt);
        Assert.Contains("选择", capturedPrompt);
        Assert.Contains("Answers here", capturedPrompt);
    }
}
