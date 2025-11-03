# МЕТОДИЧНІ РЕКОМЕНДАЦІЇ
## до лабораторної роботи
# "ІНТЕГРАЦІЯ SQL SERVER З .NET ДОДАТКАМИ ЧЕРЕЗ ENTITY FRAMEWORK CORE"
### .NET 9 + SQL Server + EF Core

---

## ЗМІСТ

1. [Практичні завдання](#2-практичні-завдання)
2. [Методичні рекомендації](#3-методичні-рекомендації)
3. [Формат здачі та загальні вимоги](#4-формат-здачі-та-загальні-вимоги)

---
**Тема:** Інтеграція SQL Server з .NET додатками через Entity Framework Core

**Мета:** Освоєння практичних навичок інтеграції баз даних SQL Server з .NET додатками через Object-Relational Mapping (ORM) фреймворк Entity Framework Core. Розуміння принципів роботи з базами даних через абстракцію ORM, використання LINQ для запитів, механізму відстеження змін (change tracking), та паттернів роботи з DbContext.

Після виконання лабораторної роботи студент зможе:

- Генерувати DbContext та entity класи з існуючої бази даних через scaffolding
- Налаштовувати підключення до SQL Server через connection strings
- Виконувати CRUD операції через DbContext API та LINQ запити
- Використовувати Include/ThenInclude для eager loading зв'язаних даних
- Працювати з механізмом change tracking та розуміти його вплив на продуктивність
- Виконувати складні запити через LINQ: фільтрація, сортування, пагінація, групування, проекції
- Використовувати AsNoTracking для оптимізації read-only операцій
- Працювати з транзакціями в EF Core (implicit та explicit)
- Інтегрувати EF Core з існуючими ADO.NET транзакціями
- Обробляти винятки DbUpdateException та DbUpdateConcurrencyException
- Оптимізувати продуктивність через compiled queries та query splitting
- Використовувати raw SQL запити через FromSqlRaw для складних сценаріїв
- Викликати stored procedures з EF Core
- Реалізовувати advanced patterns: global query filters, value converters
- Порівнювати продуктивність різних підходів через benchmarking

---

## 1. ПРАКТИЧНІ ЗАВДАННЯ

### Завдання 1: Database Scaffolding та налаштування DbContext (15 балів)

**Опис:**
Генерація DbContext та entity класів з існуючої бази даних через reverse engineering (scaffolding). Розуміння структури згенерованого коду та способів його розширення.

**Передумови:**
Студент має завершену лабораторну роботу з ADO.NET з виконаними міграціями бази даних (мінімум 3-5 таблиць з відносинами).

**Вимоги до виконання:**

1. Встановити необхідні NuGet пакети:
   - Microsoft.EntityFrameworkCore.SqlServer
   - Microsoft.EntityFrameworkCore.Tools (для команд scaffolding)

2. Виконати scaffolding існуючої бази даних:
   ```bash
   dotnet ef dbcontext scaffold "ConnectionString" Microsoft.Data.SqlClient \
       --output-dir EFModels \
       --context-dir . \
       --context LibraryDbContext \
       --data-annotations \
       --force
   ```

3. Проаналізувати згенерований код:
   - Структуру DbContext класу (DbSet властивості, OnConfiguring, OnModelCreating)
   - Entity класи (властивості, навігаційні властивості)
   - Конфігурацію відносин через Fluent API
   - Як представлені первинні ключі, зовнішні ключі, індекси

4. Створити partial class для DbContext для додавання власної логіки:
   - Додати власні методи або перевизначити OnConfiguring

6. Створити repository pattern поверх згенерованих entities

**Критерії оцінювання:**

- Правильне виконання scaffolding з коректними параметрами (3 бали)
- Згенеровані entity класи правильно відображають структуру БД (4 бали)
- Винесення connection string в User Secrets, налаштування DbContext (4 бали)
- Розуміння згенерованого коду (OnModelCreating, navigation properties) (4 бали)

**Документи для вивчення:**
- docs/28-scaffolding-and-query-builders.md - детальна інформація про scaffolding
- docs/01-project-setup.md - налаштування User Secrets

---

### Завдання 2: DbContext та базові CRUD операції через LINQ (25 балів)

**Опис:**
Реалізація паттерну Repository для доступу до даних з використанням DbContext API та LINQ запитів. Розуміння механізму change tracking та його впливу на операції.

**Вимоги до виконання:**

1. Створити інтерфейс репозиторію для основної сутності предметної області

2. Реалізувати інтерфейс з наступними методами:
   - `CreateAsync` - додавання нової сутності через `context.Set<T>().Add()`, виклик `SaveChangesAsync()`
   - `GetByIdAsync` - отримання за ID через `FindAsync()` або `FirstOrDefaultAsync()`
   - `GetAllAsync` - отримання всіх записів через `ToListAsync()`
   - `UpdateAsync` - оновлення існуючої сутності через change tracking
   - `DeleteAsync` - видалення через `Remove()` та `SaveChangesAsync()`

3. Реалізувати методи з правильним використанням change tracking:
   - Розуміти коли entity знаходиться в tracked стані
   - Коли потрібно викликати `Update()` (для detached entities)
   - Коли EF Core автоматично відстежує зміни

4. Використовувати `AsNoTracking()` для read-only операцій:
   - Метод `GetAllAsync` з параметром `trackChanges = false`
   - Розуміти переваги AsNoTracking для продуктивності

5. Всі методи мають бути асинхронними з підтримкою CancellationToken

6. Правильно обробляти null значення (nullable reference types)

7. Створити демонстраційний код який показує:
   - Створення нового запису
   - Читання за ID
   - Оновлення існуючого запису (два способи: tracked та detached)
   - Видалення запису

**Приклад коректної реалізації Update:**

```csharp
// Спосіб 1: Tracked entity (automatic change detection)
public async Task UpdateAsync(Book book, CancellationToken ct = default)
{
    var existing = await _context.Books.FindAsync(new object[] { book.Id }, ct);
    if (existing == null) throw new NotFoundException();

    // EF Core відстежує зміни
    existing.Title = book.Title;
    existing.Price = book.Price;

    await _context.SaveChangesAsync(ct);
}

// Спосіб 2: Detached entity (manual tracking)
public async Task UpdateAsync(Book book, CancellationToken ct = default)
{
    _context.Books.Update(book); // Attach and mark as modified
    await _context.SaveChangesAsync(ct);
}
```

**Критерії оцінювання:**

- Правильна архітектура: інтерфейс + реалізація (4 бали)
- Всі п'ять CRUD методів реалізовані коректно з використанням DbContext API (8 балів)
- Правильне розуміння change tracking (tracked vs detached entities) (6 балів)
- Використання AsNoTracking для read-only операцій (3 бали)
- Async/await з CancellationToken (4 бали)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секції: DbContext Lifecycle, Change Tracking, SaveChanges Internals

---

### Завдання 3: Складні LINQ запити та eager loading (10 балів)

**Опис:**
Розширення репозиторію методами для складних запитів з використанням LINQ операторів, eager loading зв'язаних даних, проекцій, групування та пагінації.

**Вимоги до виконання:**

1. Реалізувати метод з eager loading зв'язаних даних:
   - Використати `Include()` для завантаження зв'язаних сутностей одним запитом
   - Використати `ThenInclude()` для завантаження вкладених зв'язків
   - Показати різницу між eager loading та lazy loading (N+1 проблема)
   - Приклад: завантажити книги разом з авторами та категоріями

2. Реалізувати метод з проекцією в DTO:
   - Використати `Select()` для вибору тільки потрібних полів
   - Створити DTO клас для результату
   - Показати переваги проекції (менше даних, автоматичний AsNoTracking)

3. Реалізувати метод з пагінацією:
   - `GetPagedAsync(int pageNumber, int pageSize, string sortBy, string sortDirection)`
   - Використати `Skip()` та `Take()` для пагінації
   - Динамічне сортування через параметр sortBy
   - Повернути як дані, так і загальну кількість записів

4. Реалізувати метод з груруванням:
   - Використати `GroupBy()` для групування записів
   - Виконати агрегацію (Count, Sum, Average, Max, Min)
   - Приклад: кількість книг по категоріях, середня ціна по авторах

5. Реалізувати метод з комплексною фільтрацією:
   - Динамічне формування WHERE умов на основі параметрів
   - Використання `Where()` з предикатами
   - Комбінування кількох умов через `&&` або `||`

**Приклад N+1 проблеми:**

```csharp
// ПОГАНО: N+1 запитів (1 запит для книг + N запитів для авторів)
var books = await _context.Books.ToListAsync();
foreach (var book in books)
{
    // Кожна ітерація робить окремий запит до БД!
    var author = book.Author.Name;
}

// ДОБРЕ: 1 запит з JOIN
var books = await _context.Books
    .Include(b => b.Author)
    .ToListAsync();
```

**Критерії оцінювання:**

- Правильне використання Include/ThenInclude, розуміння N+1 проблеми (4 бали)
- Проекції через Select для оптимізації запитів (2 бали)
- Пагінація з Skip/Take та динамічним сортуванням (2 бали)
- GroupBy з агрегацією (2 бали)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секції: Advanced Query Patterns, Include and Eager Loading, Projections, Performance Optimization

---

### Завдання 4: Транзакції та SaveChanges (20 балів)

**Опис:**
Розуміння механізму транзакцій в EF Core. Демонстрація неявних транзакцій (automatic), явних транзакцій (explicit), та інтеграції з зовнішніми ADO.NET транзакціями.

**Вимоги до виконання:**

1. **Продемонструвати неявні транзакції (implicit):**
   - Показати що `SaveChangesAsync()` автоматично виконується в транзакції
   - Виконати кілька операцій Add/Update/Remove
   - Викликати `SaveChangesAsync()` один раз - всі зміни застосуються атомарно
   - Якщо виникне помилка - жодна зміна не застосується

2. **Реалізувати явну транзакцію (explicit) через BeginTransaction:**
   - Використати `context.Database.BeginTransactionAsync()`
   - Виконати кілька незалежних `SaveChangesAsync()` в межах однієї транзакції
   - При успіху викликати `CommitAsync()`
   - При помилці викликати `RollbackAsync()`
   - Показати use case коли це потрібно

3. **Реалізувати інтеграцію з зовнішньою ADO.NET транзакцією:**
   - Створити SqlConnection та SqlTransaction через ADO.NET
   - Прив'язати EF Core DbContext до існуючої транзакції через `UseTransaction()`
   - Виконати операції як через ADO.NET (SqlCommand), так і через EF Core
   - Показати що обидва підходи працюють в одній транзакції
   - Commit або Rollback через ADO.NET транзакцію

4. **Обробка винятків:**
   - `DbUpdateException` - порушення constraints, конфлікти
   - `DbUpdateConcurrencyException` - concurrency conflicts
   - Правильна обробка через try-catch блоки

**Приклад неявної транзакції:**

```csharp
// SaveChangesAsync автоматично створює транзакцію
public async Task TransferBooksBetweenCategories(int fromCategoryId, int toCategoryId)
{
    var books = await _context.Books
        .Where(b => b.CategoryId == fromCategoryId)
        .ToListAsync();

    foreach (var book in books)
    {
        book.CategoryId = toCategoryId; // Change tracking
    }

    // Всі зміни застосовуються атомарно в одній транзакції
    // Якщо помилка - жодна книга не переміститься
    await _context.SaveChangesAsync();
}
```

**Приклад явної транзакції:**

```csharp
public async Task ComplexBusinessOperation()
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // Перша операція
        var book = new Book { Title = "New Book" };
        _context.Books.Add(book);
        await _context.SaveChangesAsync(); // Зберігає, але не комітить

        // Друга операція (залежить від першої)
        var review = new Review { BookId = book.Id, Rating = 5 };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(); // Зберігає, але не комітить

        await transaction.CommitAsync(); // Обидві операції комітяться разом
    }
    catch
    {
        await transaction.RollbackAsync(); // Обидві операції скасовуються
        throw;
    }
}
```

**Приклад інтеграції з ADO.NET транзакцією:**

```csharp
public async Task MixedAdoNetAndEfCoreOperation()
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    await using var transaction = connection.BeginTransaction();

    try
    {
        // ADO.NET операція
        await using var cmd = new SqlCommand(
            "UPDATE Books SET Price = Price * 1.1 WHERE CategoryId = @categoryId",
            connection,
            transaction);
        cmd.Parameters.AddWithValue("@categoryId", 1);
        await cmd.ExecuteNonQueryAsync();

        // EF Core операція в тій самій транзакції
        await _context.Database.UseTransactionAsync(transaction);
        var category = await _context.Categories.FindAsync(1);
        category.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Критерії оцінювання:**

- Демонстрація неявних транзакцій через SaveChangesAsync (5 балів)
- Правильна реалізація явної транзакції з BeginTransaction/Commit/Rollback (8 балів)
- Інтеграція з ADO.NET транзакцією через UseTransaction (5 балів)
- Обробка DbUpdateException та DbUpdateConcurrencyException (2 бали)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секція Transaction Management
- docs/14-transactions.md - загальна інформація про транзакції

---

### Завдання 5: На вибір студента (15 балів)

**Студент обирає ОДИН з двох варіантів:**

#### Варіант A: Compiled Queries та оптимізація продуктивності

**Опис:** Дослідження впливу різних оптимізаційних технік EF Core на продуктивність запитів.

**Вимоги:**

1. **Compiled Queries:**
   - Створити compiled query через `EF.CompileAsyncQuery`
   - Показати переваги для "hot path" запитів які виконуються часто
   - Порівняти час виконання: звичайний LINQ vs compiled query
   - Виміряти через Stopwatch або BenchmarkDotNet

2. **AsNoTracking порівняння:**
   - Створити запит з tracking (за замовчуванням)
   - Той самий запит з AsNoTracking
   - Виміряти час виконання та споживання пам'яті
   - Пояснити різницю

3. **Query Splitting:**
   - Створити запит з Include кількох колекцій
   - Виконати як single query (за замовчуванням)
   - Виконати як split query через `AsSplitQuery()`
   - Проаналізувати згенеровані SQL запити (через logging або SQL Profiler)
   - Пояснити коли використовувати кожен підхід

4. **Benchmarking:**
   - Використати BenchmarkDotNet для точних вимірювань
   - Створити benchmark класи для різних підходів
   - Згенерувати звіт з результатами
   - Створити порівняльну таблицю

5. **Висновки:**
   - Коли використовувати compiled queries
   - Коли використовувати AsNoTracking
   - Trade-offs між single query та split query
   - Рекомендації для практичного застосування

**Приклад compiled query:**

```csharp
private static readonly Func<LibraryDbContext, int, Task<Book?>> _getBookById =
    EF.CompileAsyncQuery((LibraryDbContext context, int id) =>
        context.Books
            .Include(b => b.Author)
            .FirstOrDefault(b => b.Id == id));

public async Task<Book?> GetByIdOptimized(int id)
{
    return await _getBookById(_context, id);
}
```

**Критерії оцінювання:**

- Реалізація compiled queries з вимірюванням продуктивності (5 балів)
- Порівняння AsNoTracking vs tracking (3 бали)
- Query splitting аналіз (single vs split) (4 бали)
- Використання BenchmarkDotNet та аналіз результатів (3 бали)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секція Performance Optimization: Compiled Queries, AsNoTracking, Query Splitting
- Приклади у src/DbDemo.Infrastructure.EFCore/Repositories/BookRepository.cs

---

#### Варіант B: Raw SQL та stored procedures

**Опис:** Використання raw SQL запитів та виклик stored procedures з EF Core для складних сценаріїв.

**Вимоги:**

1. **FromSqlRaw для SELECT запитів:**
   - Використати `FromSqlRaw()` для виконання raw SQL SELECT
   - Показати як результати мапляться на entity класи
   - Використати параметризовані запити через `FromSqlRaw("SELECT * FROM Books WHERE Price > {0}", price)`
   - Комбінувати з LINQ (Where, OrderBy після FromSqlRaw)

2. **ExecuteSqlRaw для команд:**
   - Використати `ExecuteSqlRaw()` для INSERT/UPDATE/DELETE
   - Показати як повертається кількість змінених рядків
   - Параметризовані запити через `ExecuteSqlRaw("UPDATE Books SET Price = {0} WHERE Id = {1}", price, id)`

3. **Stored Procedures:**
   - Створити stored procedure в SQL Server (з INPUT та OUTPUT параметрами)
   - Викликати через `FromSqlRaw` для процедур що повертають result set
   - Викликати через `ExecuteSqlRaw` для процедур що виконують команди
   - Обробити OUTPUT параметри

4. **Table-Valued Functions:**
   - Створити table-valued function в SQL Server
   - Викликати через `FromSqlRaw`
   - Показати як результати мапляться на entities

5. **Комбінування з LINQ:**
   - Показати що після `FromSqlRaw` можна використовувати LINQ
   - Приклад: `context.Books.FromSqlRaw("...").Where(b => b.Price > 100).OrderBy(b => b.Title)`
   - Розуміти які операції виконуються на SQL сервері, а які в пам'яті

**Приклад FromSqlRaw:**

```csharp
// Простий запит
var books = await _context.Books
    .FromSqlRaw("SELECT * FROM Books WHERE Price > {0}", minPrice)
    .ToListAsync();

// Комбінування з LINQ
var expensiveBooks = await _context.Books
    .FromSqlRaw("SELECT * FROM Books WHERE CategoryId = {0}", categoryId)
    .Where(b => b.Price > 100)  // Додається до SQL запиту
    .OrderBy(b => b.Title)      // Додається до SQL запиту
    .ToListAsync();
```

**Приклад stored procedure:**

```sql
CREATE PROCEDURE GetBooksByPriceRange
    @MinPrice DECIMAL(10,2),
    @MaxPrice DECIMAL(10,2),
    @TotalCount INT OUTPUT
AS
BEGIN
    SELECT @TotalCount = COUNT(*) FROM Books WHERE Price BETWEEN @MinPrice AND @MaxPrice;
    SELECT * FROM Books WHERE Price BETWEEN @MinPrice AND @MaxPrice;
END
```

```csharp
// Виклик з C#
var minPrice = new SqlParameter("@MinPrice", 10);
var maxPrice = new SqlParameter("@MaxPrice", 50);
var totalCount = new SqlParameter
{
    ParameterName = "@TotalCount",
    SqlDbType = SqlDbType.Int,
    Direction = ParameterDirection.Output
};

var books = await _context.Books
    .FromSqlRaw("EXEC GetBooksByPriceRange @MinPrice, @MaxPrice, @TotalCount OUTPUT",
        minPrice, maxPrice, totalCount)
    .ToListAsync();

var count = (int)totalCount.Value;
```

**Критерії оцінювання:**

- FromSqlRaw для SELECT з параметрами та комбінуванням з LINQ (4 бали)
- ExecuteSqlRaw для команд (3 бали)
- Створення та виклик stored procedure з OUTPUT параметрами (5 балів)
- Table-valued functions або розширений сценарій (3 бали)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секція Raw SQL Queries
- src/DbDemo.Infrastructure.EFCore/Repositories/MemberRepository.cs - приклади використання

---

### Завдання 6: На вибір студента (15 балів)

**Студент обирає ОДИН з двох варіантів:**

#### Варіант A: Global Query Filters та Value Converters

**Опис:** Реалізація advanced patterns для обробки soft delete та перетворення даних на рівні ORM.

**Вимоги:**

1. **Global Query Filters для Soft Delete:**
   - Додати поле `IsDeleted` (або `DeletedAt`) до entity класу
   - Налаштувати global query filter в OnModelCreating:
     ```csharp
     modelBuilder.Entity<Book>().HasQueryFilter(b => !b.IsDeleted);
     ```
   - Показати що всі запити автоматично фільтрують видалені записи
   - Реалізувати метод "видалення" який встановлює `IsDeleted = true`
   - Показати як ігнорувати фільтр через `IgnoreQueryFilters()`

2. **Value Converters:**
   - Створити value converter для зберігання enum як string (замість int)
   - Створити value converter для шифрування/дешифрування чутливих даних
   - Створити value converter для JSON серіалізації складних об'єктів
   - Налаштувати через Fluent API в OnModelCreating

3. **Owned Entities (Value Objects):**
   - Створити value object (наприклад, Address з Street, City, ZipCode)
   - Налаштувати як owned entity через `OwnsOne()`
   - Показати як EF Core зберігає owned entities в тій самій таблиці

4. **Demonstration:**
   - Показати роботу soft delete (видалення та відновлення)
   - Показати роботу value converters (збереження та читання)
   - Показати роботу owned entities (збереження та читання)

**Приклад Global Query Filter:**

```csharp
// Entity
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool IsDeleted { get; set; }
}

// Configuration
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Book>()
        .HasQueryFilter(b => !b.IsDeleted);
}

// Usage
var books = await _context.Books.ToListAsync(); // Автоматично фільтрує IsDeleted = true

var allBooks = await _context.Books
    .IgnoreQueryFilters() // Включити видалені
    .ToListAsync();
```

**Приклад Value Converter:**

```csharp
// Enum
public enum BookStatus { Available, CheckedOut, Reserved, Lost }

// Configuration
modelBuilder.Entity<Book>()
    .Property(b => b.Status)
    .HasConversion<string>(); // Зберігається як "Available", "CheckedOut" etc.

// JSON converter
modelBuilder.Entity<Book>()
    .Property(b => b.Metadata)
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
        v => JsonSerializer.Deserialize<BookMetadata>(v, (JsonSerializerOptions)null));
