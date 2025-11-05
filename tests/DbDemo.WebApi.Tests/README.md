# DbDemo.WebApi Tests

Comprehensive test suite for the DbDemo WebApi project.

## Test Structure

```
DbDemo.WebApi.Tests/
├── Controllers/
│   ├── BooksControllerTests.cs          # Unit tests for BooksController
│   └── CategoriesControllerTests.cs     # Unit tests for CategoriesController
├── Middleware/
│   └── ErrorHandlingMiddlewareTests.cs  # Unit tests for error handling middleware
├── DTOs/
│   └── ValidationTests.cs               # Validation tests for DTOs and request models
└── Integration/
    └── WebApiIntegrationTests.cs        # Integration tests for API endpoints
```

## Test Categories

### 1. Controller Unit Tests

**BooksControllerTests** (32 tests)
- GetBooks endpoint tests (pagination, filtering, validation)
- GetBook endpoint tests (valid/invalid IDs)
- SearchBooks endpoint tests (query validation)
- CreateBook endpoint tests (validation, duplicate ISBN, missing category)
- UpdateBook endpoint tests (validation, missing entities)
- DeleteBook endpoint tests (soft delete operations)
- GetBookCategory endpoint tests

**CategoriesControllerTests** (16 tests)
- GetCategories endpoint tests
- GetCategory endpoint tests (valid/invalid IDs)
- CreateCategory endpoint tests (validation)
- UpdateCategory endpoint tests (full/partial updates)
- DeleteCategory endpoint tests

### 2. Middleware Tests

**ErrorHandlingMiddlewareTests** (10 tests)
- ArgumentException handling → BadRequest
- ArgumentNullException handling → BadRequest
- InvalidOperationException handling → BadRequest
- KeyNotFoundException handling → NotFound
- Generic Exception handling → InternalServerError
- Error logging verification
- JSON response format validation
- Multiple exception type validation

### 3. DTO Validation Tests

**ValidationTests** (18 tests)
- CreateBookRequest validation (ISBN format, required fields, length constraints)
- CreateCategoryRequest validation
- UpdateBookRequest validation
- ApiResponse helper methods
- PaginatedResponse pagination calculations

### 4. Integration Tests

**WebApiIntegrationTests** (10 tests)
- HTTP endpoint availability
- Content-Type header verification
- Error status code validation
- Response format validation
- CORS policy verification

## Running the Tests

### Run all tests
```bash
dotnet test
```

### Run with detailed output
```bash
dotnet test --verbosity detailed
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~BooksControllerTests"
```

### Run with coverage
```bash
dotnet test /p:CollectCoverage=true
```

## Test Dependencies

- **xUnit**: Test framework
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Fluent assertion library for readable tests
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing support

## Important Notes

### Controller Test Limitations

The controller tests have a known limitation due to the architecture of the controllers:
- Controllers directly manage SqlConnection opening/closing
- SqlConnection is a sealed class and cannot be mocked
- Tests use a real SqlConnection object with a dummy connection string that never opens

**Current Approach:**
- Repository methods are mocked
- Connection object exists but is not opened in tests
- Tests verify business logic and response types

**For Full Integration Testing:**
To test the complete database interaction flow, use the integration tests in `DbDemo.Integration.Tests` project which:
- Use a real test database
- Test full request/response cycles
- Verify database state changes

## Test Coverage

Current test coverage includes:
- ✅ Controller action methods
- ✅ Request validation
- ✅ Response formatting
- ✅ Error handling
- ✅ Middleware behavior
- ✅ DTO validation rules
- ✅ Pagination logic
- ✅ HTTP endpoint contracts

## Adding New Tests

When adding new endpoints or features:

1. Add controller unit tests in `Controllers/`
2. Add validation tests if new DTOs are introduced
3. Add integration tests for the HTTP contract
4. Update this README with new test counts

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- No external dependencies required for unit/middleware tests
- Integration tests may require environment configuration
- All tests should complete within 30 seconds

## Test Data Helpers

Test helper methods are provided in each test class:
- `CreateTestBook()`: Creates test Book entities
- `CreateTestCategory()`: Creates test Category entities
- `ValidateModel()`: Validates data annotations

These helpers use reflection to set readonly properties for testing purposes.
