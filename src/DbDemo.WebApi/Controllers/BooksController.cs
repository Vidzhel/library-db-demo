using Microsoft.AspNetCore.Mvc;
using DbDemo.Application.Repositories;
using DbDemo.Domain.Entities;
using DbDemo.WebApi.DTOs;
using DbDemo.WebApi.Services;

namespace DbDemo.WebApi.Controllers;

/// <summary>
/// API controller for managing books
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionContext _transactionContext;
    private readonly ILogger<BooksController> _logger;

    public BooksController(
        IBookRepository bookRepository,
        ICategoryRepository categoryRepository,
        ITransactionContext transactionContext,
        ILogger<BooksController> logger)
    {
        _bookRepository = bookRepository;
        _categoryRepository = categoryRepository;
        _transactionContext = transactionContext;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of books with optional filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <param name="categoryId">Filter by category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of books</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedResponse<BookDto>>> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest(ApiResponse<object>.ErrorResponse("Page number must be at least 1"));

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(ApiResponse<object>.ErrorResponse("Page size must be between 1 and 100"));

        var transaction = _transactionContext.Transaction;

        List<Book> books;
        int totalCount;

        if (categoryId.HasValue)
        {
            books = await _bookRepository.GetByCategoryAsync(categoryId.Value, transaction, cancellationToken);
            totalCount = books.Count;

            // Manual pagination for category filtering
            books = books
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            books = await _bookRepository.GetPagedAsync(page, pageSize, includeDeleted: false, transaction, cancellationToken);
            totalCount = await _bookRepository.GetCountAsync(includeDeleted: false, transaction, cancellationToken);
        }

        var bookDtos = new List<BookDto>();
        foreach (var book in books)
        {
            var category = await _categoryRepository.GetByIdAsync(book.CategoryId, transaction, cancellationToken);
            bookDtos.Add(MapToDto(book, category?.Name));
        }

        var response = new PaginatedResponse<BookDto>
        {
            Data = bookDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific book by ID
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Book details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BookDto>>> GetBook(int id, CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var book = await _bookRepository.GetByIdAsync(id, transaction, cancellationToken);
        if (book == null)
            return NotFound(ApiResponse<BookDto>.ErrorResponse($"Book with ID {id} not found"));

        var category = await _categoryRepository.GetByIdAsync(book.CategoryId, transaction, cancellationToken);
        var bookDto = MapToDto(book, category?.Name);

        return Ok(ApiResponse<BookDto>.SuccessResponse(bookDto));
    }

    /// <summary>
    /// Search for books by title
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching books</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<BookDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BookDto>>>> SearchBooks(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<List<BookDto>>.ErrorResponse("Search query is required"));

        var transaction = _transactionContext.Transaction;

        var books = await _bookRepository.SearchByTitleAsync(query, transaction, cancellationToken);
        var bookDtos = new List<BookDto>();

        foreach (var book in books)
        {
            var category = await _categoryRepository.GetByIdAsync(book.CategoryId, transaction, cancellationToken);
            bookDtos.Add(MapToDto(book, category?.Name));
        }

        return Ok(ApiResponse<List<BookDto>>.SuccessResponse(bookDtos, $"Found {bookDtos.Count} book(s)"));
    }

    /// <summary>
    /// Create a new book
    /// </summary>
    /// <param name="request">Book creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created book</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BookDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BookDto>>> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<BookDto>.ErrorResponse("Invalid book data", GetModelStateErrors()));

        var transaction = _transactionContext.Transaction;

        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, transaction, cancellationToken);
        if (category == null)
            return BadRequest(ApiResponse<BookDto>.ErrorResponse($"Category with ID {request.CategoryId} not found"));

        // Check if ISBN already exists
        var existingBook = await _bookRepository.GetByIsbnAsync(request.ISBN, transaction, cancellationToken);
        if (existingBook != null)
            return BadRequest(ApiResponse<BookDto>.ErrorResponse($"Book with ISBN {request.ISBN} already exists"));

        // Create book entity
        var book = new Book(
            isbn: request.ISBN,
            title: request.Title,
            categoryId: request.CategoryId,
            totalCopies: request.TotalCopies
        );

        // Set optional properties using UpdateDetails
        book.UpdateDetails(
            title: request.Title,
            subtitle: request.Subtitle,
            description: request.Description,
            publisher: request.Publisher
        );

        // Update publishing info if any provided
        if (request.PublishedDate.HasValue || request.PageCount.HasValue || !string.IsNullOrWhiteSpace(request.Language))
        {
            book.UpdatePublishingInfo(
                publishedDate: request.PublishedDate,
                pageCount: request.PageCount,
                language: request.Language
            );
        }

        // Update shelf location if provided
        if (!string.IsNullOrWhiteSpace(request.ShelfLocation))
            book.UpdateShelfLocation(request.ShelfLocation);

        // Save to database
        var createdBook = await _bookRepository.CreateAsync(book, transaction, cancellationToken);
        var bookDto = MapToDto(createdBook, category.Name);

        return CreatedAtAction(nameof(GetBook), new { id = createdBook.Id }, ApiResponse<BookDto>.SuccessResponse(bookDto, "Book created successfully"));
    }

    /// <summary>
    /// Update an existing book
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated book</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BookDto>>> UpdateBook(
        int id,
        [FromBody] UpdateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<BookDto>.ErrorResponse("Invalid book data", GetModelStateErrors()));

        var transaction = _transactionContext.Transaction;

        var book = await _bookRepository.GetByIdAsync(id, transaction, cancellationToken);
        if (book == null)
            return NotFound(ApiResponse<BookDto>.ErrorResponse($"Book with ID {id} not found"));

        // Verify category if it's being changed
        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, transaction, cancellationToken);
            if (category == null)
                return BadRequest(ApiResponse<BookDto>.ErrorResponse($"Category with ID {request.CategoryId.Value} not found"));
        }

        // Update book properties
        if (!string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Subtitle) ||
            !string.IsNullOrWhiteSpace(request.Description) || !string.IsNullOrWhiteSpace(request.Publisher))
        {
            book.UpdateDetails(
                title: request.Title ?? book.Title,
                subtitle: request.Subtitle ?? book.Subtitle,
                description: request.Description ?? book.Description,
                publisher: request.Publisher ?? book.Publisher
            );
        }

        if (request.PublishedDate.HasValue || request.PageCount.HasValue || !string.IsNullOrWhiteSpace(request.Language))
        {
            book.UpdatePublishingInfo(
                publishedDate: request.PublishedDate ?? book.PublishedDate,
                pageCount: request.PageCount ?? book.PageCount,
                language: request.Language ?? book.Language
            );
        }

        if (!string.IsNullOrWhiteSpace(request.ShelfLocation))
        {
            book.UpdateShelfLocation(request.ShelfLocation);
        }

        // Update in database
        var success = await _bookRepository.UpdateAsync(book, transaction, cancellationToken);
        if (!success)
            return NotFound(ApiResponse<BookDto>.ErrorResponse($"Failed to update book with ID {id}"));

        var updatedCategory = await _categoryRepository.GetByIdAsync(book.CategoryId, transaction, cancellationToken);
        var bookDto = MapToDto(book, updatedCategory?.Name);

        return Ok(ApiResponse<BookDto>.SuccessResponse(bookDto, "Book updated successfully"));
    }

    /// <summary>
    /// Delete a book (soft delete)
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBook(int id, CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var success = await _bookRepository.DeleteAsync(id, transaction, cancellationToken);
        if (!success)
            return NotFound(ApiResponse<object>.ErrorResponse($"Book with ID {id} not found"));

        return Ok(ApiResponse<object>.SuccessResponse(default, "Book deleted successfully"));
    }

    /// <summary>
    /// Get the category of a specific book
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("{id}/category")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetBookCategory(int id, CancellationToken cancellationToken = default)
    {
        var transaction = _transactionContext.Transaction;

        var book = await _bookRepository.GetByIdAsync(id, transaction, cancellationToken);
        if (book == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse($"Book with ID {id} not found"));

        var category = await _categoryRepository.GetByIdAsync(book.CategoryId, transaction, cancellationToken);
        if (category == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse($"Category not found for book {id}"));

        var categoryDto = MapToDto(category);

        return Ok(ApiResponse<CategoryDto>.SuccessResponse(categoryDto));
    }

    // Helper methods for mapping entities to DTOs
    private static BookDto MapToDto(Book book, string? categoryName = null)
    {
        return new BookDto
        {
            Id = book.Id,
            ISBN = book.ISBN,
            Title = book.Title,
            Subtitle = book.Subtitle,
            Description = book.Description,
            Publisher = book.Publisher,
            PublishedDate = book.PublishedDate,
            PageCount = book.PageCount,
            Language = book.Language,
            CategoryId = book.CategoryId,
            CategoryName = categoryName,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            ShelfLocation = book.ShelfLocation,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private List<string> GetModelStateErrors()
    {
        return ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