```

**Приклад Owned Entity:**

```csharp
// Value object
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

// Entity
public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Address { get; set; } // Owned
}

// Configuration
modelBuilder.Entity<Publisher>()
    .OwnsOne(p => p.Address);

// Результат в БД: таблиця Publishers з колонками Address_Street, Address_City, Address_ZipCode
```

**Критерії оцінювання:**

- Global query filters для soft delete з демонстрацією (6 балів)
- Value converters (мінімум 2 різних типи) (5 балів)
- Owned entities (value objects) (4 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції Advanced Patterns: Global Query Filters, Value Converters, Owned Entities
- src/DbDemo.Infrastructure.EFCore.CodeFirst/ - приклади реалізації

---

#### Варіант B: Spatial Data Queries

**Опис:** Робота з просторовими даними (geography/geometry) в EF Core для геолокаційних запитів.

**Передумова:** База даних містить таблиці з просторовими даними (Geography або Geometry типи).

**Вимоги:**

1. **Налаштування Spatial Support:**
   - Додати пакет Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite
   - Налаштувати DbContext для використання NetTopologySuite

2. **Entity з Spatial Properties:**
   - Створити entity з властивістю типу `Point` (координати)
   - Налаштувати через Fluent API тип колонки (geography або geometry)

3. **Spatial Queries:**
   - Знайти записи в радіусі N метрів від точки
   - Знайти найближчі записи до заданої точки (сортування за відстанню)
   - Обчислити відстань між двома точками
   - Перевірити чи точка знаходиться в межах полігону

4. **Performance:**
   - Створити spatial index для оптимізації
   - Виміряти продуктивність запитів з індексом та без

**Приклад:**

```csharp
// Entity
public class Library
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Point Location { get; set; } // NetTopologySuite.Geometries.Point
}

