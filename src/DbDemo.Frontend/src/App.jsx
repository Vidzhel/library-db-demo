import { useState } from 'react';
import { Api } from './api/Api';
import BookList from './components/BookList';
import BookForm from './components/BookForm';
import './App.css';

// Initialize API client
const api = new Api({
  baseURL: 'http://localhost:5000'
});

function App() {
  const [view, setView] = useState('list'); // 'list' or 'form'
  const [selectedBook, setSelectedBook] = useState(null);

  const handleAddNew = () => {
    setSelectedBook(null);
    setView('form');
  };

  const handleEdit = (book) => {
    setSelectedBook(book);
    setView('form');
  };

  const handleSave = () => {
    setView('list');
    setSelectedBook(null);
  };

  const handleCancel = () => {
    setView('list');
    setSelectedBook(null);
  };

  return (
    <div className="app">
      <header>
        <h1>Library Management System</h1>
        <p className="subtitle">Demo Frontend for DbDemo.WebApi</p>
      </header>

      <main>
        {view === 'list' ? (
          <>
            <div className="toolbar">
              <button onClick={handleAddNew} className="primary-btn">
                Add New Book
              </button>
            </div>
            <BookList api={api.api} onEditBook={handleEdit} />
          </>
        ) : (
          <BookForm
            api={api.api}
            book={selectedBook}
            onSave={handleSave}
            onCancel={handleCancel}
          />
        )}
      </main>

      <footer>
        <p>
          This is a demo application showing integration with the DbDemo.WebApi.
          <br />
          API Endpoint: <code>http://localhost:5000</code>
        </p>
      </footer>
    </div>
  );
}

export default App;
