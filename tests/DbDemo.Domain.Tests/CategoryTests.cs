using Xunit;
using DbDemo.Models;
using FluentAssertions;

namespace DbDemo.Domain.Tests;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesCategory()
    {
        // Arrange & Act
        var category = new Category("Science Fiction");

        // Assert
        category.Name.Should().Be("Science Fiction");
        category.Description.Should().BeNull();
        category.ParentCategoryId.Should().BeNull();
        category.IsTopLevel.Should().BeTrue();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        category.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesCategory()
    {
        // Arrange & Act
        var category = new Category("Physics", "Science of matter and energy", parentCategoryId: 1);

        // Assert
        category.Name.Should().Be("Physics");
        category.Description.Should().Be("Science of matter and energy");
        category.ParentCategoryId.Should().Be(1);
        category.IsTopLevel.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Act
        Action act = () => new Category(invalidName!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('A', 101);

        // Act
        Action act = () => new Category(longName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Category name cannot exceed 100 characters*");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        // Act
        var category = new Category("  Science Fiction  ");

        // Assert
        category.Name.Should().Be("Science Fiction");
    }

    [Fact]
    public void UpdateDetails_WithValidData_UpdatesCategory()
    {
        // Arrange
        var category = new Category("Fiction");
        var originalUpdatedAt = category.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        category.UpdateDetails("Non-Fiction", "Factual books");

        // Assert
        category.Name.Should().Be("Non-Fiction");
        category.Description.Should().Be("Factual books");
        category.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDetails_WithInvalidName_ThrowsArgumentException()
    {
        // Arrange
        var category = new Category("Fiction");

        // Act
        Action act = () => category.UpdateDetails("", "Some description");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsTopLevel_WhenParentIdIsNull_ReturnsTrue()
    {
        // Arrange
        var category = new Category("Fiction");

        // Act & Assert
        category.IsTopLevel.Should().BeTrue();
    }

    [Fact]
    public void IsTopLevel_WhenParentIdIsSet_ReturnsFalse()
    {
        // Arrange
        var category = new Category("Science Fiction", parentCategoryId: 1);

        // Act & Assert
        category.IsTopLevel.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var category = new Category("Fiction");

        // Act
        var result = category.ToString();

        // Assert
        result.Should().Contain("Fiction");
        result.Should().Contain("ID:");
    }
}