// Configuration
modelBuilder.Entity<Library>()
    .Property(l => l.Location)
    .HasColumnType("geography");

// Queries
var userLocation = new Point(30.5234, 50.4501) { SRID = 4326 };

// Знайти бібліотеки в радіусі 5 км
var nearbyLibraries = await _context.Libraries
    .Where(l => l.Location.Distance(userLocation) <= 5000) // метри
    .ToListAsync();

// Сортування за відстанню
var closest = await _context.Libraries
    .OrderBy(l => l.Location.Distance(userLocation))
    .Take(5)
    .ToListAsync();
```

**Критерії оцінювання:**

- Налаштування spatial support та entity configuration (4 бали)
- Реалізація spatial queries (distance, within radius) (6 балів)
- Spatial index та аналіз продуктивності (5 балів)

**Документи для вивчення:**
- docs/29-ef-core-orm.md - секція про spatial data
- Microsoft Docs: Spatial Data in EF Core

---

## 2. МЕТОДИЧНІ РЕКОМЕНДАЦІЇ

### 2.1. Що таке ORM та Entity Framework Core?

**Object-Relational Mapping (ORM)** - це техніка програмування що дозволяє працювати з базою даних через об'єктно-орієнтований підхід. ORM автоматично перетворює (мапить) об'єкти C# класів в рядки таблиць бази даних і навпаки.

**Entity Framework Core** - це сучасний, легкий, розширюваний та кросплатформний ORM фреймворк від Microsoft для .NET. Це повне переписування попередньої версії Entity Framework з фокусом на продуктивність та модульність.

**Основні переваги EF Core:**

- **Продуктивність розробки** - менше коду для CRUD операцій
- **LINQ запити** - строго типізовані запити на C# замість SQL рядків
- **Change Tracking** - автоматичне відстеження змін об'єктів
- **Міграції** - версіонування схеми бази даних через код
- **Абстракція БД** - легше перейти на іншу СУБД
- **Безпека** - автоматична параметризація запитів (захист від SQL injection)

**Недоліки EF Core:**

- **Продуктивність** - overhead порівняно з ADO.NET (10-30%)
- **Складність налагодження** - генерований SQL може бути неоптимальним
- **Обмеження** - деякі складні SQL запити важко або неможливо виразити через LINQ
- **Розмір** - додаткові NuGet пакети збільшують розмір додатка
- **Крива навчання** - потрібно розуміти як працює ORM, не тільки SQL

**Коли використовувати EF Core:**

- Більшість бізнес-додатків де зручність розробки важливіша за максимальну продуктивність
- CRUD-heavy додатки
- Проєкти де потрібна підтримка кількох СУБД
- Rapid prototyping та MVP
- Команди де не всі розробники знають SQL добре
- Важливо аби додатки були малими, бо здорові додатки з купою таблиць неодмінно зустрінуться з проблемою і впруться в продуктивність запитів EF Core

**Коли використовувати ADO.NET:**

- Критичні за продуктивністю операції (high-throughput системи)
- Складні аналітичні запити
- Bulk операції (масове завантаження даних)
- Legacy системи
- Мікросервіси де потрібен мінімальний розмір

### 2.2. Database-First vs Code-First

Існують два основні підходи до роботи з EF Core:

**Database-First (Scaffolding):**
- База даних вже існує (створена через SQL скрипти або міграції ADO.NET)
- Генеруються C# класи з існуючої схеми через `dotnet ef dbcontext scaffold`
- Підходить для legacy баз даних або коли DBA керує схемою
- **Ця лабораторна робота використовує Database-First підхід**

**Code-First:**
- C# класи є джерелом істини (source of truth)
- База даних генерується з C# класів через міграції
- Підходить для нових проєктів або greenfield розробки
- Більш популярний в сучасній розробці

**Порівняння:**

| Аспект | Database-First | Code-First |
|--------|----------------|------------|
| Джерело істини | База даних (SQL) | C# класи |
| Workflow | SQL → C# | C# → SQL |
| Зміна схеми | Через SQL, потім re-scaffold | Через C# classes, потім міграція |
| Контроль над SQL | Повний | Частковий (через міграції) |
| Підходить для | Legacy БД, DBA-managed | Нові проєкти, dev-managed |

### 2.3. DbContext - серце EF Core

`DbContext` - це головний клас для взаємодії з базою даних в EF Core. Він представляє сесію роботи з БД і відповідає за:

**Основні відповідальності:**

1. **Connection Management** - управління з'єднанням з БД
2. **Change Tracking** - відстеження змін в entities
3. **Querying** - виконання LINQ запитів та їх перетворення в SQL
4. **Saving** - збереження змін в БД через `SaveChanges()`
5. **Caching** - кешування entities (first-level cache)
6. **Transaction Management** - управління транзакціями

**Lifecycle DbContext:**

```
Create → Open Connection → Query/Modify → SaveChanges → Dispose
```

**DbSet<T> Properties:**

DbContext містить `DbSet<T>` властивості для кожної таблиці:

```csharp
public class LibraryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
}
```

`DbSet<T>` - це колекція-подібний інтерфейс для виконання LINQ запитів.

**Важливо:**
- DbContext НЕ є thread-safe - кожен потік повинен мати свій екземпляр
- DbContext має короткий lifecycle - створюється для операції, потім dispose
- Використовувати `await using` для автоматичного Dispose

### 2.4. LINQ - Language Integrated Query

LINQ дозволяє писати запити до бази даних на C# замість SQL. EF Core перекладає LINQ вирази в SQL запити.

**Основні LINQ оператори:**

```csharp
// WHERE - фільтрація
var books = await context.Books
    .Where(b => b.Price > 100)
    .ToListAsync();
