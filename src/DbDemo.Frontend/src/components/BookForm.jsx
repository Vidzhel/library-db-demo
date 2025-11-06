import { useState, useEffect } from 'react';

export default function BookForm({ api, book, onSave, onCancel }) {
  const [categories, setCategories] = useState([]);
  const [formData, setFormData] = useState({
    isbn: '',
    title: '',
    subtitle: '',
    description: '',
    publisher: '',
    publishedDate: '',
    pageCount: '',
    language: '',
    categoryId: '',
    totalCopies: 1,
    shelfLocation: ''
  });
  const [errors, setErrors] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadCategories();
    if (book) {
      setFormData({
        isbn: book.isbn || '',
        title: book.title || '',
        subtitle: book.subtitle || '',
        description: book.description || '',
        publisher: book.publisher || '',
        publishedDate: book.publishedDate ? book.publishedDate.split('T')[0] : '',
        pageCount: book.pageCount || '',
        language: book.language || '',
        categoryId: book.categoryId || '',
        totalCopies: book.totalCopies || 1,
        shelfLocation: book.shelfLocation || ''
      });
    }
  }, [book]);

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

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setErrors([]);

    try {
      const payload = {
        ...formData,
        categoryId: parseInt(formData.categoryId),
        pageCount: formData.pageCount ? parseInt(formData.pageCount) : null,
        totalCopies: parseInt(formData.totalCopies),
        publishedDate: formData.publishedDate || null
      };

      let response;
      if (book) {
        // Update existing book
        response = await api.booksUpdate(book.id, payload);
      } else {
        // Create new book
        response = await api.booksCreate(payload);
      }

      if (response.data.success) {
        onSave();
      } else {
        setErrors(response.data.errors || ['Operation failed']);
      }
    } catch (err) {
      if (err.response?.data?.errors) {
        setErrors(err.response.data.errors);
      } else {
        setErrors([err.message || 'Failed to save book']);
      }
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  return (
    <div className="book-form">
      <h2>{book ? 'Edit Book' : 'Add New Book'}</h2>

      {errors.length > 0 && (
        <div className="error">
          <ul>
            {errors.map((err, i) => <li key={i}>{err}</li>)}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>ISBN *</label>
          <input
            type="text"
            name="isbn"
            value={formData.isbn}
            onChange={handleChange}
            required
            disabled={!!book}
            placeholder="10 or 13 digits"
          />
        </div>

        <div className="form-group">
          <label>Title *</label>
          <input
            type="text"
            name="title"
            value={formData.title}
            onChange={handleChange}
            required
            maxLength={200}
          />
        </div>

        <div className="form-group">
          <label>Subtitle</label>
          <input
            type="text"
            name="subtitle"
            value={formData.subtitle}
            onChange={handleChange}
            maxLength={200}
          />
        </div>

        <div className="form-group">
          <label>Description</label>
          <textarea
            name="description"
            value={formData.description}
            onChange={handleChange}
            rows={4}
            maxLength={2000}
          />
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>Publisher</label>
            <input
              type="text"
              name="publisher"
              value={formData.publisher}
              onChange={handleChange}
              maxLength={100}
            />
          </div>

          <div className="form-group">
            <label>Published Date</label>
            <input
              type="date"
              name="publishedDate"
              value={formData.publishedDate}
              onChange={handleChange}
            />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>Page Count</label>
            <input
              type="number"
              name="pageCount"
              value={formData.pageCount}
              onChange={handleChange}
              min={1}
              max={10000}
            />
          </div>

          <div className="form-group">
            <label>Language</label>
            <input
              type="text"
              name="language"
              value={formData.language}
              onChange={handleChange}
              maxLength={50}
              placeholder="e.g., English"
            />
          </div>
        </div>

        <div className="form-row">
          <div className="form-group">
            <label>Category *</label>
            <select
              name="categoryId"
              value={formData.categoryId}
              onChange={handleChange}
              required
            >
              <option value="">Select a category</option>
              {categories.map(cat => (
                <option key={cat.id} value={cat.id}>{cat.name}</option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Total Copies *</label>
            <input
              type="number"
              name="totalCopies"
              value={formData.totalCopies}
              onChange={handleChange}
              required
              min={0}
              max={1000}
            />
          </div>
        </div>

        <div className="form-group">
          <label>Shelf Location</label>
          <input
            type="text"
            name="shelfLocation"
            value={formData.shelfLocation}
            onChange={handleChange}
            maxLength={50}
            placeholder="e.g., A-12-3"
          />
        </div>

        <div className="form-actions">
          <button type="submit" disabled={loading}>
            {loading ? 'Saving...' : (book ? 'Update Book' : 'Create Book')}
          </button>
          <button type="button" onClick={onCancel} disabled={loading}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
