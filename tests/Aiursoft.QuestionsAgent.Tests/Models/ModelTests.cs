using Aiursoft.QuestionsAgent.PluginFramework.Models;
using System.Text.Json;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Models;

public class QuestionItemTests
{
    [Fact]
    public void QuestionItem_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var item = new QuestionItem();

        // Assert
        Assert.Equal("选择", item.Type);
        Assert.Equal(string.Empty, item.Question);
        Assert.NotNull(item.Options);
        Assert.Empty(item.Options);
        Assert.Equal("未知", item.Answer);
        Assert.Equal(string.Empty, item.Analysis);
        Assert.Equal(string.Empty, item.OriginalFilename);
    }

    [Fact]
    public void QuestionItem_CanSetAllProperties()
    {
        // Arrange
        var item = new QuestionItem
        {
            Type = "填空",
            Question = "What is AI?",
            Options = new List<string> { "A. Option 1", "B. Option 2" },
            Answer = "A",
            Analysis = "Analysis text",
            OriginalFilename = "test.md"
        };

        // Assert
        Assert.Equal("填空", item.Type);
        Assert.Equal("What is AI?", item.Question);
        Assert.Equal(2, item.Options.Count);
        Assert.Equal("A", item.Answer);
        Assert.Equal("Analysis text", item.Analysis);
        Assert.Equal("test.md", item.OriginalFilename);
    }

    [Fact]
    public void QuestionItem_SerializesToJson()
    {
        // Arrange
        var item = new QuestionItem
        {
            Type = "选择",
            Question = "Test question",
            Options = new List<string> { "A. Option A" },
            Answer = "A",
            Analysis = "Test analysis",
            OriginalFilename = "test.md"
        };

        // Act
        var json = JsonSerializer.Serialize(item);
        var deserialized = JsonSerializer.Deserialize<QuestionItem>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(item.Type, deserialized.Type);
        Assert.Equal(item.Question, deserialized.Question);
        Assert.Equal(item.Options.Count, deserialized.Options.Count);
        Assert.Equal(item.Answer, deserialized.Answer);
        Assert.Equal(item.Analysis, deserialized.Analysis);
        Assert.Equal(item.OriginalFilename, deserialized.OriginalFilename);
    }

    [Fact]
    public void QuestionItem_DeserializesWithMissingFields()
    {
        // Arrange
        var json = "{\"Question\":\"Test\"}";

        // Act
        var item = JsonSerializer.Deserialize<QuestionItem>(json);

        // Assert
        Assert.NotNull(item);
        Assert.Equal("Test", item.Question);
        Assert.Equal("选择", item.Type); // Default value
        Assert.Equal("未知", item.Answer); // Default value
    }
}

public class PaperContextTests
{
    [Fact]
    public void PaperContext_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var context = new PaperContext();

        // Assert
        Assert.Equal(string.Empty, context.FileName);
        Assert.Equal(string.Empty, context.Content);
    }

    [Fact]
    public void PaperContext_CanSetProperties()
    {
        // Arrange & Act
        var context = new PaperContext
        {
            FileName = "exam.md",
            Content = "Question content"
        };

        // Assert
        Assert.Equal("exam.md", context.FileName);
        Assert.Equal("Question content", context.Content);
    }
}

public class SectionInfoTests
{
    [Fact]
    public void SectionInfo_HasCorrectDefaultValues()
    {
        // Arrange & Act
        var section = new SectionInfo();

        // Assert
        Assert.Equal(string.Empty, section.Type);
        Assert.Equal(0, section.StartLine);
        Assert.Equal(0, section.EndLine);
    }

    [Fact]
    public void SectionInfo_CanSetProperties()
    {
        // Arrange & Act
        var section = new SectionInfo
        {
            Type = "选择",
            StartLine = 10,
            EndLine = 20
        };

        // Assert
        Assert.Equal("选择", section.Type);
        Assert.Equal(10, section.StartLine);
        Assert.Equal(20, section.EndLine);
    }

    [Fact]
    public void SectionInfo_SerializesWithJsonPropertyNames()
    {
        // Arrange
        var section = new SectionInfo
        {
            Type = "填空",
            StartLine = 5,
            EndLine = 15
        };

        // Act
        var json = JsonSerializer.Serialize(section);

        // Assert
        Assert.Contains("\"start_line\"", json);
        Assert.Contains("\"end_line\"", json);
        Assert.Contains("\"Type\"", json);
    }

    [Fact]
    public void SectionInfo_DeserializesWithJsonPropertyNames()
    {
        // Arrange
        var json = "{\"type\":\"简答\",\"start_line\":1,\"end_line\":10}";

        // Act
        var section = JsonSerializer.Deserialize<SectionInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(section);
        Assert.Equal("简答", section.Type);
        Assert.Equal(1, section.StartLine);
        Assert.Equal(10, section.EndLine);
    }
}
