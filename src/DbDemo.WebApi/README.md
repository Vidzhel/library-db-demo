# DbDemo.WebApi - Library Management REST API

ASP.NET Core Web API for the Library Management System, demonstrating RESTful API design with ADO.NET.

## Quick Start

```bash
# 1. Start SQL Server (if not already running)
docker-compose up -d

# 2. Run the Web API (migrations run automatically!)
# From project root:
dotnet run -p src/DbDemo.WebApi/

# OR from the WebApi directory:
cd src/DbDemo.WebApi
dotnet run

# 3. Open Swagger UI in your browser
# http://localhost:5000
```

## Features

- **RESTful API Design** - Books and Categories CRUD operations
- **Swagger/OpenAPI** - Interactive API documentation
- **ADO.NET Data Access** - Direct SQL with transactions
- **Input Validation** - DataAnnotations with automatic validation
- **Error Handling** - Global middleware for consistent responses
- **Pagination & Filtering** - Efficient list endpoints
- **Auto Migrations** - Database schema updates on startup

## API Endpoints

### Books
- `GET /api/books` - List books (paginated, filterable)
- `GET /api/books/{id}` - Get book by ID
- `GET /api/books/search?query={term}` - Search books by title
- `POST /api/books` - Create new book
- `PUT /api/books/{id}` - Update book
- `DELETE /api/books/{id}` - Delete book (soft delete)
- `GET /api/books/{id}/category` - Get book's category

### Categories
- `GET /api/categories` - List all categories
- `GET /api/categories/{id}` - Get category by ID
- `POST /api/categories` - Create category
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category

## Testing the API

### Swagger UI (Recommended)
Open http://localhost:5000 and use the interactive interface to test endpoints.

### curl Examples

```bash
# Get all books
curl http://localhost:5000/api/books

# Get specific book
curl http://localhost:5000/api/books/1

# Create a book
curl -X POST http://localhost:5000/api/books \
  -H "Content-Type: application/json" \
  -d '{
    "isbn": "9781234567890",
    "title": "Learning Web APIs",
    "categoryId": 1,
    "totalCopies": 5
  }'

# Search books
curl "http://localhost:5000/api/books/search?query=Java"
```

## Architecture

```
DbDemo.WebApi/
├── Controllers/          # API endpoints
│   ├── BooksController.cs
│   └── CategoriesController.cs
├── DTOs/                # Request/response models
├── Middleware/          # Error handling
└── Program.cs           # Startup & DI configuration
```

The Web API reuses the Clean Architecture layers from the console app:
- **Domain** - Business entities (Book, Category)
- **Application** - Repository interfaces
- **Infrastructure** - ADO.NET implementations

## Configuration

Connection string is loaded from `.env` file:
- `DB_HOST` - SQL Server host
- `DB_PORT` - SQL Server port
- `DB_NAME` - Database name
- `APP_USER` / `APP_PASSWORD` - Application credentials
- `SA_USER` / `SA_PASSWORD` - Admin credentials (for migrations)

## Learn More

See the comprehensive tutorial: [docs/31-aspnet-web-api.md](../../docs/31-aspnet-web-api.md)

Topics covered:
- What is a Web API and REST?
- ASP.NET Core fundamentals
- MVC architecture
- Dependency injection
- HTTP status codes
- Next steps for advanced learning
