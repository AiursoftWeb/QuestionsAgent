using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Xunit;

namespace Aiursoft.QuestionsAgent.Tests.Services;

public class TextCleanerTests
{
    private readonly TextCleaner _textCleaner;

    public TextCleanerTests()
    {
        _textCleaner = new TextCleaner();
    }

    [Fact]
    public void NormalizeText_RemovesMarkdownImages()
    {
        // Arrange
        var content = "Some text ![image](path/to/image.png) more text";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.DoesNotContain("![image]", string.Join(" ", result));
        Assert.Contains("Some text", string.Join(" ", result));
        Assert.Contains("more text", string.Join(" ", result));
    }

    [Fact]
    public void NormalizeText_AddsLineBreaksBeforeQuestionNumbers()
    {
        // Arrange
        var content = "Question 1. First question 2. Second question";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.Contains("1. First question", result);
        Assert.Contains("2. Second question", result);
    }

    [Fact]
    public void NormalizeText_AddsLineBreaksBeforeOptions()
    {
        // Arrange
        var content = "Question text A. Option A B. Option B C. Option C D. Option D";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.Contains(result, line => line.Contains("A. Option A"));
        Assert.Contains(result, line => line.Contains("B. Option B"));
        Assert.Contains(result, line => line.Contains("C. Option C"));
        Assert.Contains(result, line => line.Contains("D. Option D"));
    }

    [Fact]
    public void NormalizeText_FiltersEmptyLines()
    {
        // Arrange
        var content = "Line 1\n\n\n   \nLine 2";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Line 1", result);
        Assert.Contains("Line 2", result);
    }

    [Fact]
    public void NormalizeText_HandlesEmptyString()
    {
        // Arrange
        var content = "";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void NormalizeText_HandlesWhitespaceOnly()
    {
        // Arrange
        var content = "   \n\n   \t\t   \n   ";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void NormalizeText_TrimsLines()
    {
        // Arrange
        var content = "   Leading and trailing spaces   \n\t\tTabs too\t\t";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.All(result, line =>
        {
            Assert.Equal(line.Trim(), line);
        });
    }

    [Fact]
    public void NormalizeText_ComplexMarkdownWithMultipleImages()
    {
        // Arrange
        var content = @"# Header
![img1](path1.png)
Some text
![img2](path2.jpg)
![img3](path3.gif)
More content";

        // Act
        var result = _textCleaner.NormalizeText(content);

        // Assert
        Assert.DoesNotContain(result, line => line.Contains("!["));
        Assert.Contains("# Header", result);
        Assert.Contains("Some text", result);
        Assert.Contains("More content", result);
    }
}
