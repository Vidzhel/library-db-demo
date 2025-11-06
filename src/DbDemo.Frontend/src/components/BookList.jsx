import { useState, useEffect } from 'react';

export default function BookList({ api, onEditBook }) {
  const [books, setBooks] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    loadBooks();
  }, [page, selectedCategory]);

  const loadCategories = async () => {
    try {
      const response = await api.categoriesList();
      if (response.data.success) {
        setCategories(response.data.data || []);
      }
    } catch (err) {
      console.error('Failed to load categories:', err);
    }
  };

  const loadBooks = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await api.booksList({
        page,
        pageSize,
        ...(selectedCategory && { categoryId: parseInt(selectedCategory) })
      });

      if (response.data) {
        setBooks(response.data.data || []);
        setTotalPages(response.data.totalPages || 1);
      }
    } catch (err) {
      setError(err.message || 'Failed to load books');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      loadBooks();
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const response = await api.booksSearchList({ query: searchQuery });

      if (response.data.success) {
        setBooks(response.data.data || []);
        setTotalPages(1); // Search doesn't support pagination
      }
    } catch (err) {
      setError(err.message || 'Search failed');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Are you sure you want to delete this book?')) return;

    try {
      await api.booksDelete(id);
      loadBooks();
    } catch (err) {
      alert('Failed to delete book: ' + err.message);
    }
  };

  if (loading && books.length === 0) {
    return <div className="loading">Loading books...</div>;
  }

  return (
    <div className="book-list">
      <div className="filters">
        <form onSubmit={handleSearch} className="search-form">
          <input
            type="text"
            placeholder="Search books by title..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          <button type="submit">Search</button>
          {searchQuery && (
            <button type="button" onClick={() => { setSearchQuery(''); loadBooks(); }}>
              Clear
            </button>
          )}
        </form>

        <select
          value={selectedCategory}
          onChange={(e) => {
            setSelectedCategory(e.target.value);
            setPage(1);
          }}
        >
          <option value="">All Categories</option>
          {categories.map(cat => (
            <option key={cat.id} value={cat.id}>{cat.name}</option>
          ))}
        </select>
      </div>

      {error && <div className="error">{error}</div>}

      <div className="books-grid">
        {books.map(book => (
          <div key={book.id} className="book-card">
            <h3>{book.title}</h3>
            {book.subtitle && <p className="subtitle">{book.subtitle}</p>}
            <div className="book-details">
              <p><strong>ISBN:</strong> {book.isbn}</p>
              <p><strong>Category:</strong> {book.categoryName}</p>
              <p><strong>Publisher:</strong> {book.publisher || 'N/A'}</p>
              <p><strong>Copies:</strong> {book.availableCopies}/{book.totalCopies} available</p>
              {book.shelfLocation && <p><strong>Location:</strong> {book.shelfLocation}</p>}
            </div>
            <div className="book-actions">
              <button onClick={() => onEditBook(book)}>Edit</button>
              <button onClick={() => handleDelete(book.id)} className="delete-btn">Delete</button>
            </div>
          </div>
        ))}
      </div>

      {!searchQuery && totalPages > 1 && (
        <div className="pagination">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
          >
            Previous
          </button>
          <span>Page {page} of {totalPages}</span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
