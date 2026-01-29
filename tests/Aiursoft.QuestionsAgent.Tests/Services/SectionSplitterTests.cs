using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class SectionSplitterTests
{
    [Fact]
    public async Task AnalyzeSectionsAsync_ReturnsSuccessfulSections()
    {
        // Arrange
        var lines = new List<string>
        {
            "一、选择题",
            "1. Question 1",
            "2. Question 2",
            "二、填空题",
            "1. Fill blank 1"
        };

        var expectedSections = new List<SectionInfo>
        {
            new SectionInfo { Type = "选择", StartLine = 0, EndLine = 2 },
            new SectionInfo { Type = "填空", StartLine = 3, EndLine = 4 }
        };

        var mockClient = CreateMockOllamaClient(expectedSections);
        var mockLogger = new Mock<ILogger<SectionSplitter>>();
        var sectionSplitter = new SectionSplitter(mockClient.Object, mockLogger.Object);

        // Act
        var result = await sectionSplitter.AnalyzeSectionsAsync(lines);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("选择", result[0].Type);
        Assert.Equal("填空", result[1].Type);
    }

    [Fact]
    public async Task AnalyzeSectionsAsync_CorrectsBoundaryExceedingEndLine()
    {
        // Arrange
        var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

        var sectionsWithInvalidEndLine = new List<SectionInfo>
        {
            new SectionInfo { Type = "选择", StartLine = 0, EndLine = 100 } // Exceeds line count
        };

        var mockClient = CreateMockOllamaClient(sectionsWithInvalidEndLine);
        var mockLogger = new Mock<ILogger<SectionSplitter>>();
        var sectionSplitter = new SectionSplitter(mockClient.Object, mockLogger.Object);

        // Act
        var result = await sectionSplitter.AnalyzeSectionsAsync(lines);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(2, result[0].EndLine); // Should be corrected to lines.Count - 1
    }

    [Fact]
    public async Task AnalyzeSectionsAsync_CorrectsBoundaryNegativeStartLine()
    {
        // Arrange
        var lines = new List<string> { "Line 1", "Line 2" };

        var sectionsWithInvalidStartLine = new List<SectionInfo>
        {
            new SectionInfo { Type = "选择", StartLine = -5, EndLine = 1 } // Negative start
        };

        var mockClient = CreateMockOllamaClient(sectionsWithInvalidStartLine);
        var mockLogger = new Mock<ILogger<SectionSplitter>>();
        var sectionSplitter = new SectionSplitter(mockClient.Object, mockLogger.Object);

        // Act
        var result = await sectionSplitter.AnalyzeSectionsAsync(lines);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(0, result[0].StartLine); // Should be corrected to 0
    }

    [Fact]
    public async Task AnalyzeSectionsAsync_ReturnsFallbackOnNull()
    {
        // Arrange
        var lines = new List<string> { "Line 1", "Line 2" };

        var mockClient = CreateMockOllamaClient<List<SectionInfo>>(null);
        var mockLogger = new Mock<ILogger<SectionSplitter>>();
        var sectionSplitter = new SectionSplitter(mockClient.Object, mockLogger.Object);

        // Act
        var result = await sectionSplitter.AnalyzeSectionsAsync(lines);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("未知", result[0].Type);
        Assert.Equal(0, result[0].StartLine);
        Assert.Equal(1, result[0].EndLine);
    }

    [Fact]
    public async Task AnalyzeSectionsAsync_ReturnsFallbackOnException()
    {
        // Arrange
        var lines = new List<string> { "Line 1", "Line 2", "Line 3" };

        var mockClient = new Mock<OllamaClient>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<OllamaClient>>(),
            Options.Create(new OllamaOptions()));

        mockClient.Setup(x => x.CallOllamaJson<List<SectionInfo>>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("AI service error"));

        var mockLogger = new Mock<ILogger<SectionSplitter>>();
        var sectionSplitter = new SectionSplitter(mockClient.Object, mockLogger.Object);

        // Act
        var result = await sectionSplitter.AnalyzeSectionsAsync(lines);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("未知", result[0].Type);
        Assert.Equal(0, result[0].StartLine);
        Assert.Equal(2, result[0].EndLine);
    }

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
}