// SQL: SELECT * FROM Books WHERE Price > 100

// SELECT - проекція
var titles = await context.Books
    .Select(b => b.Title)
    .ToListAsync();
// SQL: SELECT Title FROM Books

// ORDER BY - сортування
var sorted = await context.Books
    .OrderBy(b => b.Title)
    .ThenByDescending(b => b.Price)
    .ToListAsync();
// SQL: SELECT * FROM Books ORDER BY Title ASC, Price DESC

// SKIP / TAKE - пагінація
var page = await context.Books
    .Skip(20)
    .Take(10)
    .ToListAsync();
// SQL: SELECT * FROM Books OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY

// JOIN через Include
var books = await context.Books
    .Include(b => b.Author)
    .ToListAsync();
// SQL: SELECT * FROM Books LEFT JOIN Authors ON Books.AuthorId = Authors.Id

// GROUP BY - групування
var stats = await context.Books
    .GroupBy(b => b.CategoryId)
    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
    .ToListAsync();
// SQL: SELECT CategoryId, COUNT(*) FROM Books GROUP BY CategoryId
```

**Expression Trees:**

LINQ вирази компілюються в expression trees - структури даних що представляють код. EF Core аналізує ці дерева і генерує SQL.

**Важливо:**
- Не всі C# методи можна перекласти в SQL
- Виклик `.ToList()` або `.ToListAsync()` виконує запит (materialization)
- До materialization можна додавати необмежену кількість LINQ операторів

### 2.5. Change Tracking - відстеження змін

Change Tracking - це механізм EF Core що автоматично відстежує зміни в entities та генерує відповідні UPDATE/DELETE SQL команди.

**Як працює:**

1. Коли entity завантажується з БД, EF Core зберігає snapshot поточного стану
2. Коли властивості entity змінюються, EF Core це відстежує
3. При виклику `SaveChanges()`, EF Core порівнює поточний стан з snapshot
4. Генеруються UPDATE SQL команди тільки для змінених полів

**Entity States:**

- **Added** - нова entity, буде INSERT при SaveChanges
- **Modified** - існуюча entity зі зміненими полями, буде UPDATE
- **Deleted** - entity позначена для видалення, буде DELETE
- **Unchanged** - entity без змін, нічого не відбудеться
- **Detached** - entity не відстежується

**Tracked vs Detached:**

```csharp
// Tracked - EF Core відстежує зміни
var book = await context.Books.FindAsync(1);
book.Price = 29.99m; // Автоматично відстежується
await context.SaveChangesAsync(); // UPDATE генерується автоматично

