# DbDemo Frontend

A simple React demo application showcasing integration with the DbDemo.WebApi library management system.

## Purpose

This is a **demonstration frontend** created to illustrate how to integrate with the DbDemo.WebApi. It provides working examples of:

- Type-safe API calls using auto-generated TypeScript client
- CRUD operations (Create, Read, Update, Delete)
- Pagination handling
- Search functionality
- Category filtering
- Error handling

**Note:** This is an educational example. Developers should use this as a reference and build their own frontend or integrate via console applications, mobile apps, or other clients.

## Tech Stack

- **React** - UI library
- **Vite** - Build tool and dev server
- **Axios** - HTTP client
- **swagger-typescript-api** - Auto-generated type-safe API client

## Prerequisites

- Node.js (v16 or later)
- npm or yarn
- DbDemo.WebApi running on `http://localhost:5000`

## Getting Started

### 1. Install Dependencies

```bash
npm install
```

### 2. Start the API

Make sure the Web API is running (from the root):

```bash
dotnet run -p src/DbDemo.WebApi/
```

The API should be accessible at `http://localhost:5000`

### 3. Run the Frontend

```bash
npm run dev
```

The application will start at `http://localhost:5173` (or another port if 5173 is busy).

## Regenerating the API Client

Whenever the Web API changes (new endpoints, updated models, etc.), regenerate the TypeScript client:

```bash
./generate-api.sh
```

Or manually:

```bash
npx swagger-typescript-api generate \
  -p http://localhost:5000/swagger/v1/swagger.json \
  -o ./src/api \
  -n Api.ts \
  --axios
```

## Project Structure

```
src/
├── api/              # Auto-generated API client
│   └── Api.ts
├── components/       # React components
│   ├── BookList.jsx  # Book listing with pagination and search
│   └── BookForm.jsx  # Create/Edit book form
├── App.jsx           # Main application component
├── App.css           # Application styles
└── main.jsx          # Entry point
```

## Features Demonstrated

### Book Management
- **List Books** - Paginated list with 10 books per page
- **Search Books** - Search by title
- **Filter by Category** - Dropdown filter
- **Create Book** - Form with validation
- **Edit Book** - Pre-filled form with existing data
- **Delete Book** - Soft delete with confirmation

### API Integration Patterns
- Using auto-generated TypeScript types
- Error handling from `ApiResponse` wrapper
- Pagination with `PaginatedResponse`
- Form validation and server-side error display
- Loading states and user feedback

## API Client Usage Examples

### Listing Books (with pagination)

```javascript
const response = await api.booksList({
  page: 1,
  pageSize: 10,
  categoryId: 5  // Optional filter
});

console.log(response.data.data);        // Array of books
console.log(response.data.totalPages);   // Total pages
console.log(response.data.hasNextPage);  // Boolean
```

### Searching Books

```javascript
const response = await api.booksSearchList({
  query: "harry potter"
});

if (response.data.success) {
  console.log(response.data.data);  // Matching books
}
```

### Creating a Book

```javascript
const newBook = {
  isbn: "9780545010221",
  title: "Harry Potter and the Deathly Hallows",
  categoryId: 1,
  totalCopies: 5,
  publisher: "Scholastic",
  language: "English"
};

const response = await api.booksCreate(newBook);

if (response.data.success) {
  console.log("Book created:", response.data.data);
} else {
  console.error("Errors:", response.data.errors);
}
```

### Updating a Book

```javascript
const updates = {
  title: "Updated Title",
  totalCopies: 10
};

const response = await api.booksUpdate(bookId, updates);
```

### Deleting a Book

```javascript
await api.booksDelete(bookId);
```

### Loading Categories

```javascript
const response = await api.categoriesList();
const categories = response.data.data;
```

## Configuration

The API base URL is configured in `src/App.jsx`:

```javascript
const api = new Api({
  baseURL: 'http://localhost:5000'
});
```

Change this if your API is running on a different host/port.

## Building for Production

```bash
npm run build
```

The build output will be in the `dist/` directory.

## Next Steps for Developers

This demo shows the basics. To build a production application, consider adding:

1. **Authentication** - JWT tokens, OAuth, etc.
2. **State Management** - Redux, Zustand, or Context API
3. **Routing** - React Router for multi-page navigation
4. **Better UI** - Material-UI, Chakra UI, or Tailwind CSS
5. **Testing** - Vitest, React Testing Library
6. **Form Library** - React Hook Form, Formik
7. **Data Fetching** - React Query, SWR for caching
8. **Error Boundaries** - Better error handling
9. **Accessibility** - ARIA labels, keyboard navigation
10. **Performance** - Code splitting, lazy loading

## Documentation

For more information about the API, see:
- Swagger UI: `http://localhost:5000` (when API is running)
- API Integration Guide: `../../docs/API_INTEGRATION_GUIDE.md`

## License

This is a demo project for educational purposes.
