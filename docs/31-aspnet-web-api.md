# Lab 31: Building a Web API with ASP.NET Core

## Table of Contents
1. [Introduction to Web APIs](#introduction-to-web-apis)
2. [What is ASP.NET Core?](#what-is-aspnet-core)
3. [Understanding MVC Architecture](#understanding-mvc-architecture)
4. [Project Structure](#project-structure)
5. [Running the Web API](#running-the-web-api)
6. [Testing with Swagger UI](#testing-with-swagger-ui)
7. [Testing with curl and Postman](#testing-with-curl-and-postman)
8. [Understanding HTTP Status Codes](#understanding-http-status-codes)
9. [Key Concepts](#key-concepts)
10. [Next Steps and Research Topics](#next-steps-and-research-topics)

---

## Introduction to Web APIs

### What is a Web API?

A **Web API** (Application Programming Interface) is a software interface that allows different applications to communicate with each other over the internet using HTTP/HTTPS protocols.

**Key Characteristics:**
- **Platform Independent**: Can be consumed by any client (web browser, mobile app, desktop app, IoT device)
- **Language Agnostic**: Clients can be written in any programming language
- **HTTP-based**: Uses standard HTTP methods (GET, POST, PUT, DELETE)
- **Data Format**: Typically exchanges data in JSON or XML format

### Web API vs Console Application

| Aspect | Console Application | Web API |
|--------|-------------------|---------|
| **User Interface** | Terminal/Command Line | HTTP endpoints (no UI) |
| **Interaction** | Direct user input | HTTP requests from clients |
| **Accessibility** | Local machine only | Accessible over network |
| **Scalability** | Single user | Multiple concurrent users |
| **Use Case** | Scripts, batch jobs, demos | Backend for web/mobile apps |

**When to use Web APIs:**
- Building backends for web or mobile applications
- Enabling third-party integrations
- Creating microservices
- Providing data access to multiple clients

**When to use Console Apps:**
- One-time data migrations
- Batch processing jobs
- Development demos and prototypes
- Administrative scripts

### REST (Representational State Transfer)

REST is an architectural style for designing networked applications. RESTful APIs follow these principles:

1. **Resources**: Everything is a resource (Book, Category, User) identified by a URL
   - `/api/books` - collection of books
   - `/api/books/5` - specific book with ID 5

2. **HTTP Methods** map to CRUD operations:
   - `GET` - Read/Retrieve data
   - `POST` - Create new data
   - `PUT` - Update existing data
   - `DELETE` - Remove data

3. **Stateless**: Each request contains all information needed (no session state on server)

4. **JSON Format**: Data is typically exchanged in JSON (JavaScript Object Notation)

**Example JSON response:**
```json
{
  "id": 5,
  "isbn": "9780134685991",
  "title": "Effective Java",
  "author": "Joshua Bloch",
  "availableCopies": 3
}
```

---

## What is ASP.NET Core?

**ASP.NET Core** is a free, open-source, cross-platform framework for building modern web applications and APIs, developed by Microsoft.

### Key Features

1. **Cross-Platform**
   - Runs on Windows, macOS, and Linux
   - Can be developed on any OS

2. **High Performance**
   - One of the fastest web frameworks
   - Optimized for cloud deployments

3. **Dependency Injection Built-in**
   - Native support for dependency injection
   - Promotes loose coupling and testability

4. **Unified Framework**
   - Build Web APIs, MVC apps, Razor Pages, SignalR apps
   - Single framework for all web workloads

5. **Modern Development**
   - Support for async/await
   - Built-in configuration system
   - Comprehensive middleware pipeline

### ASP.NET Core vs ASP.NET Framework

| Feature | ASP.NET Core | ASP.NET Framework |
|---------|-------------|-------------------|
| Platform | Cross-platform | Windows only |
| Performance | High | Moderate |
| Deployment | Self-contained or framework-dependent | Framework-dependent |
| Open Source | Yes | Partially |
| Future | Active development | Maintenance mode |

**For new projects, always choose ASP.NET Core.**

---

## Understanding MVC Architecture

### The MVC Pattern

**MVC** stands for **Model-View-Controller**, a software design pattern that separates an application into three interconnected components.

```
┌─────────┐      ┌────────────┐      ┌───────┐
│  User   │─────>│ Controller │─────>│ Model │
│ Request │      │  (Logic)   │      │(Data) │
└─────────┘      └────────────┘      └───────┘
                        │
                        v
                  ┌──────────┐
                  │   View   │
                  │ (Output) │
                  └──────────┘
```

### MVC Components

#### 1. Model
- Represents the **data** and **business logic**
- Contains domain entities (Book, Category)
- Validation rules and business rules
- Independent of the UI

**Example in our project:**
```csharp
public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; }
    public string Title { get; set; }
    // ... business logic methods
    public void BorrowCopy() { /* ... */ }
}
```

#### 2. View
- Handles the **presentation** and **user interface**
- Displays data from the model
- Sends user commands to controller

**In Web APIs**: There is **no View** component! Web APIs return data (JSON), not HTML.

#### 3. Controller
- Handles **user requests**
- Contains **application logic**
- Interacts with Model and returns data
- Decides what data to return

**Example in our project:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(int id)
    {
        // 1. Receive HTTP request
        // 2. Call repository (Model layer)
        // 3. Return JSON response
    }
}
```

### Web API = MC (No V)

In Web API projects:
- **Model (M)**: Domain entities, DTOs, business logic
- **Controller (C)**: API controllers that handle HTTP requests
- **No View (V)**: Returns JSON data instead of HTML

The "View" is handled by the **client** (web browser, mobile app, etc.) that consumes the API.

### MVC vs Clean Architecture

Our project uses **Clean Architecture** (also called Onion Architecture), which is more sophisticated than simple MVC:

```
┌─────────────────────────────────────────────┐
│  Presentation Layer (Web API, Console App)  │
├─────────────────────────────────────────────┤
│  Infrastructure Layer (Repositories)        │
├─────────────────────────────────────────────┤
│  Application Layer (Interfaces, DTOs)       │
├─────────────────────────────────────────────┤
│  Domain Layer (Entities, Business Logic)    │ <- Core
└─────────────────────────────────────────────┘
```

**Key Principles:**
- **Dependency Rule**: Dependencies point inward (toward Domain)
- **Domain Layer**: Contains business logic, no external dependencies
- **Application Layer**: Defines interfaces, use cases
- **Infrastructure**: Implements data access (ADO.NET, EF Core)
- **Presentation**: Web API, Console App (interchangeable)

**Benefits over simple MVC:**
- Better testability (domain logic isolated)
- Flexibility (swap data access implementations)
- Maintainability (clear separation of concerns)
- Multiple UIs can share the same core logic

---

## Project Structure

### DbDemo.WebApi Project Layout

```
DbDemo.WebApi/
├── Controllers/           # API endpoints (Controller layer)
│   ├── BooksController.cs
│   └── CategoriesController.cs
├── DTOs/                 # Data Transfer Objects
│   ├── BookDto.cs
│   ├── CreateBookRequest.cs
│   ├── UpdateBookRequest.cs
│   ├── CategoryDto.cs
│   ├── ApiResponse.cs
│   └── PaginatedResponse.cs
├── Middleware/           # Custom middleware components
│   └── ErrorHandlingMiddleware.cs
├── Program.cs            # Application entry point, DI configuration
├── appsettings.json      # Configuration
└── DbDemo.WebApi.csproj  # Project file
```

### Key Components Explained

#### 1. Controllers
Controllers handle HTTP requests and return HTTP responses.

```csharp
[ApiController]
[Route("api/[controller]")]  // Route: /api/books
public class BooksController : ControllerBase
{
    private readonly IBookRepository _repository;

    // Dependency injection in constructor
    public BooksController(IBookRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]  // GET /api/books
    public async Task<ActionResult<List<BookDto>>> GetBooks()
    {
        // Implementation
    }
}
```

**Controller Attributes:**
- `[ApiController]` - Enables automatic model validation, binding
- `[Route]` - Defines URL pattern
- `[HttpGet]`, `[HttpPost]`, etc. - HTTP method routing

#### 2. DTOs (Data Transfer Objects)
DTOs shape the data sent to/from the API, separate from domain entities.

**Why use DTOs instead of domain entities?**
- **Security**: Hide internal implementation details
- **Versioning**: Change API contract without changing domain
- **Validation**: Add API-specific validation rules
- **Optimization**: Return only needed fields

```csharp
// Domain Entity (internal)
public class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; }
    public int AvailableCopies { get; set; }
    public bool IsDeleted { get; set; }  // Don't expose!
    // ... complex business logic
}

// DTO (API contract)
public class BookDto
{
    public int Id { get; set; }
    public string ISBN { get; set; }
    public string Title { get; set; }
    public int AvailableCopies { get; set; }
    // IsDeleted not exposed!
}
```

#### 3. Middleware
Middleware components process HTTP requests/responses in a pipeline.

```
Request  →  [Middleware 1]  →  [Middleware 2]  →  [Controller]
Response ←  [Middleware 1]  ←  [Middleware 2]  ←  [Controller]
```

**Our middleware components:**
- **Error handling** (`ErrorHandlingMiddleware`) - Catches and formats exceptions
- **Transaction management** (`TransactionMiddleware`) - Manages database transactions per request

**Common middleware:**
- Authentication/Authorization
- Logging
- CORS
- Compression

##### Transaction Middleware

The `TransactionMiddleware` automatically manages database transactions for each HTTP request:

```csharp
public class TransactionMiddleware
{
    public async Task InvokeAsync(HttpContext context,
        ITransactionContext transactionContext,
        SqlConnection connection)
    {
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            transactionContext.Initialize(connection, transaction);
            await _next(context);  // Execute controllers

            // Commit on success (2xx status codes)
            if (context.Response.StatusCode >= 200 &&
                context.Response.StatusCode < 300)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
```

**Benefits:**
- **Automatic transaction management** - No need to manually open/close connections in controllers
- **Consistent behavior** - All requests follow the same transaction pattern
- **Clean controllers** - Business logic isn't mixed with transaction boilerplate
- **Safe rollback** - Transactions automatically rollback on errors

**Before (with manual transaction management):**
```csharp
[HttpPost]
public async Task<ActionResult> CreateBook([FromBody] CreateBookRequest request)
{
    await _connection.OpenAsync();
    using var transaction = _connection.BeginTransaction();

    try
    {
        var book = await _repository.CreateAsync(book, transaction);
        await transaction.CommitAsync();
        return Ok(book);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
    finally
    {
        await _connection.CloseAsync();
    }
}
```

**After (with TransactionMiddleware):**
```csharp
[HttpPost]
public async Task<ActionResult> CreateBook([FromBody] CreateBookRequest request)
{
    var transaction = _transactionContext.Transaction;
    var book = await _repository.CreateAsync(book, transaction);
    return Ok(book);
}
```

The middleware handles all connection/transaction lifecycle, reducing controller code by ~70%!

#### 4. Program.cs
Configures services and the HTTP request pipeline.

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services (Dependency Injection)
builder.Services.AddControllers();
builder.Services.AddTransient<SqlConnection>(_ => new SqlConnection(connectionString));
builder.Services.AddScoped<ITransactionContext, TransactionContext>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Configure Middleware Pipeline
app.UseErrorHandling();          // Error handling (first!)
app.UseTransactionManagement();  // Transaction management
app.UseSwagger();                // Swagger docs
app.UseAuthorization();          // Auth middleware
app.MapControllers();            // Route to controllers

app.Run();  // Start the server
```

**Service lifetimes:**
- **Transient** (`SqlConnection`) - New instance every time
- **Scoped** (`ITransactionContext`, repositories) - One instance per HTTP request
- **Singleton** - One instance for application lifetime

**Middleware order matters!**
1. Error handling must be first to catch all exceptions
2. Transaction management comes after error handling
3. Controllers execute last in the pipeline

### Dependency Injection (DI)

ASP.NET Core has built-in dependency injection. Instead of creating dependencies manually, you register them and the framework injects them.

**Without DI (tightly coupled):**
```csharp
public class BooksController
{
    private readonly BookRepository _repository;

    public BooksController()
    {
        _repository = new BookRepository();  // Hard-coded!
    }
}
```

**With DI (loosely coupled):**
```csharp
// 1. Register in Program.cs
builder.Services.AddScoped<IBookRepository, BookRepository>();

// 2. Inject via constructor
public class BooksController
{
    private readonly IBookRepository _repository;

    public BooksController(IBookRepository repository)
    {
        _repository = repository;  // Injected by framework!
    }
}
```

**DI Lifetimes:**
- **Transient**: New instance every time (e.g., `SqlConnection`)
- **Scoped**: One instance per HTTP request (e.g., repositories)
- **Singleton**: One instance for application lifetime (e.g., configuration)

---

## Running the Web API

### Prerequisites

1. **SQL Server running** (via Docker):
   ```bash
   docker-compose up -d
   ```

2. **Environment variables** loaded from `.env` file

### Starting the API

The Web API automatically runs database migrations on startup, so you don't need to run the Setup project separately!

```bash
dotnet run -p src/DbDemo.WebApi/
```

**Note:** The API will check for and apply any pending migrations before starting the server. If migrations fail, the API will still start but you may need to run migrations manually.

**Expected output:**
```
====================================
Library Management Web API
====================================

🔍 Checking database migrations...
✅ Database is up to date (0 migrations applied)
✅ Database schema is now up to date!

====================================
Starting Web API Server
====================================
Environment: Development
Database: LibraryDb
Server: localhost:1453
Swagger UI: http://localhost:5000
====================================
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Verify It's Running

Open your browser to: **http://localhost:5000**

You should see the **Swagger UI** with interactive API documentation.

---

## Testing with Swagger UI

**Swagger UI** is an interactive documentation interface automatically generated from your API.

### Accessing Swagger

1. Start the Web API: `dotnet run`
2. Open browser: `http://localhost:5000`
3. You'll see all available endpoints

### Testing Endpoints in Swagger

#### Example: Get All Books

1. **Find the endpoint**: `GET /api/books`
2. **Click "Try it out"**
3. **Set parameters** (optional):
   - `page`: 1
   - `pageSize`: 10
   - `categoryId`: (leave empty)
4. **Click "Execute"**
5. **View the response**:
   - **Response Code**: `200 OK`
   - **Response Body**:
     ```json
     {
       "data": [
         {
           "id": 1,
           "isbn": "9780134685991",
           "title": "Effective Java",
           "categoryName": "Programming",
           "availableCopies": 5
         }
       ],
       "page": 1,
       "pageSize": 10,
       "totalCount": 1,
       "totalPages": 1
     }
     ```

#### Example: Create a New Book

1. **Find**: `POST /api/books`
2. **Click "Try it out"**
3. **Edit the JSON request body**:
   ```json
   {
     "isbn": "9781234567890",
     "title": "Learning ASP.NET Core",
     "categoryId": 1,
     "totalCopies": 5,
     "publisher": "Tech Press",
     "language": "English"
   }
   ```
4. **Click "Execute"**
5. **Check response**: `201 Created` with the created book data

#### Example: Update a Book

1. **Find**: `PUT /api/books/{id}`
2. **Click "Try it out"**
3. **Enter ID**: 1
4. **Edit request body** (only fields to update):
   ```json
   {
     "title": "Effective Java - 3rd Edition",
     "totalCopies": 10
   }
   ```
5. **Click "Execute"**
6. **Check response**: `200 OK` with updated data

#### Example: Delete a Book

1. **Find**: `DELETE /api/books/{id}`
2. **Click "Try it out"**
3. **Enter ID**: 1
4. **Click "Execute"**
5. **Check response**: `200 OK` with success message

---

## Testing with curl and Postman

### Using curl (Command Line)

#### GET Request
```bash
# Get all books
curl -X GET "http://localhost:5000/api/books?page=1&pageSize=10" \
     -H "accept: application/json"

# Get specific book
curl -X GET "http://localhost:5000/api/books/1" \
     -H "accept: application/json"

# Search books by title
curl -X GET "http://localhost:5000/api/books/search?query=Java" \
     -H "accept: application/json"
```

#### POST Request (Create)
```bash
curl -X POST "http://localhost:5000/api/books" \
     -H "Content-Type: application/json" \
     -d '{
       "isbn": "9781234567890",
       "title": "Learning Web APIs",
       "categoryId": 1,
       "totalCopies": 3
     }'
```

#### PUT Request (Update)
```bash
curl -X PUT "http://localhost:5000/api/books/1" \
     -H "Content-Type: application/json" \
     -d '{
       "title": "Effective Java - Updated",
       "totalCopies": 8
     }'
```

#### DELETE Request
```bash
curl -X DELETE "http://localhost:5000/api/books/1" \
     -H "accept: application/json"
```

### Using Postman

**Postman** is a popular GUI tool for testing APIs.

1. **Download**: https://www.postman.com/downloads/
2. **Create a new request**
3. **Set the HTTP method**: GET, POST, PUT, DELETE
4. **Enter URL**: `http://localhost:5000/api/books`
5. **For POST/PUT**, go to **Body** tab:
   - Select "raw"
   - Choose "JSON" format
   - Enter JSON data
6. **Click "Send"**
7. **View response** below

**Postman Collections**: You can save multiple requests and organize them for easy testing.

---

## Understanding HTTP Status Codes

HTTP status codes indicate the result of an HTTP request.

### Success Codes (2xx)

| Code | Name | Meaning | When Used in Our API |
|------|------|---------|---------------------|
| **200** | OK | Request succeeded | GET, PUT, DELETE succeeded |
| **201** | Created | Resource created | POST created a new book |
| **204** | No Content | Success, no response body | Alternative to 200 for DELETE |

**Example:**
```http
GET /api/books/1
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "data": { "id": 1, "title": "Effective Java" }
}
```

### Client Error Codes (4xx)

| Code | Name | Meaning | When Used in Our API |
|------|------|---------|---------------------|
| **400** | Bad Request | Invalid request data | Validation failed, invalid JSON |
| **404** | Not Found | Resource doesn't exist | Book ID not found |
| **409** | Conflict | Request conflicts with state | Duplicate ISBN |
| **422** | Unprocessable Entity | Validation error | Business rule violation |

**Example:**
```http
GET /api/books/999
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "success": false,
  "message": "Book with ID 999 not found"
}
```

### Server Error Codes (5xx)

| Code | Name | Meaning | When Used in Our API |
|------|------|---------|---------------------|
| **500** | Internal Server Error | Unexpected server error | Database connection failed, unhandled exception |
| **503** | Service Unavailable | Server temporarily down | Database maintenance |

**Example:**
```http
POST /api/books
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "success": false,
  "message": "An internal server error occurred",
  "errors": ["An unexpected error occurred. Please try again later."]
}
```

### Status Code Best Practices

1. **Be consistent**: Use the same status codes for the same situations
2. **Be specific**: Use the most appropriate code (404 vs 400 vs 500)
3. **Provide messages**: Include helpful error messages in response body
4. **Document**: Document which status codes each endpoint returns

---

## Key Concepts

### 1. Routing

Routing maps HTTP requests to controller actions.

**Attribute Routing (used in our project):**
```csharp
[Route("api/[controller]")]  // /api/books
public class BooksController : ControllerBase
{
    [HttpGet]                    // GET /api/books
    public ActionResult GetAll() { }

    [HttpGet("{id}")]            // GET /api/books/5
    public ActionResult Get(int id) { }

    [HttpPost]                   // POST /api/books
    public ActionResult Create() { }

    [HttpPut("{id}")]            // PUT /api/books/5
    public ActionResult Update(int id) { }

    [HttpDelete("{id}")]         // DELETE /api/books/5
    public ActionResult Delete(int id) { }
}
```

**Route Parameters:**
- `{id}` - Required parameter
- `{id?}` - Optional parameter
- `{id:int}` - Type constraint

**Query Parameters:**
```csharp
[HttpGet]
public ActionResult GetBooks(
    [FromQuery] int page = 1,       // ?page=1
    [FromQuery] int pageSize = 10,  // &pageSize=10
    [FromQuery] int? categoryId)    // &categoryId=5
{
    // ...
}
```

### 2. Model Binding and Validation

**Model Binding** automatically maps HTTP request data to action method parameters.

**Sources:**
- `[FromRoute]` - URL parameters: `/api/books/{id}`
- `[FromQuery]` - Query string: `?page=1&pageSize=10`
- `[FromBody]` - Request body (JSON)
- `[FromHeader]` - HTTP headers
- `[FromForm]` - Form data

**Validation with Data Annotations:**
```csharp
public class CreateBookRequest
{
    [Required(ErrorMessage = "ISBN is required")]
    [RegularExpression(@"^\d{10}(\d{3})?$")]
    public string ISBN { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; }

    [Range(0, 1000)]
    public int TotalCopies { get; set; }
}
```

**Automatic Validation** (with `[ApiController]`):
```csharp
[HttpPost]
public ActionResult Create([FromBody] CreateBookRequest request)
{
    // If validation fails, 400 Bad Request is returned automatically!
    // No need to check ModelState manually
}
```

### 3. Async/Await Pattern

Web APIs should be **asynchronous** to handle many concurrent requests efficiently.

**Synchronous (blocks thread):**
```csharp
public ActionResult<Book> GetBook(int id)
{
    var book = _repository.GetById(id);  // Blocks thread!
    return Ok(book);
}
```

**Asynchronous (non-blocking):**
```csharp
public async Task<ActionResult<Book>> GetBook(int id)
{
    var book = await _repository.GetByIdAsync(id);  // Releases thread!
    return Ok(book);
}
```

**Benefits:**
- Thread not blocked during I/O operations
- Server can handle more concurrent requests
- Better scalability

**Rule**: Always use async for database calls, HTTP requests, file I/O.

### 4. Transaction Management

Our repositories require explicit transaction management for ADO.NET.

```csharp
[HttpPost]
public async Task<ActionResult> CreateBook([FromBody] CreateBookRequest request)
{
    await _connection.OpenAsync();
    using var transaction = _connection.BeginTransaction();

    try
    {
        // Multiple operations in one transaction
        var book = await _repository.CreateAsync(book, transaction);
        await _auditRepo.LogAsync(book.Id, "Created", transaction);

        await transaction.CommitAsync();  // Success - persist changes
        return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();  // Failure - undo changes
        throw;
    }
    finally
    {
        await _connection.CloseAsync();
    }
}
```

**Why transactions?**
- **Atomicity**: All operations succeed or all fail
- **Consistency**: Database stays in valid state
- **Data integrity**: No partial updates

### 5. CORS (Cross-Origin Resource Sharing)

CORS allows your API to be called from web browsers on different domains.

**Configured in Program.cs:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()      // Allow requests from any domain
              .AllowAnyMethod()      // Allow GET, POST, PUT, DELETE
              .AllowAnyHeader();     // Allow any headers
    });
});

app.UseCors("AllowAll");
```

**Production**: Restrict to specific origins:
```csharp
policy.WithOrigins("https://myapp.com")
      .WithMethods("GET", "POST")
      .WithHeaders("Content-Type");
```

---

## Next Steps and Research Topics

Congratulations on building your first ASP.NET Core Web API! Here are suggested topics to deepen your knowledge.

### Beginner-Level Topics to Explore

1. **API Versioning**
   - How to version your API (`/api/v1/books`, `/api/v2/books`)
   - URL versioning vs header versioning
   - When to create a new version

2. **Logging and Monitoring**
   - Using `ILogger` for structured logging
   - Logging HTTP requests and responses
   - Application Insights for monitoring

3. **Configuration Management**
   - Using `appsettings.json` and environment variables
   - User secrets for local development
   - Azure Key Vault for production secrets

4. **Input Validation Deep Dive**
   - FluentValidation library
   - Custom validation attributes
   - Business rule validation

### Intermediate-Level Topics

5. **Authentication and Authorization**
   - **JWT (JSON Web Tokens)**: Stateless authentication
   - **OAuth 2.0**: Third-party authentication (Google, Facebook)
   - **ASP.NET Core Identity**: User management system
   - Role-based and policy-based authorization

   **Research Questions:**
   - What is the difference between authentication and authorization?
   - How does JWT work and why is it stateless?
   - What is OAuth and when should you use it?

6. **Rate Limiting and Throttling**
   - Protect API from abuse
   - Limit requests per user/IP
   - .NET 7+ built-in rate limiting middleware

   **Research Questions:**
   - Why is rate limiting important?
   - What's the difference between throttling and rate limiting?
   - What are common rate limiting strategies?

7. **API Security Best Practices**
   - **HTTPS**: Always use SSL/TLS in production
   - **Input validation**: Prevent SQL injection, XSS
   - **OWASP API Security Top 10**: Study common vulnerabilities
   - **API keys**: Simple authentication mechanism

   **Research Questions:**
   - What is the OWASP Top 10 for APIs?
   - How do you prevent SQL injection in Web APIs?
   - What is HTTPS and why is it essential?

8. **Caching Strategies**
   - **In-memory caching**: Store frequently accessed data
   - **Distributed caching**: Redis for multi-server scenarios
   - **HTTP caching headers**: ETag, Cache-Control

   **Research Questions:**
   - When should you use caching?
   - What is the difference between in-memory and distributed caching?
   - How do HTTP cache headers work?

9. **API Documentation**
   - **Swagger/OpenAPI**: Generate interactive docs (you've already used this!)
   - **API design guidelines**: Microsoft REST API guidelines
   - **Versioning documentation**: Document API changes

   **Research Questions:**
   - What is the OpenAPI specification?
   - How can you improve your API documentation?
   - What are best practices for API design?

### Advanced Topics

10. **GraphQL as an Alternative to REST**
    - **GraphQL**: Query language for APIs
    - **Advantages**: Client specifies exact data needed, single endpoint
    - **When to use**: Complex data requirements, mobile apps
    - **Libraries**: Hot Chocolate, GraphQL.NET

    **Research Questions:**
    - What is GraphQL and how does it differ from REST?
    - When should you choose GraphQL over REST?
    - What are the trade-offs?

11. **gRPC for High-Performance APIs**
    - **gRPC**: High-performance RPC framework using HTTP/2
    - **Protocol Buffers**: Binary serialization (faster than JSON)
    - **Use cases**: Microservices, real-time communication

    **Research Questions:**
    - What is gRPC and when is it better than REST?
    - What are Protocol Buffers?
    - How does HTTP/2 improve performance?

12. **API Gateway Pattern**
    - **API Gateway**: Single entry point for multiple microservices
    - **Features**: Routing, authentication, rate limiting, aggregation
    - **Tools**: Azure API Management, Kong, Ocelot

    **Research Questions:**
    - What problems does an API gateway solve?
    - When do you need an API gateway?
    - What is the Backend for Frontend (BFF) pattern?

13. **CQRS (Command Query Responsibility Segregation)**
    - **CQRS**: Separate read and write operations
    - **Benefits**: Optimized for different concerns, scalability
    - **MediatR library**: Implement CQRS in .NET

    **Research Questions:**
    - What is CQRS and when should you use it?
    - How does CQRS relate to Event Sourcing?
    - What is the Mediator pattern?

14. **Event-Driven Architecture**
    - **Message queues**: RabbitMQ, Azure Service Bus
    - **Event streaming**: Apache Kafka
    - **Asynchronous communication** between services

    **Research Questions:**
    - What is event-driven architecture?
    - When should you use message queues?
    - What is eventual consistency?

15. **Microservices Architecture**
    - **Microservices**: Break application into small, independent services
    - **Service discovery**: Consul, Eureka
    - **Distributed tracing**: OpenTelemetry
    - **Resilience patterns**: Circuit breaker, retry policies

    **Research Questions:**
    - What are microservices and when should you use them?
    - What are the challenges of microservices?
    - What is the Circuit Breaker pattern?

### Hands-On Extension Ideas

16. **Add Authentication to Your API**
    - Implement JWT authentication
    - Protect endpoints with `[Authorize]` attribute
    - Add role-based access control

17. **Build Author and Loan Endpoints**
    - Create `AuthorsController` with CRUD operations
    - Create `LoansController` with borrow/return workflows
    - Handle many-to-many relationships (Book-Author)

18. **Create a Frontend Application**
    - **React**: Build a book management UI
    - **Angular**: Create a library dashboard
    - **Blazor**: Use C# for frontend (full-stack .NET!)

    Connect it to your Web API using `fetch()` or `axios`.

19. **Add Integration Tests**
    - Use `WebApplicationFactory` for in-memory testing
    - Test API endpoints end-to-end
    - Verify HTTP status codes and responses

20. **Deploy Your API to the Cloud**
    - **Azure App Service**: Deploy with a few clicks
    - **Docker**: Containerize your API
    - **Kubernetes**: Orchestrate containers

    **Research Questions:**
    - What is Platform as a Service (PaaS)?
    - What is containerization and why use Docker?
    - What is Kubernetes and when do you need it?

### Architecture Patterns to Study

21. **Clean Architecture (You're Already Using It!)**
    - Study the principles deeper
    - Compare with Hexagonal Architecture
    - Understand the Dependency Inversion Principle

22. **Repository Pattern vs Unit of Work**
    - **Repository Pattern**: Abstract data access (you've used this)
    - **Unit of Work**: Manage transactions across multiple repositories

    **Research Questions:**
    - What is the Unit of Work pattern?
    - How does it complement the Repository pattern?
    - When should you use these patterns?

23. **Mediator Pattern with MediatR**
    - Decouple request/response handling
    - Implement CQRS more easily
    - Centralize cross-cutting concerns

---

## Summary

In this lab, you learned:

- ✅ **What a Web API is** and when to use it
- ✅ **REST principles** and HTTP methods
- ✅ **ASP.NET Core** framework basics
- ✅ **MVC architecture** and how it applies to Web APIs
- ✅ **Project structure**: Controllers, DTOs, Middleware
- ✅ **Dependency Injection** and service lifetimes
- ✅ **Running and testing** APIs with Swagger, curl, and Postman
- ✅ **HTTP status codes** and their meanings
- ✅ **Routing, validation, async/await**, and transactions

### Key Takeaways

1. **Web APIs enable cross-platform communication** using HTTP and JSON
2. **ASP.NET Core is a modern, high-performance, cross-platform framework**
3. **MVC separates concerns**, but Web APIs only use Model and Controller
4. **Clean Architecture provides better separation** than simple MVC
5. **DTOs protect your domain** and provide a stable API contract
6. **Dependency Injection makes code testable** and loosely coupled
7. **Always use async/await** for I/O operations
8. **Swagger is essential** for API documentation and testing

---

## Additional Resources

### Official Documentation
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
- [OpenAPI Specification](https://swagger.io/specification/)

### Books
- "ASP.NET Core in Action" by Andrew Lock
- "Building Microservices" by Sam Newman
- "RESTful Web API Design with Node.js" (concepts apply to any language)

### Online Courses
- Microsoft Learn: ASP.NET Core path
- Pluralsight: ASP.NET Core Web API courses
- YouTube: Nick Chapsas, Tim Corey (excellent .NET content)

### Practice Projects
1. **Todo API**: Classic beginner project
2. **E-commerce API**: Products, orders, customers
3. **Social Media API**: Posts, comments, likes
4. **Weather API**: Integrate external data sources
5. **Blog API**: Articles, tags, comments

---

## Glossary

- **API**: Application Programming Interface - a contract for communication between software
- **REST**: Representational State Transfer - architectural style for web services
- **HTTP**: HyperText Transfer Protocol - foundation of web communication
- **JSON**: JavaScript Object Notation - lightweight data format
- **DTO**: Data Transfer Object - object that carries data between processes
- **CRUD**: Create, Read, Update, Delete - basic data operations
- **MVC**: Model-View-Controller - software design pattern
- **DI**: Dependency Injection - design pattern for loose coupling
- **JWT**: JSON Web Token - secure way to transmit information
- **CORS**: Cross-Origin Resource Sharing - mechanism to allow cross-domain requests
- **ORM**: Object-Relational Mapping - technique to query databases using objects
- **gRPC**: Google Remote Procedure Call - high-performance RPC framework
- **CQRS**: Command Query Responsibility Segregation - pattern separating reads and writes

---

**🎓 Continue learning and building! The best way to master Web APIs is through practice. Pick one of the extension ideas above and start coding!**