// Detached - потрібно manually attach
var book = new Book { Id = 1, Price = 29.99m };
context.Books.Update(book); // Attach and mark as Modified
await context.SaveChangesAsync();
```

**AsNoTracking для продуктивності:**

Якщо дані тільки для читання (не будуть змінюватись), використовуйте `AsNoTracking()`:

```csharp
var books = await context.Books
    .AsNoTracking() // Вимкнути change tracking
    .ToListAsync();
```

**Переваги AsNoTracking:**
- Швидше (не потрібно зберігати snapshot)
- Менше пам'яті (не потрібно кешувати entities)
- Підходить для read-only операцій

**Недоліки:**
- Entities не відстежуються - не можна просто змінити властивість і викликати SaveChanges

### 2.6. SaveChanges - збереження змін

`SaveChanges()` / `SaveChangesAsync()` - це метод що зберігає всі відстежені зміни в базу даних.

**Що відбувається всередині SaveChanges:**

1. **DetectChanges** - сканує всі tracked entities та виявляє зміни
2. **Validation** - перевіряє data annotations та validation attributes
3. **Transaction Begin** - автоматично відкривається транзакція
4. **Generate SQL** - генеруються INSERT/UPDATE/DELETE команди
5. **Execute SQL** - виконуються SQL команди
6. **Transaction Commit** - транзакція комітиться (або rollback при помилці)
7. **Update State** - стани entities оновлюються (Modified → Unchanged)

**Важливо:**

- SaveChanges виконується в транзакції - всі зміни атомарні
- Якщо одна операція падає - всі скасовуються
- SaveChanges може бути дорогою операцією - краще батчити зміни

**Порівняння з ADO.NET:**

```csharp
// ADO.NET - явні INSERT/UPDATE/DELETE команди
await cmd.ExecuteNonQueryAsync();

