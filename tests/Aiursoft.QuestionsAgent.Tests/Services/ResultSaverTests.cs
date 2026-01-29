using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class ResultSaverTests
{

    private readonly ResultSaver _resultSaver;
    private readonly string _testOutputDir;

    public ResultSaverTests()
    {
        var mockLogger = new Mock<ILogger<ResultSaver>>();
        _resultSaver = new ResultSaver(mockLogger.Object);
        _testOutputDir = Path.Combine(Path.GetTempPath(), "QuestionsAgentTests", Guid.NewGuid().ToString());
    }

    [Fact]
    public async Task SaveQuestionsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "Test question" }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(questions, _testOutputDir);

        // Assert
        Assert.True(Directory.Exists(_testOutputDir));

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }

    [Fact]
    public async Task SaveQuestionsAsync_CreatesNewJsonFile()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "Test question", Answer = "A" }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(questions, _testOutputDir);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "选择.json");
        Assert.True(File.Exists(filePath));

        var json = await File.ReadAllTextAsync(filePath);
        var saved = JsonSerializer.Deserialize<List<QuestionItem>>(json);
        Assert.NotNull(saved);
        Assert.Single(saved);
        Assert.Equal("Test question", saved[0].Question);

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }

    [Fact]
    public async Task SaveQuestionsAsync_AppendsToExistingFile()
    {
        // Arrange
        var firstBatch = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "Question 1" }
        };
        var secondBatch = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "Question 2" }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(firstBatch, _testOutputDir);
        await _resultSaver.SaveQuestionsAsync(secondBatch, _testOutputDir);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "选择.json");
        var json = await File.ReadAllTextAsync(filePath);
        var saved = JsonSerializer.Deserialize<List<QuestionItem>>(json);
        Assert.NotNull(saved);
        Assert.Equal(2, saved.Count);
        Assert.Equal("Question 1", saved[0].Question);
        Assert.Equal("Question 2", saved[1].Question);

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }

    [Fact]
    public async Task SaveQuestionsAsync_GroupsByType()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "Choice question" },
            new QuestionItem { Type = "填空", Question = "Fill blank question" },
            new QuestionItem { Type = "选择", Question = "Another choice question" }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(questions, _testOutputDir);

        // Assert
        var choiceFile = Path.Combine(_testOutputDir, "选择.json");
        var fillFile = Path.Combine(_testOutputDir, "填空.json");
        
        Assert.True(File.Exists(choiceFile));
        Assert.True(File.Exists(fillFile));

        var choiceJson = await File.ReadAllTextAsync(choiceFile);
        var choiceQuestions = JsonSerializer.Deserialize<List<QuestionItem>>(choiceJson);
        Assert.NotNull(choiceQuestions);
        Assert.Equal(2, choiceQuestions.Count);

        var fillJson = await File.ReadAllTextAsync(fillFile);
        var fillQuestions = JsonSerializer.Deserialize<List<QuestionItem>>(fillJson);
        Assert.NotNull(fillQuestions);
        Assert.Single(fillQuestions);

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }

    [Fact]
    public async Task SaveQuestionsAsync_HandlesCorruptedExistingFile()
    {
        // Arrange
        Directory.CreateDirectory(_testOutputDir);
        var filePath = Path.Combine(_testOutputDir, "选择.json");
        await File.WriteAllTextAsync(filePath, "{ corrupted json content");

        var questions = new List<QuestionItem>
        {
            new QuestionItem { Type = "选择", Question = "New question" }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(questions, _testOutputDir);

        // Assert - Should create new file with only new question
        var json = await File.ReadAllTextAsync(filePath);
        var saved = JsonSerializer.Deserialize<List<QuestionItem>>(json);
        Assert.NotNull(saved);
        Assert.Single(saved);
        Assert.Equal("New question", saved[0].Question);

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }

    [Fact]
    public async Task SaveQuestionsAsync_SavesAllQuestionProperties()
    {
        // Arrange
        var questions = new List<QuestionItem>
        {
            new QuestionItem
            {
                Type = "选择",
                Question = "What is the answer?",
                Options = new List<string> { "A. Option 1", "B. Option 2" },
                Answer = "A",
                Analysis = "Analysis text",
                OriginalFilename = "test.md"
            }
        };

        // Act
        await _resultSaver.SaveQuestionsAsync(questions, _testOutputDir);

        // Assert
        var filePath = Path.Combine(_testOutputDir, "选择.json");
        var json = await File.ReadAllTextAsync(filePath);
        var saved = JsonSerializer.Deserialize<List<QuestionItem>>(json);
        Assert.NotNull(saved);
        var item = saved[0];
        Assert.Equal("选择", item.Type);
        Assert.Equal("What is the answer?", item.Question);
        Assert.Equal(2, item.Options.Count);
        Assert.Equal("A", item.Answer);
        Assert.Equal("Analysis text", item.Analysis);
        Assert.Equal("test.md", item.OriginalFilename);

        // Cleanup
        Directory.Delete(_testOutputDir, true);
    }
}
