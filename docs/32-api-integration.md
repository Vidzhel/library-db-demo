# DbDemo.WebApi Integration Guide

Complete guide for integrating with the DbDemo Library Management Web API.

## Table of Contents

1. [Overview](#overview)
2. [Getting Started](#getting-started)
3. [Authentication](#authentication)
4. [API Endpoints](#api-endpoints)
5. [Data Models](#data-models)
6. [Response Format](#response-format)
7. [Error Handling](#error-handling)
8. [Code Examples](#code-examples)
9. [Best Practices](#best-practices)

## Overview

The DbDemo.WebApi is a RESTful API for managing a library system with books and categories. It demonstrates:

- Clean Architecture principles
- ADO.NET for database access
- Comprehensive CRUD operations
- Pagination support
- Transaction management
- Swagger/OpenAPI documentation

**Base URL:** `http://localhost:5000`
**Swagger UI:** `http://localhost:5000`
**OpenAPI Spec:** `http://localhost:5000/swagger/v1/swagger.json`

## Getting Started

### Prerequisites

- API running on localhost:5000
- HTTP client (fetch, axios, HttpClient, curl, etc.)
- Understanding of REST principles

### Quick Test

```bash
# Check API health
curl http://localhost:5000/api/books

# Get first page of books
curl http://localhost:5000/api/books?page=1&pageSize=10
```

## Authentication

**Current Status:** None

The API currently has no authentication. All endpoints are publicly accessible.

**For Production:** Implement one of:
- JWT Bearer Tokens
- OAuth 2.0
- API Keys
- Basic Authentication

## Code Examples

### JavaScript / TypeScript (Fetch)

#### List Books

```javascript
const response = await fetch('http://localhost:5000/api/books?page=1&pageSize=10');
const data = await response.json();

console.log(`Total books: ${data.totalCount}`);
console.log(`Current page: ${data.page} of ${data.totalPages}`);
data.data.forEach(book => {
  console.log(`${book.title} by ${book.publisher}`);
});
```

#### Create Book

```javascript
const newBook = {
  isbn: "9780132350884",
  title: "Clean Code",
  subtitle: "A Handbook of Agile Software Craftsmanship",
  publisher: "Prentice Hall",
  categoryId: 1,
  totalCopies: 5,
  language: "English"
};

const response = await fetch('http://localhost:5000/api/books', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(newBook)
});

const result = await response.json();

if (result.success) {
  console.log('Book created:', result.data);
} else {
  console.error('Errors:', result.errors);
}
```

#### Search Books

```javascript
const query = 'clean code';
const response = await fetch(
  `http://localhost:5000/api/books/search?query=${encodeURIComponent(query)}`
);
const result = await response.json();

if (result.success) {
  result.data.forEach(book => console.log(book.title));
}
```

### JavaScript / TypeScript (Axios)

#### Setup

```bash
npm install axios
```

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json'
  }
});
```

#### List Books

```javascript
const { data } = await api.get('/api/books', {
  params: {
    page: 1,
    pageSize: 10,
    categoryId: 5  // Optional filter
  }
});

console.log(data.data);  // Array of books
```

#### Update Book

```javascript
const bookId = 123;
const updates = {
  title: "Updated Title",
  totalCopies: 10
};

try {
  const { data } = await api.put(`/api/books/${bookId}`, updates);

  if (data.success) {
    console.log('Updated:', data.data);
  } else {
    console.error('Errors:', data.errors);
  }
} catch (error) {
  console.error('Request failed:', error.message);
}
```

### C# (HttpClient)

```csharp
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000")
};

// List books
var booksResponse = await client.GetFromJsonAsync<PaginatedResponse<BookDto>>(
    "/api/books?page=1&pageSize=10"
);

Console.WriteLine($"Total: {booksResponse.TotalCount}");
foreach (var book in booksResponse.Data)
{
    Console.WriteLine($"{book.Title} - {book.Publisher}");
}

// Create book
var newBook = new CreateBookRequest
{
    Isbn = "9780132350884",
    Title = "Clean Code",
    CategoryId = 1,
    TotalCopies = 5
};

var createResponse = await client.PostAsJsonAsync("/api/books", newBook);
var result = await createResponse.Content.ReadFromJsonAsync<ApiResponse<BookDto>>();

if (result.Success)
{
    Console.WriteLine($"Created book with ID: {result.Data.Id}");
}
else
{
    Console.WriteLine("Errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### cURL

#### List Books

```bash
curl -X GET "http://localhost:5000/api/books?page=1&pageSize=10" \
  -H "Accept: application/json"
```

#### Create Book

```bash
curl -X POST "http://localhost:5000/api/books" \
  -H "Content-Type: application/json" \
  -d '{
    "isbn": "9780132350884",
    "title": "Clean Code",
    "categoryId": 1,
    "totalCopies": 5
  }'
```

#### Update Book

```bash
curl -X PUT "http://localhost:5000/api/books/123" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated Title",
    "totalCopies": 10
  }'
```

#### Delete Book

```bash
curl -X DELETE "http://localhost:5000/api/books/123"
```

### Python (requests)

```python
import requests

BASE_URL = "http://localhost:5000"

# List books
response = requests.get(f"{BASE_URL}/api/books", params={
    "page": 1,
    "pageSize": 10
})
books = response.json()
print(f"Total books: {books['totalCount']}")

# Create book
new_book = {
    "isbn": "9780132350884",
    "title": "Clean Code",
    "categoryId": 1,
    "totalCopies": 5
}
response = requests.post(f"{BASE_URL}/api/books", json=new_book)
result = response.json()

if result["success"]:
    print(f"Created book: {result['data']['title']}")
else:
    print(f"Errors: {result['errors']}")
```

## Best Practices

### 1. Use Pagination

Always use pagination for list endpoints to avoid performance issues:

```javascript
// Good
const books = await api.get('/api/books?page=1&pageSize=20');

// Bad (might return thousands of records)
const books = await api.get('/api/books');
```

### 2. Handle Errors Gracefully

Always check the `success` field and handle errors:

```javascript
const response = await api.post('/api/books', bookData);

if (response.data.success) {
  // Success
  handleSuccess(response.data.data);
} else {
  // Validation or business logic errors
  handleErrors(response.data.errors);
}
```

### 3. Use Type-Safe Clients

For TypeScript/JavaScript, use the auto-generated client:

```bash
npx swagger-typescript-api generate \
  -p http://localhost:5000/swagger/v1/swagger.json \
  -o ./src/api \
  --axios
```

### 4. Respect Field Constraints

Follow the validation rules:

- ISBN: 10 or 13 digits
- Title: Max 200 characters
- Description: Max 2000 characters
- Page Count: 1-10000
- Total Copies: 0-1000

### 5. Use Appropriate HTTP Methods

- `GET` - Retrieve data (idempotent, no side effects)
- `POST` - Create new resources
- `PUT` - Update existing resources (entire resource)
- `DELETE` - Remove resources

### 6. Monitor for Changes

If the API schema changes, regenerate your client:

```bash
# JavaScript/TypeScript
./generate-api.sh

# C# - Use NSwag or similar tools
```

### 7. Implement Retry Logic

For production apps, implement retry logic for transient failures:

```javascript
async function fetchWithRetry(url, options, retries = 3) {
  for (let i = 0; i < retries; i++) {
    try {
      return await fetch(url, options);
    } catch (error) {
      if (i === retries - 1) throw error;
      await new Promise(resolve => setTimeout(resolve, 1000 * (i + 1)));
    }
  }
}
```

### 8. Use Environment Variables

Don't hardcode the API URL:

```javascript
// .env
VITE_API_URL=http://localhost:5000

// In code
const api = new Api({
  baseURL: import.meta.env.VITE_API_URL
});
```

## CORS Configuration

The API is configured to allow all origins, methods, and headers (for development):

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**For Production:** Restrict to specific origins:

```csharp
policy.WithOrigins("https://your-domain.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

## Additional Resources

- **Swagger UI:** Interactive API documentation at `http://localhost:5000`
- **OpenAPI Spec:** Download at `http://localhost:5000/swagger/v1/swagger.json`
- **Demo Frontend:** See `src/DbDemo.Frontend/` for React implementation example
- **Source Code:** All controllers in `src/DbDemo.WebApi/Controllers/`

## Support

This is a demo/educational project. For questions:
1. Check the Swagger UI documentation
2. Review the demo React frontend for examples
3. Examine the controller source code