// EF Core - просто змінюємо об'єкти, SaveChanges генерує SQL
book.Price = 29.99m;
await context.SaveChangesAsync();
```

### 2.7. Include та Eager Loading

**Проблема N+1:**

```csharp
// ПОГАНО: N+1 запитів
var books = await context.Books.ToListAsync(); // 1 запит
foreach (var book in books)
{
    // Кожна ітерація = +1 запит до БД!
    Console.WriteLine(book.Author.Name); // Lazy loading
}
// Якщо 100 книг = 101 запит до БД!
```

**Рішення - Eager Loading через Include:**

```csharp
// ДОБРЕ: 1 запит з JOIN
var books = await context.Books
    .Include(b => b.Author) // LEFT JOIN з таблицею Authors
    .ToListAsync();
// 1 запит до БД з JOIN
```

**ThenInclude для вкладених зв'язків:**

```csharp
var books = await context.Books
    .Include(b => b.Author)
        .ThenInclude(a => a.Country) // Author.Country
    .Include(b => b.Reviews) // Колекція
        .ThenInclude(r => r.Member) // Review.Member
    .ToListAsync();
```

**Альтернативи:**

1. **Explicit Loading** - ручне завантаження зв'язків:
   ```csharp
   var book = await context.Books.FindAsync(1);
   await context.Entry(book).Reference(b => b.Author).LoadAsync();
   ```

2. **Projection (Select)** - вибір тільки потрібних полів:
   ```csharp
   var data = await context.Books
       .Select(b => new { b.Title, AuthorName = b.Author.Name })
       .ToListAsync();
   ```

### 2.8. Транзакції в EF Core

**Три типи транзакцій:**

1. **Implicit (Automatic):**
   - `SaveChanges()` автоматично виконується в транзакції
   - Найпростіший підхід для більшості випадків

2. **Explicit (Manual):**
   - `BeginTransaction()` для явного контролю
   - Потрібно коли кілька `SaveChanges()` мають бути атомарними

3. **External (ADO.NET Integration):**
   - `UseTransaction()` для інтеграції з існуючою ADO.NET транзакцією
   - Потрібно коли змішується EF Core та ADO.NET код

**Детальна інформація:** Див. Завдання 4

### 2.9. Raw SQL в EF Core

Іноді LINQ недостатньо для складних запитів. EF Core дозволяє виконувати raw SQL:

**FromSqlRaw для SELECT:**
```csharp
var books = await context.Books
    .FromSqlRaw("SELECT * FROM Books WHERE Price > {0}", minPrice)
    .ToListAsync();
