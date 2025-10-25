using DbDemo.Models;
using FluentAssertions;
using Xunit;

namespace DbDemo.Domain.Tests;

public class BookAuthorTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesBookAuthor()
    {
        // Act
        var bookAuthor = new BookAuthor(bookId: 1, authorId: 2, authorOrder: 0);

        // Assert
        bookAuthor.BookId.Should().Be(1);
        bookAuthor.AuthorId.Should().Be(2);
        bookAuthor.AuthorOrder.Should().Be(0);
        bookAuthor.Role.Should().BeNull();
        bookAuthor.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithRole_CreatesBookAuthorWithRole()
    {
        // Act
        var bookAuthor = new BookAuthor(1, 2, 0, "Editor");

        // Assert
        bookAuthor.Role.Should().Be("Editor");
    }

    [Fact]
    public void UpdateOrder_WithValidOrder_UpdatesAuthorOrder()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0);

        // Act
        bookAuthor.UpdateOrder(1);

        // Assert
        bookAuthor.AuthorOrder.Should().Be(1);
    }

    [Fact]
    public void UpdateOrder_WithNegativeOrder_ThrowsArgumentException()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0);

        // Act
        Action act = () => bookAuthor.UpdateOrder(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Order cannot be negative*");
    }

    [Fact]
    public void UpdateRole_UpdatesRole()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0);

        // Act
        bookAuthor.UpdateRole("Translator");

        // Assert
        bookAuthor.Role.Should().Be("Translator");
    }

    [Fact]
    public void UpdateRole_WithNull_SetsRoleToNull()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0, "Editor");

        // Act
        bookAuthor.UpdateRole(null);

        // Assert
        bookAuthor.Role.Should().BeNull();
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0, "Primary Author");

        // Act
        var result = bookAuthor.ToString();

        // Assert
        result.Should().Contain("Book 1");
        result.Should().Contain("Author 2");
        result.Should().Contain("Primary Author");
        result.Should().Contain("Order: 0");
    }

    [Fact]
    public void ToString_WithoutRole_UsesDefaultAuthorRole()
    {
        // Arrange
        var bookAuthor = new BookAuthor(1, 2, 0);

        // Act
        var result = bookAuthor.ToString();

        // Assert
        result.Should().Contain("Author"); // Default role
    }
}