```

**ExecuteSqlRaw для команд:**
```csharp
var affected = await context.Database
    .ExecuteSqlRawAsync("UPDATE Books SET Price = Price * 1.1 WHERE CategoryId = {0}", categoryId);
```

**Переваги:**
- Можна використати складні SQL конструкції
- Підвищена продуктивність для специфічних запитів
- Доступ до database-specific функцій

**Недоліки:**
- Втрата абстракції БД
- Ризик SQL injection (якщо не використовувати параметри)
- Важче підтримувати

### 2.10. Best Practices

**DO:**

✓ Використовувати `await using` для DbContext
✓ Використовувати AsNoTracking для read-only запитів
✓ Використовувати Include для eager loading (уникати N+1)
✓ Використовувати проекції (Select) для DTO
✓ Використовувати compiled queries для hot paths
✓ Батчити операції перед SaveChanges
✓ Обробляти DbUpdateException
✓ Логувати генеровані SQL запити в development

**DON'T:**

✗ НЕ створювати один DbContext для всього додатка (не thread-safe)
✗ НЕ використовувати Select * (явно перераховувати поля)
✗ НЕ ігнорувати N+1 проблему
✗ НЕ використовувати ToList() передчасно (lazy evaluation)
✗ НЕ змішувати синхронні та асинхронні методи
✗ НЕ забувати про AsNoTracking для великих read-only запитів
✗ НЕ використовувати EF Core для bulk операцій (використати SqlBulkCopy)

### 2.11. Порівняння з ADO.NET

| Аспект | ADO.NET | EF Core |
|--------|---------|---------|
| Рівень абстракції | Низький | Високий |
| Кількість коду | Багато | Мало |
| Продуктивність | Максимальна | На 10-30% повільніше |
| SQL контроль | Повний | Частковий |
| Безпека | Вручну (параметри) | Автоматична |
| Підтримка СУБД | Одна СУБД | Легко змінити |
| Крива навчання | SQL потрібен | C# + LINQ достатньо |
| Change Tracking | Немає | Автоматичний |
| Міграції | Вручну | Автоматичні |
| Типові use cases | Performance-critical, bulk ops | Business apps, CRUD |

**Висновок:**
Використовуйте EF Core для 80% додатка (зручність), ADO.NET для 20% (продуктивність).

---

## 3. ФОРМАТ ЗДАЧІ ТА ЗАГАЛЬНІ ВИМОГИ

### 3.1. Структура проєкту

Проєкт має мати наступну структуру:

```
YourProjectName.EFCore/
├── EFModels/                   # Згенеровані entity класи (scaffolded)
│   ├── Book.cs
│   ├── Author.cs
│   ├── Category.cs
│   └── ...
├── Repositories/               # Репозиторії для доступу до даних
│   ├── IBookRepository.cs
│   ├── BookRepository.cs
│   └── ...
├── DTOs/                       # Data Transfer Objects для проекцій
│   └── BookWithAuthorDto.cs
├── LibraryDbContext.cs         # Згенерований DbContext
├── LibraryDbContext.Custom.cs  # Partial class з кастомізаціями
├── Program.cs                  # Точка входу, демонстрація
├── appsettings.json            # Конфігурація (БЕЗ паролів!)
├── README.md                   # Інструкції по запуску
└── REPORT.md                   # Звіт про виконану роботу
```

### 3.2. README.md

Файл README.md має містити:

1. **Назва проєкту та опис предметної області**
   - Коротко описати що робить система
   - Які сутності в базі даних

2. **Інструкції по налаштуванню**
   - Передумови (SQL Server, завершена ADO.NET лабораторна)
   - Як налаштувати connection string через User Secrets
   - Які NuGet пакети потрібні

3. **Інструкції по scaffolding**
   - Команда для scaffolding бази даних
   - Параметри які використовувались

4. **Інструкції по запуску**
   - Як запустити демонстрацію
   - Які операції виконуються

5. **Виконані завдання**
   - Список завдань 1-4 (обов'язкові)
   - Який варіант обрано для завдання 5
   - Який варіант обрано для завдання 6

### 3.3. REPORT.md

Файл REPORT.md має містити звіт про виконану роботу:

1. **Опис виконання кожного завдання**
   - Що було зроблено
   - Які складнощі виникли
   - Як вони були вирішені

2. **Скріншоти результатів виконання**
   - Результати scaffolding (згенеровані класи)
   - Результати CRUD операцій
   - Згенеровані SQL запити (через logging)
   - Результати транзакцій
   - Графіки/таблиці продуктивності (де застосовно)

3. **Згенеровані SQL запити**
   - Для складних LINQ запитів показати відповідний SQL
   - Можна використати SQL Profiler або EF Core logging

4. **Виміряні показники продуктивності**
   - Для завдання 5A (compiled queries, AsNoTracking)
   - Benchmarking результати

5. **Порівняльний аналіз** (опціонально)
   - Порівняння EF Core з ADO.NET для тієї самої операції
   - Відмінності в коді та продуктивності

6. **Висновки та спостереження**
   - Що нового дізнались
   - Які підходи виявились найефективнішими
   - Коли використовувати EF Core, а коли ADO.NET
   - Рекомендації для практичного застосування

### 3.4. Вимоги до коду

**Стиль коду:**
- Професійний стиль (БЕЗ емоджі в коді)
- Змістовні назви змінних, методів та класів (англійською)
- Консистентне форматування (рекомендується використати IDE formatter)
- XML documentation коментарі для публічних методів
- Inline коментарі для складної логіки (українською або англійською)

**Обов'язкові практики:**

- Всі LINQ запити правильно сформовані
- Використання `await using` для DbContext
- Async/await для всіх операцій з БД
- Обробка помилок через try-catch де потрібно
- Підтримка CancellationToken в асинхронних методах
- AsNoTracking для read-only операцій
- Include для eager loading (уникнення N+1)

**Чого уникати:**

- Синхронних методів (ToList, Find, SaveChanges замість async версій)
- N+1 запитів (забули Include)
- Відкритих DbContext без using
- Select * (завжди перераховувати поля в проекціях)
- Передчасного ToList() (порушення lazy evaluation)
- Зберігання паролів в appsettings.json або коді

**Налаштування EF Core Logging (для development):**

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlServer(_connectionString)
        .LogTo(Console.WriteLine, LogLevel.Information) // Виводити SQL в консоль
        .EnableSensitiveDataLogging(); // Показувати значення параметрів
}
```

Це допоможе побачити які SQL запити генерує EF Core.

### 3.5. Загальні вимоги

**Технічні вимоги:**

- Проєкт має компілюватись без помилок та попереджень
- Scaffolding має бути виконано успішно
- Демонстраційний код в Program.cs має працювати
- Connection string налаштовано через User Secrets

**Обов'язкові завдання:**

- Завдання 1: Database Scaffolding ✓
- Завдання 2: DbContext та CRUD ✓
- Завдання 3: Складні LINQ запити ✓
- Завдання 4: Транзакції ✓

**Завдання на вибір:**

- Завдання 5: Один варіант з двох (A/B) ✓
- Завдання 6: Один варіант з двох (A/B) ✓

**Документація:**

- README.md з інструкціями ✓
- REPORT.md зі звітом ✓