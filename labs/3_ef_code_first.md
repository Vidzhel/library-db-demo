# МЕТОДИЧНІ РЕКОМЕНДАЦІЇ
## до лабораторної роботи
# "ІНТЕГРАЦІЯ SQL SERVER З .NET ДОДАТКАМИ ЧЕРЕЗ ENTITY FRAMEWORK CORE: CODE-FIRST ПІДХІД"
### .NET 9 + SQL Server + EF Core Code-First

---

## ЗМІСТ

1. [Практичні завдання](#1-практичні-завдання)
2. [Методичні рекомендації](#2-методичні-рекомендації)
3. [Формат здачі та загальні вимоги](#3-формат-здачі-та-загальні-вимоги)

---

**Тема:** Інтеграція SQL Server з .NET додатками через Entity Framework Core: Code-First підхід

**Мета:** Освоєння практичних навичок роботи з Entity Framework Core в режимі Code-First, де C# класи є джерелом істини (source of truth) для схеми бази даних. Розуміння процесу конфігурації entities через Fluent API, роботи з міграціями для версіонування схеми, стратегій seed даних, та реалізації advanced patterns для розширених сценаріїв.

**Передумови:** Студент має завершені лабораторні роботи з ADO.NET (Lab 1) та EF Core Database-First (Lab 2) з певною предметною областю та базою даних.

Після виконання лабораторної роботи студент зможе:

- Конвертувати Database-First entities в Code-First підхід
- Налаштовувати entity classes через Fluent API (замість data annotations)
- Створювати окремі configuration класи через `IEntityTypeConfiguration<T>`
- Конфігурувати різні типи відносин: one-to-many, many-to-many, one-to-one, self-referencing
- Налаштовувати індекси, constraints, default values, computed columns через Fluent API
- Розуміти та керувати міграціями: генерація, перегляд, застосування, rollback
- Виконувати data seeding через `HasData()` метод
- Обробляти schema evolution: додавання properties, entities, зміна структури
- Писати custom SQL в міграціях для складних трансформацій даних
- Реалізовувати advanced patterns: soft delete з global query filters, value converters, owned entities
- Порівнювати Database-First та Code-First підходи
- Приймати обґрунтовані рішення про вибір підходу для конкретних сценаріїв

---

## 1. ПРАКТИЧНІ ЗАВДАННЯ

### Завдання 1: Entity Definition та DbContext Setup (15 балів)

**Опис:**
Підготовка entity classes для Code-First підходу шляхом очищення scaffolded entities з Lab 2 та створення нового DbContext з нуля. Розуміння різниці між згенерованим та вручну створеним DbContext.

**Передумови:**
Студент має завершену Lab 2 з згенерованими (scaffolded) entity класами та DbContext.

**Вимоги до виконання:**

1. **Створити новий проєкт або папку для Code-First підходу:**
   - Рекомендується: `YourProjectName.CodeFirst`
   - Встановити NuGet пакети:
     - `Microsoft.EntityFrameworkCore.SqlServer`
     - `Microsoft.EntityFrameworkCore.Tools` (для команд міграцій)
     - `Microsoft.EntityFrameworkCore.Design`

2. **Скопіювати entity класи з Lab 2 та очистити їх:**
   - Видалити усі data annotations окрім `[Key]` (якщо не можна визначити через convention)
   - Видалити атрибути згенеровані scaffolding: `[Column]`, `[Table]`, `[ForeignKey]`, тощо
   - Залишити тільки властивості та navigation properties
   - Переконатись що navigation properties правильно визначені (collection vs reference)
   - Видалити коментарі згенеровані scaffolding

3. **Створити новий DbContext клас з нуля:**
   - НЕ копіювати згенерований DbContext з Lab 2
   - Створити чистий клас що наслідує `DbContext`
   - Додати `DbSet<T>` властивості для кожної entity
   - Створити конструктор що приймає `DbContextOptions<TContext>`
   - НЕ використовувати `OnConfiguring` для connection string (буде через DI)

4. **Створити Program.cs для демонстрації:**
   - Налаштувати DbContext через dependency injection
   - Додати connection string з User Secrets або configuration
   - Переконатись що DbContext можна створити

**Приклад чистого entity класу (ДО очищення):**

```csharp
// Scaffolded entity з Lab 2 (BEFORE - згенерований код)
[Table("Books")]
public partial class Book
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ISBN")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Isbn { get; set; }

    [Required]
    [Column("Title")]
    [StringLength(200)]
    public string Title { get; set; } = null!;

    [Column("CategoryId")]
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Books")]
    public virtual Category? Category { get; set; }

    [InverseProperty("Book")]
    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
```

**Приклад чистого entity класу (ПІСЛЯ очищення):**

```csharp
// Code-First entity (AFTER - очищений для Code-First)
public class Book
{
    public int Id { get; set; }
    public string? Isbn { get; set; }
    public string Title { get; set; } = null!;
    public decimal? Price { get; set; }
    public int? CategoryId { get; set; }

    // Navigation properties
    public Category? Category { get; set; }
    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
```

**Приклад чистого DbContext:**

```csharp
public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфігурація буде в Завданні 2
    }
}
```

**Критерії оцінювання:**

- Правильне встановлення NuGet пакетів та структура проєкту (4 бали)
- Entity класи очищені від scaffolding artifacts, залишені тільки властивості (6 балів)
- Новий DbContext створений з нуля з правильним конструктором (5 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції "Code-First Fundamentals", "Entity Definition"
- docs/01-project-setup.md - налаштування User Secrets

---

### Завдання 2: Fluent API Configuration та Relationships (25 балів)

**Опис:**
Конфігурація entities через Fluent API замість data annotations. Створення окремих configuration класів для кожної entity через паттерн `IEntityTypeConfiguration<T>`. Налаштування всіх аспектів: primary keys, properties, indexes, relationships.

**Вимоги до виконання:**

1. **Створити окремі configuration класи для кожної entity:**
   - Створити папку `Configurations/` в проєкті
   - Для кожної entity створити клас `{EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>`
   - Реалізувати метод `Configure(EntityTypeBuilder<T> builder)`
   - Застосувати всі configuration класи в `OnModelCreating`

2. **Налаштувати основні властивості для КОЖНОЇ entity:**
   - Primary key (якщо не за convention `Id`)
   - Назви таблиць (якщо відрізняються від DbSet)
   - Required fields через `IsRequired()`
   - String lengths через `HasMaxLength()`
   - Decimal precision через `HasColumnType("decimal(10,2)")`
   - Default values через `HasDefaultValueSql()`
   - Computed columns через `HasComputedColumnSql()`

3. **Налаштувати індекси:**
   - Unique indexes для полів які мають бути унікальними (ISBN, Email, тощо)
   - Regular indexes для foreign keys та часто використовуваних полів
   - Filtered indexes (з WHERE умовою)
   - Composite indexes (на кілька колонок)

4. **Налаштувати relationships для ВСІХ зв'язків:**

   **One-to-Many (наприклад, Category → Books):**
   ```csharp
   builder.HasOne(b => b.Category)
       .WithMany(c => c.Books)
       .HasForeignKey(b => b.CategoryId)
       .OnDelete(DeleteBehavior.SetNull); // або Cascade, Restrict
   ```

   **Many-to-Many через explicit junction table (Books ↔ Authors):**
   ```csharp
   // У BookAuthorConfiguration
   builder.HasKey(ba => new { ba.BookId, ba.AuthorId }); // Composite PK

   builder.HasOne(ba => ba.Book)
       .WithMany(b => b.BookAuthors)
       .HasForeignKey(ba => ba.BookId)
       .OnDelete(DeleteBehavior.Cascade);

   builder.HasOne(ba => ba.Author)
       .WithMany(a => a.BookAuthors)
       .HasForeignKey(ba => ba.AuthorId)
       .OnDelete(DeleteBehavior.Cascade);
   ```

   **Self-referencing (якщо є, наприклад, Category → ParentCategory):**
   ```csharp
   builder.HasOne(c => c.ParentCategory)
       .WithMany(c => c.SubCategories)
       .HasForeignKey(c => c.ParentCategoryId)
       .OnDelete(DeleteBehavior.Restrict); // НЕ Cascade для self-reference!
   ```

5. **Застосувати configuration класи в OnModelCreating:**
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.ApplyConfiguration(new BookConfiguration());
       modelBuilder.ApplyConfiguration(new AuthorConfiguration());
       modelBuilder.ApplyConfiguration(new CategoryConfiguration());
       modelBuilder.ApplyConfiguration(new BookAuthorConfiguration());

       // АБО застосувати всі конфігурації з assembly
       modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
   }
   ```

**Приклад повної BookConfiguration:**

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // Table name
        builder.ToTable("Books");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Isbn)
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(b => b.Price)
            .HasColumnType("decimal(10,2)");

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(b => b.Isbn)
            .IsUnique()
            .HasFilter("[Isbn] IS NOT NULL");

        builder.HasIndex(b => b.Title);

        builder.HasIndex(b => new { b.CategoryId, b.PublishedDate })
            .HasDatabaseName("IX_Books_Category_PublishedDate");

        // Relationships
        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

**Приклад AuthorConfiguration з computed column:**

```csharp
public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.Property(a => a.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Computed column: FullName = FirstName + ' ' + LastName
        builder.Property(a => a.FullName)
            .HasComputedColumnSql("[FirstName] + ' ' + [LastName]", stored: true);

        builder.Property(a => a.Email)
            .HasMaxLength(200);

        // Unique index on Email
        builder.HasIndex(a => a.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");
    }
}
```

**Критерії оцінювання:**

- Створені окремі configuration класи для всіх entities (5 балів)
- Правильно налаштовані properties (required, lengths, types, defaults) (6 балів)
- Налаштовані індекси (unique, regular, filtered, composite) (4 бали)
- Всі relationships правильно налаштовані з appropriate delete behaviors (8 балів)
- Configuration класи застосовані в OnModelCreating (2 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції "Fluent API Configuration", "Relationships Configuration", "Indexes and Constraints"

---

### Завдання 3: Initial Migration Generation та Schema Comparison (15 балів)

**Опис:**
Генерація початкової міграції з entity classes, аналіз згенерованих Up/Down методів, застосування до нової бази даних, та порівняння результуючої схеми з оригінальною базою з Lab 1.

**Вимоги до виконання:**

1. **Згенерувати початкову міграцію:**
   ```bash
   dotnet ef migrations add InitialCreate --project YourProject.CodeFirst
   ```

2. **Проаналізувати згенеровану міграцію:**
   - Відкрити файл міграції в `Migrations/{Timestamp}_InitialCreate.cs`
   - Переглянути метод `Up()` - що створюється (таблиці, колонки, індекси, FK)
   - Переглянути метод `Down()` - як відкотити міграцію
   - Переглянути snapshot `{Context}ModelSnapshot.cs` - поточний стан моделі

3. **Застосувати міграцію до НОВОЇ бази даних:**
   ```bash
   # Створити нову БД або видалити існуючу
   dotnet ef database drop --force --project YourProject.CodeFirst

   # Застосувати міграцію
   dotnet ef database update --project YourProject.CodeFirst
   ```

4. **Порівняти схеми:**
   - **Оригінальна БД з Lab 1** (створена через ADO.NET міграції)
   - **Нова БД з Lab 3** (створена через Code-First міграцію)

   Використати JetBrains:
   - https://www.jetbrains.com/help/idea/schema-comparison-and-migration.html#show_differences_between_objects
   
   Або SQL Server Management Studio або Azure Data Studio:
   ```sql
   -- Порівняти таблиці
   SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
   WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;

   -- Порівняти колонки для конкретної таблиці
   SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'Books'
   ORDER BY ORDINAL_POSITION;

   -- Порівняти індекси
   SELECT
       i.name AS IndexName,
       i.type_desc AS IndexType,
       i.is_unique AS IsUnique,
       COL_NAME(ic.object_id, ic.column_id) AS ColumnName
   FROM sys.indexes i
   INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
   WHERE OBJECT_NAME(i.object_id) = 'Books'
   ORDER BY i.name, ic.key_ordinal;
   ```

5. **Задокументувати різниці:**
   - Створити порівняльну таблицю в REPORT.md
   - Які таблиці, колонки, індекси, constraints відрізняються відрізняються та ЧОМУ (пояснити причини)
   - Чи є відсутні елементи (stored procedures, views, functions - Code-First їх не створює)

6. **Перевірити міграційну таблицю:**
   ```sql
   SELECT * FROM __EFMigrationsHistory;
   ```
   - Переконатись що міграція `InitialCreate` записана

**Приклад структури згенерованої міграції:**

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Books",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Isbn = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Books", x => x.Id);
                table.ForeignKey(
                    name: "FK_Books_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Books_CategoryId",
            table: "Books",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Books_Isbn",
            table: "Books",
            column: "Isbn",
            unique: true,
            filter: "[Isbn] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Books");
        migrationBuilder.DropTable(name: "Categories");
    }
}
```

**Приклад порівняльної таблиці для REPORT.md:**

| Елемент | Lab 1 (ADO.NET) | Lab 3 (Code-First) | Співпадає? | Пояснення |
|---------|----------------|-------------------|-----------|-----------|
| Таблиця Books | ✓ | ✓ | ✓ | Однакова структура |
| Колонка Books.ISBN | VARCHAR(20) | VARCHAR(20) | ✓ | Правильно налаштовано |
| Індекс на ISBN | Unique | Unique with filter | ≈ | Code-First додав filter для NULL |
| Stored Procedure GetBooksByCategory | ✓ | ✗ | ✗ | Code-First не створює SP |
| View ActiveBooks | ✓ | ✗ | ✗ | Code-First не створює views |

**Критерії оцінювання:**

- Міграція згенерована успішно, містить всі таблиці та relationships (4 бали)
- Міграція застосована до бази даних, таблиця __EFMigrationsHistory оновлена (3 бали)
- Виконано порівняння схем двох баз даних (4 бали)
- Створена порівняльна таблиця з поясненнями різниць (4 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секція "Migrations Deep Dive"

---

### Завдання 4: Data Seeding з HasData() (10 балів)

**Опис:**
Реалізація seed даних для lookup/reference таблиць через метод `HasData()` в configuration класах. Розуміння як працювати з primary keys та foreign keys в seed data.

**Вимоги до виконання:**

1. **Додати seed дані в configuration класи:**
   - Мінімум для 2-3 lookup/reference таблиць (Categories, наприклад)
   - Використати `HasData()` метод в `Configure()`
   - Явно вказати primary key значення
   - Правильно налаштувати foreign keys для зв'язаних даних

2. **Seed дані мають включати:**
   - Lookup таблиці (Categories, Roles, Statuses, тощо)
   - Опціонально: кілька records основних entities для демонстрації
   - Опціонально: junction table records для many-to-many relationships

3. **Згенерувати та застосувати міграцію з seed даними:**
   ```bash
   dotnet ef migrations add SeedData
   dotnet ef database update
   ```

4. **Перевірити що дані з'явились в БД:**
   ```sql
   SELECT * FROM Categories;
   SELECT * FROM Books;
   SELECT * FROM BookAuthors;
   ```

**Приклад seed даних для CategoryConfiguration:**

```csharp
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // ... інша конфігурація ...

        // Seed data
        builder.HasData(
            new Category
            {
                Id = 1,
                Name = "Fiction",
                Description = "Fictional literature including novels and short stories"
            },
            new Category
            {
                Id = 2,
                Name = "Non-Fiction",
                Description = "Factual books and educational literature"
            },
            new Category
            {
                Id = 3,
                Name = "Science Fiction",
                Description = "Science fiction and fantasy literature",
                ParentCategoryId = 1 // Self-referencing FK
            },
            new Category
            {
                Id = 4,
                Name = "Biography",
                Description = "Biographical and autobiographical works",
                ParentCategoryId = 2
            }
        );
    }
}
```

**Приклад seed даних для BookConfiguration з FK:**

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ... інша конфігурація ...

        builder.HasData(
            new Book
            {
                Id = 1,
                Isbn = "978-0-123456-78-9",
                Title = "The Hobbit",
                CategoryId = 3, // FK до Fiction → Science Fiction
                Price = 19.99m,
                PublishedDate = new DateTime(1937, 9, 21)
            },
            new Book
            {
                Id = 2,
                Isbn = "978-0-987654-32-1",
                Title = "A Brief History of Time",
                CategoryId = 2, // FK до Non-Fiction
                Price = 15.99m,
                PublishedDate = new DateTime(1988, 4, 1)
            }
        );
    }
}
```

**Приклад seed даних для junction table (BookAuthorConfiguration):**

```csharp
public class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        // ... інша конфігурація ...

        // Composite key
        builder.HasKey(ba => new { ba.BookId, ba.AuthorId });

        // Seed data - спочатку потрібно мати seed для Authors
        builder.HasData(
            new BookAuthor
            {
                BookId = 1,
                AuthorId = 1,
                Role = "Author",
                DisplayOrder = 1
            },
            new BookAuthor
            {
                BookId = 2,
                AuthorId = 2,
                Role = "Author",
                DisplayOrder = 1
            }
        );
    }
}
```

**ВАЖЛИВО - порядок seed даних:**

Для foreign keys потрібно дотримуватись порядку:
1. Спочатку seed parent entities (Categories, Authors)
2. Потім seed child entities (Books з CategoryId, AuthorId)
3. Потім seed junction tables (BookAuthors)

**Альтернатива - seed через окремий метод:**

Якщо seed даних багато, можна створити окремий extension метод:

```csharp
public static class ModelBuilderExtensions
{
    public static void SeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Fiction" },
            new Category { Id = 2, Name = "Non-Fiction" }
            // ... more seed data
        );

        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Book 1", CategoryId = 1 },
            new Book { Id = 2, Title = "Book 2", CategoryId = 2 }
        );
    }
}

// В OnModelCreating:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    modelBuilder.SeedData(); // Seed всіх даних
}
```

**Згенерована міграція з seed даними:**

```csharp
public partial class SeedData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Categories",
            columns: new[] { "Id", "Name", "Description", "ParentCategoryId" },
            values: new object[,]
            {
                { 1, "Fiction", "Fictional literature", null },
                { 2, "Non-Fiction", "Factual books", null },
                { 3, "Science Fiction", "SF literature", 1 }
            });

        migrationBuilder.InsertData(
            table: "Books",
            columns: new[] { "Id", "Isbn", "Title", "CategoryId", "Price" },
            values: new object[,]
            {
                { 1, "978-0-123456-78-9", "The Hobbit", 3, 19.99m },
                { 2, "978-0-987654-32-1", "A Brief History of Time", 2, 15.99m }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(table: "Books", keyColumn: "Id", keyValue: 1);
        migrationBuilder.DeleteData(table: "Books", keyColumn: "Id", keyValue: 2);
        migrationBuilder.DeleteData(table: "Categories", keyColumn: "Id", keyValue: 3);
        migrationBuilder.DeleteData(table: "Categories", keyColumn: "Id", keyValue: 2);
        migrationBuilder.DeleteData(table: "Categories", keyColumn: "Id", keyValue: 1);
    }
}
```

**Критерії оцінювання:**

- Seed дані додані для мінімум 2-3 таблиць через HasData() (4 бали)
- Правильно налаштовані primary keys в seed даних (2 бали)
- Правильно налаштовані foreign keys з дотриманням порядку (2 бали)
- Міграція згенерована та застосована, дані з'явились в БД (2 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секція "Data Seeding Strategies"

---

### Завдання 5: Schema Evolution через Migrations (20 балів)

**Опис:**
Демонстрація повного lifecycle schema evolution через міграції: додавання properties, entities, модифікація структури, rollback, та custom SQL в міграціях для складних трансформацій.

**Вимоги до виконання:**

Виконати ВСІ 5 сценаріїв зміни схеми:

#### Сценарій 1: Додавання нової властивості до існуючої entity (4 бали)

**Завдання:**
- Додати нову властивість до однієї з entities (наприклад, `Book.Publisher`)
- Згенерувати міграцію
- Проаналізувати Up/Down методи
- Застосувати міграцію

**Приклад:**

```csharp
// 1. Додати property в entity клас
public class Book
{
    // ... існуючі properties ...
    public string? Publisher { get; set; } // NEW
}

// 2. Оновити configuration (опціонально, для constraints)
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ... існуюча конфігурація ...

        builder.Property(b => b.Publisher)
            .HasMaxLength(200);
    }
}

// 3. Згенерувати міграцію
// dotnet ef migrations add AddPublisherToBook

// 4. Переглянути згенеровану міграцію
public partial class AddPublisherToBook : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Publisher",
            table: "Books",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Publisher",
            table: "Books");
    }
}

// 5. Застосувати
// dotnet ef database update
```

#### Сценарій 2: Додавання нової entity з relationships (4 бали)

**Завдання:**
- Створити нову entity з relationship до існуючих entities
- Наприклад: `Review` entity (зв'язана з `Book` та `Member`)
- Створити configuration клас
- Згенерувати та застосувати міграцію

**Приклад:**

```csharp
// 1. Нова entity
public class Review
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Book Book { get; set; } = null!;
    public Member Member { get; set; } = null!;
}

// 2. Configuration
public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(r => r.Book)
            .WithMany(b => b.Reviews)
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Member)
            .WithMany(m => m.Reviews)
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index
        builder.HasIndex(r => new { r.BookId, r.MemberId });
    }
}

// 3. Додати DbSet в контекст
public class LibraryDbContext : DbContext
{
    // ... існуючі DbSets ...
    public DbSet<Review> Reviews => Set<Review>();
}

// 4. Згенерувати міграцію
// dotnet ef migrations add AddReviewEntity

// Міграція створить таблицю Reviews з двома FK та індексами
```

#### Сценарій 3: Перейменування властивості/колонки (4 бали)

**Завдання:**
- Перейменувати property в entity (наприклад, `Book.Title` → `Book.BookTitle`)
- Модифікувати міграцію щоб зберегти дані (RenameColumn замість Drop+Add)
- Застосувати міграцію

**ВАЖЛИВО:** За замовчуванням EF Core генерує Drop + Add для rename, що втрачає дані!

**Приклад:**

```csharp
// 1. Перейменувати property
public class Book
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = null!; // Renamed from Title
    // ...
}

// 2. Згенерувати міграцію
// dotnet ef migrations add RenameBookTitle

// 3. EF Core згенерує (ПОГАНО - втрата даних):
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "Title", table: "Books");
    migrationBuilder.AddColumn<string>(
        name: "BookTitle",
        table: "Books",
        type: "nvarchar(200)",
        nullable: false,
        defaultValue: "");
}

// 4. ВИПРАВИТИ вручну на RenameColumn (ДОБРЕ):
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "Title",
        table: "Books",
        newName: "BookTitle");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "BookTitle",
        table: "Books",
        newName: "Title");
}

// 5. Застосувати
// dotnet ef database update
```

#### Сценарій 4: Модифікація індексу або constraint (4 бали)

**Завдання:**
- Змінити існуючий індекс (наприклад, зробити unique або додати колонку)
- АБО додати новий constraint (CHECK constraint)
- Згенерувати та застосувати міграцію

**Приклад - додавання CHECK constraint:**

```csharp
// 1. Оновити configuration
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ... існуюча конфігурація ...

        // Додати CHECK constraint: Price >= 0
        builder.HasCheckConstraint("CK_Books_Price", "[Price] >= 0");

        // Модифікувати існуючий індекс - додати колонку
        builder.HasIndex(b => new { b.CategoryId, b.PublishedDate, b.Price })
            .HasDatabaseName("IX_Books_Category_Date_Price");
    }
}

// 2. Згенерувати міграцію
// dotnet ef migrations add AddPriceConstraintAndModifyIndex

// 3. Згенерована міграція:
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Видалити старий індекс
    migrationBuilder.DropIndex(
        name: "IX_Books_Category_PublishedDate",
        table: "Books");

    // Додати CHECK constraint
    migrationBuilder.AddCheckConstraint(
        name: "CK_Books_Price",
        table: "Books",
        sql: "[Price] >= 0");

    // Створити новий індекс
    migrationBuilder.CreateIndex(
        name: "IX_Books_Category_Date_Price",
        table: "Books",
        columns: new[] { "CategoryId", "PublishedDate", "Price" });
}
```

#### Сценарій 5: Custom SQL в міграції для data transformation (4 бали)

**Завдання:**
- Створити міграцію з custom SQL для трансформації даних
- Наприклад: розділити колонку `FullName` на `FirstName` та `LastName`
- АБО populate нову колонку на основі існуючих даних
- Використати `migrationBuilder.Sql()`

**Приклад:**

```csharp
// Сценарій: Додали поле Book.PageCount, потрібно заповнити його
// на основі якоїсь логіки або default значення

// 1. Додати property
public class Book
{
    // ...
    public int? PageCount { get; set; } // NEW
}

// 2. Згенерувати міграцію
// dotnet ef migrations add AddPageCountToBook

// 3. Модифікувати міграцію для data transformation:
public partial class AddPageCountToBook : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Додати колонку
        migrationBuilder.AddColumn<int>(
            name: "PageCount",
            table: "Books",
            type: "int",
            nullable: true);

        // Custom SQL для populate даних
        migrationBuilder.Sql(@"
            UPDATE Books
            SET PageCount = 300
            WHERE PageCount IS NULL AND CategoryId = 1; -- Fiction default 300

            UPDATE Books
            SET PageCount = 400
            WHERE PageCount IS NULL AND CategoryId = 2; -- Non-Fiction default 400
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PageCount",
            table: "Books");
    }
}
```

**Складніший приклад - розділення колонки:**

```csharp
// Сценарій: Було поле Author.FullName, треба розділити на FirstName та LastName

public partial class SplitAuthorFullName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Додати нові колонки
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "Authors",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "Authors",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        // 2. Populate з існуючих даних (розділити по пробілу)
        migrationBuilder.Sql(@"
            UPDATE Authors
            SET
                FirstName = LEFT(FullName, CHARINDEX(' ', FullName + ' ') - 1),
                LastName = SUBSTRING(FullName, CHARINDEX(' ', FullName + ' ') + 1, LEN(FullName))
            WHERE FullName IS NOT NULL;
        ");

        // 3. Зробити колонки required (тепер є дані)
        migrationBuilder.AlterColumn<string>(
            name: "FirstName",
            table: "Authors",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "LastName",
            table: "Authors",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            oldNullable: true);

        // 4. Видалити стару колонку
        migrationBuilder.DropColumn(
            name: "FullName",
            table: "Authors");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Відновити FullName
        migrationBuilder.AddColumn<string>(
            name: "FullName",
            table: "Authors",
            type: "nvarchar(200)",
            nullable: false,
            defaultValue: "");

        // Populate FullName назад
        migrationBuilder.Sql(@"
            UPDATE Authors
            SET FullName = FirstName + ' ' + LastName;
        ");

        // Видалити FirstName та LastName
        migrationBuilder.DropColumn(name: "FirstName", table: "Authors");
        migrationBuilder.DropColumn(name: "LastName", table: "Authors");
    }
}
```

**Демонстрація Rollback:**

```bash
# Переглянути застосовані міграції
dotnet ef migrations list

# Відкотити останню міграцію
dotnet ef database update PreviousMigrationName

# Відкотити всі міграції (повернути до порожньої БД)
dotnet ef database update 0

# Застосувати знову
dotnet ef database update
```

**Критерії оцінювання:**

- Сценарій 1: Додавання property - міграція згенерована та застосована (4 бали)
- Сценарій 2: Додавання entity - створена з relationships та застосована (4 бали)
- Сценарій 3: Rename property - міграція модифікована для RenameColumn (4 бали)
- Сценарій 4: Модифікація index/constraint - застосовано успішно (4 бали)
- Сценарій 5: Custom SQL - data transformation виконана коректно (4 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції "Migrations Deep Dive", "Schema Evolution", "Custom SQL in Migrations"

---

### Завдання 6: Advanced Patterns - На вибір студента (15 балів)

**Студент обирає ОДИН з трьох варіантів:**

---

#### Варіант A: Global Query Filters та Soft Delete Pattern (15 балів)

**Опис:**
Реалізація soft delete pattern де записи не видаляються фізично з БД, а тільки маркуються як видалені через поле `IsDeleted` або `DeletedAt`. Використання global query filters щоб автоматично фільтрувати видалені записи в усіх запитах.

**Вимоги:**

1. **Додати soft delete поля до entity:**

```csharp
public class Book
{
    // ... існуючі properties ...

    // Soft delete fields - обрати ОДИН підхід:

    // Підхід 1: Boolean flag
    public bool IsDeleted { get; set; }

    // АБО Підхід 2: Nullable DateTime (більш інформативний)
    public DateTime? DeletedAt { get; set; }
}
```

2. **Налаштувати global query filter в configuration:**

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ... існуюча конфігурація ...

        // Global Query Filter - автоматично фільтрує видалені записи
        builder.HasQueryFilter(b => !b.IsDeleted);
        // АБО для DateTime підходу:
        // builder.HasQueryFilter(b => b.DeletedAt == null);

        // Default value
        builder.Property(b => b.IsDeleted)
            .HasDefaultValue(false);
    }
}
```

3. **Згенерувати та застосувати міграцію:**

```bash
dotnet ef migrations add AddSoftDeleteToBook
dotnet ef database update
```

4. **Реалізувати методи soft delete в repository або service:**

```csharp
public class BookRepository
{
    private readonly LibraryDbContext _context;

    // Soft Delete - встановити IsDeleted = true
    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var book = await _context.Books.FindAsync(new object[] { id }, ct);
        if (book == null)
            throw new NotFoundException($"Book with ID {id} not found");

        book.IsDeleted = true;
        // АБО: book.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    // Restore - відновити видалений запис
    public async Task RestoreAsync(int id, CancellationToken ct = default)
    {
        // Потрібно ігнорувати query filter щоб знайти видалений запис
        var book = await _context.Books
            .IgnoreQueryFilters() // ← Важливо!
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (book == null)
            throw new NotFoundException($"Book with ID {id} not found");

        book.IsDeleted = false;
        // АБО: book.DeletedAt = null;

        await _context.SaveChangesAsync(ct);
    }

    // GetAll - за замовчуванням фільтрує видалені (завдяки global filter)
    public async Task<List<Book>> GetAllAsync(CancellationToken ct = default)
    {
        // Автоматично НЕ повертає IsDeleted = true записи
        return await _context.Books.ToListAsync(ct);
    }

    // GetAllIncludingDeleted - отримати ВСІ записи включно з видаленими
    public async Task<List<Book>> GetAllIncludingDeletedAsync(CancellationToken ct = default)
    {
        return await _context.Books
            .IgnoreQueryFilters() // Ігнорувати filter
            .ToListAsync(ct);
    }

    // GetOnlyDeleted - отримати тільки видалені записи
    public async Task<List<Book>> GetOnlyDeletedAsync(CancellationToken ct = default)
    {
        return await _context.Books
            .IgnoreQueryFilters()
            .Where(b => b.IsDeleted)
            .ToListAsync(ct);
    }
}
```

5. **Альтернатива - додати extension method до entity (Domain behavior):**

```csharp
public class Book
{
    // ... properties ...

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Behavior methods
    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }
}

// Usage:
var book = await _context.Books.FindAsync(id);
book.MarkAsDeleted();
await _context.SaveChangesAsync();
```

6. **Демонстрація роботи:**

```csharp
// Тест 1: Soft delete
var book = await repository.GetByIdAsync(1);
Console.WriteLine($"Book: {book.Title}");

await repository.SoftDeleteAsync(1);
Console.WriteLine("Book soft deleted");

// Тест 2: Спроба отримати видалений запис - не знайдено
var deletedBook = await repository.GetByIdAsync(1);
// NotFoundException - бо global filter фільтрує IsDeleted = true

// Тест 3: Отримати з ігноруванням фільтра
var allBooks = await repository.GetAllIncludingDeletedAsync();
Console.WriteLine($"Total books (including deleted): {allBooks.Count}");

var onlyDeleted = await repository.GetOnlyDeletedAsync();
Console.WriteLine($"Deleted books: {onlyDeleted.Count}");

// Тест 4: Відновлення
await repository.RestoreAsync(1);
Console.WriteLine("Book restored");

var restoredBook = await repository.GetByIdAsync(1);
Console.WriteLine($"Restored book: {restoredBook.Title}");
```

7. **Обробка cascade delete для soft delete:**

Якщо є child records, потрібно вирішити що з ними робити:

```csharp
// Опція 1: Каскадний soft delete (видалити всі пов'язані)
public async Task SoftDeleteBookWithReviewsAsync(int bookId)
{
    var book = await _context.Books
        .Include(b => b.Reviews)
        .FirstOrDefaultAsync(b => b.Id == bookId);

    if (book == null) throw new NotFoundException();

    // Soft delete всіх reviews
    foreach (var review in book.Reviews)
    {
        review.IsDeleted = true;
    }

    // Soft delete книги
    book.IsDeleted = true;

    await _context.SaveChangesAsync();
}

// Опція 2: Залишити child records (але вони стануть "осиротілими")
// Опція 3: Заборонити видалення якщо є child records
```

8. **Advanced: Audit fields + Soft Delete:**

```csharp
public class Book
{
    // ... основні properties ...

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; } // Хто видалив

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// Configuration
builder.Property(b => b.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

// Usage з current user
public async Task SoftDeleteAsync(int id, string currentUserId)
{
    var book = await _context.Books.FindAsync(id);
    book.IsDeleted = true;
    book.DeletedAt = DateTime.UtcNow;
    book.DeletedBy = currentUserId;
    await _context.SaveChangesAsync();
}
```

**Критерії оцінювання:**

- Поля IsDeleted/DeletedAt додані до entity, міграція застосована (3 бали)
- Global query filter налаштований правильно (4 бали)
- Реалізовані методи SoftDelete, Restore, GetAllIncludingDeleted (5 балів)
- Демонстрація роботи з усіма сценаріями (3 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секція "Advanced Patterns: Soft Delete and Query Filters"
- src/DbDemo.Infrastructure.EFCore.CodeFirst/Models/Book.cs - приклад реалізації

---

#### Варіант B: Value Converters та Owned Entities (15 балів)

**Опис:**
Використання value converters для перетворення даних між C# types та database types, та owned entities (value objects) для моделювання складних властивостей.

**Вимоги:**

**Частина 1: Value Converters (9 балів)**

Реалізувати ТРИ різних value converters:

**1. Enum to String Converter (3 бали):**

```csharp
// 1. Визначити enum
public enum BookStatus
{
    Available,
    CheckedOut,
    Reserved,
    Lost,
    Damaged
}

// 2. Додати property до entity
public class Book
{
    // ...
    public BookStatus Status { get; set; }
}

// 3. Налаштувати converter в configuration
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ...

        // Зберігати як string замість int
        builder.Property(b => b.Status)
            .HasConversion<string>() // "Available", "CheckedOut", etc.
            .HasMaxLength(50);

        // АБО explicit converter:
        builder.Property(b => b.Status)
            .HasConversion(
                v => v.ToString(),
                v => (BookStatus)Enum.Parse(typeof(BookStatus), v));
    }
}

// В БД буде зберігатись: "Available", "CheckedOut" замість 0, 1
```

**2. JSON Serialization Converter (3 бали):**

```csharp
// 1. Визначити complex object
public class BookMetadata
{
    public string? Illustrator { get; set; }
    public string? Translator { get; set; }
    public string? Edition { get; set; }
    public List<string> Awards { get; set; } = new();
    public Dictionary<string, string> ExternalIds { get; set; } = new();
}

// 2. Додати property до entity
public class Book
{
    // ...
    public BookMetadata? Metadata { get; set; }
}

// 3. Налаштувати JSON converter
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        // ...

        builder.Property(b => b.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<BookMetadata>(v, (JsonSerializerOptions?)null));
    }
}

// В БД буде зберігатись JSON string:
// {"Illustrator":"John Doe","Awards":["Best Book 2023"],...}

// Usage:
var book = new Book
{
    Title = "Sample",
    Metadata = new BookMetadata
    {
        Illustrator = "John Doe",
        Awards = new List<string> { "Best Book 2023" },
        ExternalIds = new Dictionary<string, string>
        {
            ["GoodReads"] = "12345",
            ["ISBN13"] = "978-0-123456-78-9"
        }
    }
};
await _context.Books.AddAsync(book);
await _context.SaveChangesAsync();

// Querying:
var booksWithAwards = await _context.Books
    .Where(b => EF.Functions.Like(b.Metadata, "%Best Book%"))
    .ToListAsync();
```

**3. Custom Value Converter (3 бали):**

Наприклад, шифрування/дешифрування чутливих даних:

```csharp
// 1. Визначити converter клас
public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter()
        : base(
            v => Encrypt(v),
            v => Decrypt(v))
    {
    }

    private static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        // Простий XOR encryption для демонстрації
        // В production використовувати AES або інші надійні алгоритми
        var key = "SecretKey123";
        var encrypted = new char[plainText.Length];
        for (int i = 0; i < plainText.Length; i++)
        {
            encrypted[i] = (char)(plainText[i] ^ key[i % key.Length]);
        }
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(encrypted));
    }

    private static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var key = "SecretKey123";
        var bytes = Convert.FromBase64String(cipherText);
        var chars = Encoding.UTF8.GetChars(bytes);
        var decrypted = new char[chars.Length];
        for (int i = 0; i < chars.Length; i++)
        {
            decrypted[i] = (char)(chars[i] ^ key[i % key.Length]);
        }
        return new string(decrypted);
    }
}

// 2. Додати sensitive property
public class Member
{
    // ...
    public string? SocialSecurityNumber { get; set; } // Sensitive!
}

// 3. Застосувати converter
public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        // ...

        builder.Property(m => m.SocialSecurityNumber)
            .HasConversion(new EncryptedStringConverter())
            .HasMaxLength(200); // Encrypted string довший
    }
}

// В БД буде зберігатись зашифрований текст
// В application - автоматично дешифрується при читанні
```

**Частина 2: Owned Entities (Value Objects) (6 балів)**

**1. Визначити value object:**

```csharp
// Address - value object (не має власного ID, належить Parent entity)
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? Country { get; set; }

    // Value objects мають value equality
    public override bool Equals(object? obj)
    {
        if (obj is not Address other) return false;
        return Street == other.Street
            && City == other.City
            && State == other.State
            && ZipCode == other.ZipCode;
    }

    public override int GetHashCode()
        => HashCode.Combine(Street, City, State, ZipCode);
}
```

**2. Додати owned property до entity:**

```csharp
public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!; // Owned entity
    public string? Website { get; set; }
}
```

**3. Налаштувати як owned entity:**

```csharp
public class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Налаштувати Address як owned entity
        builder.OwnsOne(p => p.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasMaxLength(200)
                .HasColumnName("Address_Street"); // Опціонально перейменувати

            addressBuilder.Property(a => a.City)
                .HasMaxLength(100)
                .HasColumnName("Address_City");

            addressBuilder.Property(a => a.State)
                .HasMaxLength(100)
                .HasColumnName("Address_State");

            addressBuilder.Property(a => a.ZipCode)
                .HasMaxLength(20)
                .HasColumnName("Address_ZipCode");

            addressBuilder.Property(a => a.Country)
                .HasMaxLength(100)
                .HasColumnName("Address_Country");
        });
    }
}
```

**Результат в БД:**

Таблиця `Publishers` буде мати колонки:
```
Id, Name, Website,
Address_Street, Address_City, Address_State, Address_ZipCode, Address_Country
```

НЕ буде окремої таблиці `Addresses`!

**4. Usage:**

```csharp
var publisher = new Publisher
{
    Name = "Penguin Random House",
    Address = new Address
    {
        Street = "1745 Broadway",
        City = "New York",
        State = "NY",
        ZipCode = "10019",
        Country = "USA"
    },
    Website = "https://www.penguinrandomhouse.com"
};

await _context.Publishers.AddAsync(publisher);
await _context.SaveChangesAsync();

// Querying:
var nyPublishers = await _context.Publishers
    .Where(p => p.Address.City == "New York")
    .ToListAsync();
```

**5. Alternative: Owned collection (якщо треба кілька адрес):**

```csharp
public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Address> Addresses { get; set; } = new(); // Owned collection
}

// Configuration:
builder.OwnsMany(p => p.Addresses, addressBuilder =>
{
    addressBuilder.Property(a => a.Street).HasMaxLength(200);
    addressBuilder.Property(a => a.City).HasMaxLength(100);
    // ...
});
```

В цьому випадку створюється окрема таблиця `Publisher_Addresses` з FK до `Publishers`.

**Демонстрація:**

```csharp
// 1. Value Converters
var book = new Book
{
    Title = "Test Book",
    Status = BookStatus.Available,
    Metadata = new BookMetadata { Illustrator = "John Doe" }
};
await _context.Books.AddAsync(book);
await _context.SaveChangesAsync();

// Перевірити в БД:
// Status зберігається як "Available" (string)
// Metadata як JSON

// 2. Owned Entities
var publisher = new Publisher
{
    Name = "Test Publisher",
    Address = new Address
    {
        Street = "123 Main St",
        City = "TestCity"
    }
};
await _context.Publishers.AddAsync(publisher);
await _context.SaveChangesAsync();

// Перевірити в БД:
// Publishers таблиця має колонки Address_Street, Address_City
// Немає окремої таблиці Addresses
```

**Критерії оцінювання:**

- Enum to String converter реалізований (3 бали)
- JSON serialization converter реалізований (3 бали)
- Custom converter (наприклад, encryption) реалізований (3 бали)
- Owned entity налаштована та демонстрована (6 балів)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції "Value Converters", "Owned Entities (Value Objects)"

---

#### Варіант C: Advanced Migration Strategies (15 балів)

**Опис:**
Поглиблене вивчення міграцій: data migrations для трансформації даних, розділення міграцій на schema та data частини, seeding з зовнішніх джерел, управління історією міграцій, та rollback strategies.

**Вимоги:**

**Частина 1: Data Migration з Custom SQL (5 балів)**

Сценарій: Змінити структуру існуючих даних під час міграції.

```csharp
// Приклад: Було поле "Tags" як comma-separated string,
// треба створити окрему таблицю BookTags

// 1. Створити нову entity
public class BookTag
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Tag { get; set; } = string.Empty;

    public Book Book { get; set; } = null!;
}

// 2. Додати navigation property
public class Book
{
    // ...
    public string? Tags { get; set; } // OLD - буде видалено
    public ICollection<BookTag> BookTags { get; set; } = new List<BookTag>(); // NEW
}

// 3. Згенерувати міграцію
// dotnet ef migrations add ConvertTagsToTable

// 4. Модифікувати міграцію для data transformation:
public partial class ConvertTagsToTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Створити нову таблицю BookTags
        migrationBuilder.CreateTable(
            name: "BookTags",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BookId = table.Column<int>(nullable: false),
                Tag = table.Column<string>(maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookTags", x => x.Id);
                table.ForeignKey(
                    name: "FK_BookTags_Books_BookId",
                    column: x => x.BookId,
                    principalTable: "Books",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // 2. Migrate data з Tags column до BookTags table
        migrationBuilder.Sql(@"
            INSERT INTO BookTags (BookId, Tag)
            SELECT
                b.Id,
                LTRIM(RTRIM(value)) AS Tag
            FROM Books b
            CROSS APPLY STRING_SPLIT(b.Tags, ',')
            WHERE b.Tags IS NOT NULL AND b.Tags <> '';
        ");

        // 3. Видалити стару колонку Tags
        migrationBuilder.DropColumn(
            name: "Tags",
            table: "Books");

        // 4. Створити індекс
        migrationBuilder.CreateIndex(
            name: "IX_BookTags_BookId",
            table: "BookTags",
            column: "BookId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Відновити колонку Tags
        migrationBuilder.AddColumn<string>(
            name: "Tags",
            table: "Books",
            type: "nvarchar(500)",
            nullable: true);

        // Migrate data назад з BookTags до Tags column
        migrationBuilder.Sql(@"
            UPDATE b
            SET b.Tags = STUFF((
                SELECT ',' + bt.Tag
                FROM BookTags bt
                WHERE bt.BookId = b.Id
                FOR XML PATH('')
            ), 1, 1, '')
            FROM Books b;
        ");

        // Видалити таблицю BookTags
        migrationBuilder.DropTable(name: "BookTags");
    }
}
```

**Частина 2: Splitting Migrations (Schema + Data) (3 бали)**

Розділити міграцію на дві частини для кращого контролю:

```csharp
// Міграція 1: Schema changes
// dotnet ef migrations add AddPublisherSchema

public partial class AddPublisherSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Тільки schema changes - CREATE TABLE, ALTER TABLE, etc.
        migrationBuilder.CreateTable(
            name: "Publishers",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                // ...
            });

        // Додати FK колонку до Books
        migrationBuilder.AddColumn<int>(
            name: "PublisherId",
            table: "Books",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Books_PublisherId",
            table: "Books",
            column: "PublisherId");

        migrationBuilder.AddForeignKey(
            name: "FK_Books_Publishers_PublisherId",
            table: "Books",
            column: "PublisherId",
            principalTable: "Publishers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}

// Міграція 2: Data seeding
// dotnet ef migrations add SeedPublishers

public partial class SeedPublishers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Тільки data changes - INSERT, UPDATE, etc.
        migrationBuilder.Sql(@"
            INSERT INTO Publishers (Name, Website, Country)
            VALUES
                ('Penguin Random House', 'https://www.penguinrandomhouse.com', 'USA'),
                ('HarperCollins', 'https://www.harpercollins.com', 'USA'),
                ('Simon & Schuster', 'https://www.simonandschuster.com', 'USA');
        ");

        // Update існуючих книг з відомими видавцями
        migrationBuilder.Sql(@"
            UPDATE Books
            SET PublisherId = (SELECT Id FROM Publishers WHERE Name = 'Penguin Random House')
            WHERE Publisher = 'Penguin Random House';

            UPDATE Books
            SET PublisherId = (SELECT Id FROM Publishers WHERE Name = 'HarperCollins')
            WHERE Publisher = 'HarperCollins';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM Publishers");
        migrationBuilder.Sql("UPDATE Books SET PublisherId = NULL");
    }
}
```

**Переваги splitting:**
- Легше code review (schema окремо, data окремо)
- Можна rollback тільки data частину
- Чітка separation of concerns

**Частина 3: Seeding з зовнішніх джерел (4 бали)**

```csharp
// Завантажити seed дані з JSON файлу або CSV

// 1. Створити JSON файл з seed даними: SeedData/books.json
[
  {
    "isbn": "978-0-123456-78-9",
    "title": "Sample Book 1",
    "categoryId": 1,
    "price": 19.99
  },
  {
    "isbn": "978-0-987654-32-1",
    "title": "Sample Book 2",
    "categoryId": 2,
    "price": 24.99
  }
]

// 2. Створити міграцію з custom SQL для load з файлу
public partial class SeedBooksFromJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Спосіб 1: Читати JSON в C# та генерувати SQL
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData", "books.json");
        var json = File.ReadAllText(jsonPath);
        var books = JsonSerializer.Deserialize<List<BookSeedData>>(json);

        foreach (var book in books)
        {
            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Isbn", "Title", "CategoryId", "Price" },
                values: new object[] { book.Isbn, book.Title, book.CategoryId, book.Price });
        }

        // Спосіб 2: Використати OPENROWSET в SQL Server
        migrationBuilder.Sql($@"
            INSERT INTO Books (Isbn, Title, CategoryId, Price)
            SELECT Isbn, Title, CategoryId, Price
            FROM OPENROWSET(
                BULK '{jsonPath}',
                SINGLE_CLOB
            ) AS j
            CROSS APPLY OPENJSON(BulkColumn)
            WITH (
                Isbn NVARCHAR(20) '$.isbn',
                Title NVARCHAR(200) '$.title',
                CategoryId INT '$.categoryId',
                Price DECIMAL(10,2) '$.price'
            );
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Видалити seed дані
        migrationBuilder.Sql("DELETE FROM Books WHERE Isbn IN (...)");
    }
}

class BookSeedData
{
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
}
```

**Спосіб 3: Seed з CSV файлу:**

```csharp
// SeedData/books.csv:
// Isbn,Title,CategoryId,Price
// 978-0-123456-78-9,Sample Book 1,1,19.99
// 978-0-987654-32-1,Sample Book 2,2,24.99

public partial class SeedBooksFromCsv : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData", "books.csv");

        // Використати BULK INSERT
        migrationBuilder.Sql($@"
            BULK INSERT Books
            FROM '{csvPath}'
            WITH (
                FIRSTROW = 2,  -- Skip header
                FIELDTERMINATOR = ',',
                ROWTERMINATOR = '\n',
                TABLOCK
            );
        ");
    }
}
```

**Частина 4: Migration History Management (3 бали)**

```csharp
// 1. Переглянути історію міграцій
// dotnet ef migrations list

// Output:
// 20231201120000_InitialCreate
// 20231205140000_AddPublisherSchema
// 20231205141000_SeedPublishers (Pending)

// 2. Застосувати до конкретної міграції
// dotnet ef database update AddPublisherSchema

// 3. Rollback всіх міграцій
// dotnet ef database update 0

// 4. Видалити останню міграцію (якщо ще не застосована)
// dotnet ef migrations remove

// 5. Згенерувати idempotent SQL script (можна запускати повторно)
// dotnet ef migrations script --idempotent --output migrations.sql

// 6. Згенерувати script тільки для нових міграцій
// dotnet ef migrations script FromMigration ToMigration --output delta.sql

// Приклад idempotent script:
IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20231201120000_InitialCreate')
BEGIN
    CREATE TABLE [Books] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Books] PRIMARY KEY ([Id])
    );

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20231201120000_InitialCreate', N'9.0.0');
END;
GO
```

**Частина 5: Rollback Strategies (продемонструвати) (0 балів, частина демонстрації)**

```csharp
// Сценарій: Застосували міграцію, але щось пішло не так

// 1. Перевірити поточний стан
dotnet ef migrations list

// 2. Rollback до попередньої міграції
dotnet ef database update PreviousMigrationName

// 3. Перевірити що Down() метод виконався
// Перевірити таблицю __EFMigrationsHistory

// 4. Виправити міграцію
// Відредагувати файл міграції або видалити та створити знову

// 5. Застосувати знову
dotnet ef database update
```

**Критерії оцінювання:**

- Data migration з трансформацією даних через custom SQL (5 балів)
- Splitting migrations на schema та data частини (3 бали)
- Seeding з зовнішніх джерел (JSON/CSV) (4 бали)
- Демонстрація migration history management та rollback (3 бали)

**Документи для вивчення:**
- docs/30-ef-code-first.md - секції "Migrations Deep Dive", "Data Seeding Strategies", "Advanced Migration Scenarios"

---

## 2. МЕТОДИЧНІ РЕКОМЕНДАЦІЇ

### 2.1. Code-First vs Database-First: Коли використовувати що?

**Code-First (цей Lab):**

Джерело істини: C# entity classes

**Переваги:**
- Повний контроль над entity classes (domain model)
- Version control для схеми БД (міграції як код)
- Легше collaboration в команді (merge conflicts рідше)
- Test-friendly (легко створити in-memory БД для тестів)
- Domain-Driven Design approach
- Підходить для нових проєктів (greenfield)

**Недоліки:**
- Менше контролю над точним SQL який генерується
- Складніше працювати з legacy БД з нестандартною структурою
- Потрібно знати як правильно конфігурувати entities
- Міграції можуть накопичуватись та стати складними

**Коли використовувати:**
- Нові проєкти
- Коли розробники контролюють схему БД
- Коли важливий version control схеми
- Коли використовується DDD підхід
- Коли потрібні automated deployments

**Database-First (Lab 2):**

Джерело істини: SQL database schema

**Переваги:**
- Повний контроль над БД через SQL
- Підходить для legacy баз даних
- DBA може керувати схемою незалежно
- Легше працювати з complex БД features (indexed views, functions)
- Scaffolding генерує код автоматично

**Недоліки:**
- Entities можуть бути не ідеальними для domain model
- Складніше version control схеми
- Re-scaffolding перезаписує зміни в entities
- Менше підходить для team collaboration

**Коли використовувати:**
- Legacy databases
- Коли DBA керує схемою
- Коли БД shared між кількома додатками
- Коли використовуються advanced DB features (SP, views, functions)
- Коли схема БД рідко змінюється

**Гібридний підхід:**
Можна починати з Database-First, потім переходити на Code-First для нових features.

### 2.2. Entity Configuration: Data Annotations vs Fluent API

**Data Annotations:**

```csharp
[Table("Books")]
public class Book
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
}
```

**Переваги:**
- Простіше для простих сценаріїв
- Менше коду
- Validation attributes також для ASP.NET MVC

**Недоліки:**
- Забруднює domain entities
- Не всі features доступні (немає всіх можливостей Fluent API)
- Entity класи залежать від EF Core

**Fluent API (рекомендується для Code-First):**

```csharp
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Price).HasColumnType("decimal(10,2)");
    }
}
```

**Переваги:**
- Separation of concerns - entity classes чисті
- Повний контроль над конфігурацією
- Підтримка всіх EF Core features
- Entity classes не залежать від EF Core

**Недоліки:**
- Більше коду
- Окремі configuration класи

**Рекомендація:**
В Code-First підході краще використовувати Fluent API для чистих entity classes.

### 2.3. Migrations Workflow

**Основний workflow:**

```bash
# 1. Змінити entity class або configuration
# (додати property, змінити relationship, тощо)

# 2. Згенерувати міграцію
dotnet ef migrations add MigrationName

# 3. Переглянути згенеровану міграцію
# Відкрити Migrations/{Timestamp}_MigrationName.cs
# Перевірити Up() та Down() методи

# 4. Якщо потрібно, модифікувати міграцію
# Додати custom SQL, виправити rename на RenameColumn, тощо

# 5. Застосувати міграцію
dotnet ef database update

# 6. Перевірити результат в БД
# Перевірити таблицю __EFMigrationsHistory
```

**Best Practices:**

1. **Малі міграції:** Краще багато малих міграцій ніж одна велика
2. **Descriptive names:** `AddPublisherToBook`, не `Update1` або `Changes`
3. **Review before apply:** Завжди переглядати Up/Down перед застосуванням
4. **Test rollback:** Перевіряти що Down() працює коректно
5. **Custom SQL:** Додавати для data migrations або complex changes
6. **Idempotent scripts:** Генерувати для production deployments

**Типові проблеми:**

**Проблема 1: Rename виявляється як Drop+Add**

```csharp
// EF Core не знає що це rename
public class Book
{
    public string BookTitle { get; set; } // Was: Title
}

// Генерує:
migrationBuilder.DropColumn(name: "Title", table: "Books");
migrationBuilder.AddColumn<string>(name: "BookTitle", table: "Books");
// ❌ ВТРАТА ДАНИХ!

// Виправлення:
migrationBuilder.RenameColumn(
    name: "Title",
    table: "Books",
    newName: "BookTitle");
// ✓ Дані зберігаються
```

**Проблема 2: Foreign key conflicts при seed data**

```csharp
// ❌ ПОМИЛКА - категорії ще не існують
modelBuilder.Entity<Book>().HasData(
    new Book { Id = 1, Title = "Book", CategoryId = 1 }
);

// ✓ ПРАВИЛЬНО - спочатку categories
modelBuilder.Entity<Category>().HasData(
    new Category { Id = 1, Name = "Fiction" }
);
modelBuilder.Entity<Book>().HasData(
    new Book { Id = 1, Title = "Book", CategoryId = 1 }
);
```

**Проблема 3: Migration conflict в команді**

```bash
# Два розробники створили міграції одночасно
# Developer A: 20231201_AddPublisher
# Developer B: 20231201_AddReviews

# При merge conflict в ModelSnapshot

# Вирішення:
1. git pull (отримати обидві міграції)
2. dotnet ef database update (застосувати обидві)
3. dotnet ef migrations remove (видалити свою)
4. dotnet ef migrations add MergedMigration (створити merged версію)
```

### 2.4. Relationships Configuration

**One-to-Many:**

```csharp
// Category → Books (one-to-many)
builder.HasOne(b => b.Category)
    .WithMany(c => c.Books)
    .HasForeignKey(b => b.CategoryId)
    .OnDelete(DeleteBehavior.SetNull);
```

**Delete Behaviors:**
- `Cascade` - видалити child records (Books) при видаленні parent (Category)
- `SetNull` - встановити FK в NULL (потребує nullable FK)
- `Restrict` - заборонити видалення parent якщо є children
- `NoAction` - SQL Server default, подібно до Restrict

**Many-to-Many (explicit junction table):**

```csharp
// BookAuthor - junction table
public class BookAuthor
{
    public int BookId { get; set; }
    public int AuthorId { get; set; }
    public string? Role { get; set; } // Extra property

    public Book Book { get; set; } = null!;
    public Author Author { get; set; } = null!;
}

// Configuration
builder.HasKey(ba => new { ba.BookId, ba.AuthorId }); // Composite PK

builder.HasOne(ba => ba.Book)
    .WithMany(b => b.BookAuthors)
    .HasForeignKey(ba => ba.BookId);

builder.HasOne(ba => ba.Author)
    .WithMany(a => a.BookAuthors)
    .HasForeignKey(ba => ba.AuthorId);
```

**Many-to-Many (автоматична junction table, якщо немає extra properties):**

```csharp
// Простіше, якщо junction table не потребує extra properties
builder.HasMany(b => b.Authors)
    .WithMany(a => a.Books)
    .UsingEntity(j => j.ToTable("BookAuthors"));
```

**One-to-One:**

```csharp
// Book ↔ BookDetail (one-to-one)
builder.HasOne(b => b.Detail)
    .WithOne(d => d.Book)
    .HasForeignKey<BookDetail>(d => d.BookId);
```

**Self-Referencing:**

```csharp
// Category → SubCategories (self-referencing)
builder.HasOne(c => c.ParentCategory)
    .WithMany(c => c.SubCategories)
    .HasForeignKey(c => c.ParentCategoryId)
    .OnDelete(DeleteBehavior.Restrict); // ВАЖЛИВО: не Cascade!
```

### 2.5. Indexes в Code-First

```csharp
// Simple index
builder.HasIndex(b => b.Title);

// Unique index
builder.HasIndex(b => b.Isbn)
    .IsUnique();

// Filtered index (з WHERE умовою)
builder.HasIndex(b => b.Isbn)
    .IsUnique()
    .HasFilter("[Isbn] IS NOT NULL");

// Composite index (на кілька колонок)
builder.HasIndex(b => new { b.CategoryId, b.PublishedDate })
    .HasDatabaseName("IX_Books_Category_Date");

// Включені колонки (SQL Server 2016+)
builder.HasIndex(b => b.Title)
    .IncludeProperties(b => new { b.Price, b.PublishedDate });

// Descending index (SQL Server 2016+)
builder.HasIndex(b => b.PublishedDate)
    .IsDescending();
```

### 2.6. Default Values та Computed Columns

**Default values:**

```csharp
// SQL default value
builder.Property(b => b.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

// C# default value
builder.Property(b => b.IsDeleted)
    .HasDefaultValue(false);
```

**Computed columns:**

```csharp
// Обчислювана колонка (stored)
builder.Property(a => a.FullName)
    .HasComputedColumnSql("[FirstName] + ' ' + [LastName]", stored: true);

// Обчислювана колонка (not stored)
builder.Property(b => b.PriceWithTax)
    .HasComputedColumnSql("[Price] * 1.2", stored: false);
```

**Stored vs Not Stored:**
- `stored: true` - значення обчислюється при INSERT/UPDATE та зберігається
- `stored: false` - значення обчислюється при кожному SELECT (не зберігається)

### 2.7. Best Practices для Code-First

**DO:**

✓ Використовувати Fluent API замість data annotations для конфігурації
✓ Створювати окремі `IEntityTypeConfiguration<T>` класи для кожної entity
✓ Групувати configuration класи в папку `Configurations/`
✓ Використовувати `ApplyConfigurationsFromAssembly()` в OnModelCreating
✓ Давати descriptive назви міграціям
✓ Переглядати згенеровані міграції перед застосуванням
✓ Додавати custom SQL для data migrations
✓ Тестувати Down() методи (rollback)
✓ Використовувати idempotent scripts для production
✓ Версіонувати міграції в Git
✓ Seed lookup/reference дані через `HasData()`

**DON'T:**

✗ НЕ змішувати data annotations та Fluent API
✗ НЕ писати всю конфігурацію в OnModelCreating (використати окремі класи)
✗ НЕ забувати про Delete Behaviors (може призвести до cascade loops)
✗ НЕ використовувати Cascade для self-referencing relationships
✗ НЕ застосовувати міграції не переглянувши їх
✗ НЕ редагувати застосовані міграції (створити нову замість)
✗ НЕ видаляти міграції з історії
✗ НЕ використовувати `dotnet ef database drop` в production
✗ НЕ забувати про順序 seed даних (parent перед child)

---

## 3. ФОРМАТ ЗДАЧІ ТА ЗАГАЛЬНІ ВИМОГИ

### 3.1. Структура проєкту

```
YourProjectName.CodeFirst/
├── Models/                          # Entity classes (очищені, без attributes)
│   ├── Book.cs
│   ├── Author.cs
│   ├── Category.cs
│   ├── BookAuthor.cs
│   └── ...
├── Configurations/                  # Entity configurations (Fluent API)
│   ├── BookConfiguration.cs
│   ├── AuthorConfiguration.cs
│   ├── CategoryConfiguration.cs
│   ├── BookAuthorConfiguration.cs
│   └── ...
├── Migrations/                      # Generated migrations
│   ├── 20231201120000_InitialCreate.cs
│   ├── 20231205140000_AddPublisher.cs
│   ├── LibraryDbContextModelSnapshot.cs
│   └── ...
├── SeedData/                        # External seed data (опціонально)
│   ├── categories.json
│   └── books.csv
├── Repositories/                    # Repositories (якщо потрібні)
│   ├── IBookRepository.cs
│   ├── BookRepository.cs
│   └── ...
├── LibraryDbContext.cs             # DbContext class
├── Program.cs                       # Entry point, демонстрація
├── appsettings.json                # Config (БЕЗ connection string!)
├── README.md                        # Setup instructions
└── REPORT.md                        # Lab report
```

### 3.2. README.md

Має містити:

**1. Опис проєкту**
- Предметна область (продовження з Lab 1/2)
- Кількість entities та їх назви

**2. Передумови**
- Завершені Lab 1 (ADO.NET) та Lab 2 (EF Core Database-First)
- SQL Server встановлений
- .NET 9 SDK

**3. Налаштування**
```bash
# Клонувати репозиторій
git clone ...

# Налаштувати User Secrets
cd YourProjectName.CodeFirst
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"

# Відновити пакети
dotnet restore
```

**4. Застосування міграцій**
```bash
# Створити БД та застосувати міграції
dotnet ef database update

# АБО якщо БД вже існує з Lab 1/2, видалити спочатку
dotnet ef database drop --force
dotnet ef database update
```

**5. Запуск**
```bash
dotnet run
```

**6. Виконані завдання**
- ✓ Завдання 1: Entity Definition та DbContext Setup
- ✓ Завдання 2: Fluent API Configuration
- ✓ Завдання 3: Initial Migration та Schema Comparison
- ✓ Завдання 4: Data Seeding
- ✓ Завдання 5: Schema Evolution (5 сценаріїв)
- ✓ Завдання 6: Advanced Patterns - [Варіант A/B/C]

### 3.3. REPORT.md

Має містити:

**1. Завдання 1 - Entity Definition**
- Скріншоти entity classes (до та після очищення)
- DbContext definition
- User Secrets configuration

**2. Завдання 2 - Fluent API Configuration**
- Приклади configuration класів (мінімум 2)
- Фрагменти коду з relationships configuration
- Пояснення delete behaviors

**3. Завдання 3 - Initial Migration**
- Фрагмент згенерованої міграції (Up метод)
- SQL запити для порівняння схем
- Порівняльна таблиця:

| Елемент | Lab 1 (ADO.NET) | Lab 3 (Code-First) | Співпадає? | Пояснення |
|---------|----------------|-------------------|-----------|-----------|
| Таблиця Books | ✓ | ✓ | ✓ | ... |
| Індекс на ISBN | Unique | Unique filtered | ≈ | ... |

**4. Завдання 4 - Data Seeding**
- Код seed даних (HasData)
- SQL запити що показують seed дані в БД
- Скріншоти результатів

**5. Завдання 5 - Schema Evolution**
Для кожного з 5 сценаріїв:
- Що було змінено в entity/configuration
- Фрагмент згенерованої міграції
- До/після скріншоти БД

**6. Завдання 6 - Advanced Pattern**
Залежить від обраного варіанту:

**Варіант A (Soft Delete):**
- Код implementation
- Демонстрація роботи (до/після soft delete)
- Використання IgnoreQueryFilters()

**Варіант B (Value Converters):**
- Код всіх трьох converters
- Приклади даних в БД (як зберігаються)
- Owned entity configuration та результуюча схема

**Варіант C (Advanced Migrations):**
- Data migration з custom SQL
- Split migrations приклад
- Seeding з зовнішніх джерел

**7. Порівняльний аналіз**
- Порівняння Database-First (Lab 2) vs Code-First (Lab 3)
- Переваги та недоліки кожного підходу
- Коли використовувати кожен підхід

**8. Висновки**
- Що нового навчились
- Які складнощі виникли
- Рекомендації для практичного застосування

### 3.4. Вимоги до коду

**Стиль:**
- Професійний C# стиль (без емоджі)
- Англійські назви для classes, properties, methods
- XML documentation коментарі для публічних методів
- Консистентне форматування

**Code Quality:**
- Entity classes чисті (без data annotations для конфігурації)
- Окремі configuration класи для кожної entity
- Descriptive migration names
- Custom SQL в міграціях де потрібно (rename, data transformation)
- Proper delete behaviors для relationships
- Indexes для foreign keys та часто використовуваних полів

**Configuration:**
- Connection string через User Secrets (НЕ в appsettings.json)
- DbContext через dependency injection
- Async/await для всіх операцій

### 3.5. Критерії оцінювання

**Обов'язкові завдання (85 балів):**
- Завдання 1: Entity Definition та DbContext (15 балів)
- Завдання 2: Fluent API Configuration (25 балів)
- Завдання 3: Initial Migration (15 балів)
- Завдання 4: Data Seeding (10 балів)
- Завдання 5: Schema Evolution (20 балів)

**На вибір (15 балів):**
- Завдання 6: Один варіант з трьох (A/B/C)

**Документація:**
- README.md - зрозумілі інструкції
- REPORT.md - детальний звіт з скріншотами
- Код добре структурований та читабельний

**Загалом: 100 балів**

### 3.6. Здача роботи

**Формат:**
- Git repository (GitHub, GitLab, Bitbucket)
- АБО ZIP архів з усім проєктом (включно з .git якщо є)

**Має включати:**
- Весь source code
- Migrations folder з усіма міграціями
- README.md та REPORT.md
- appsettings.json (БЕЗ connection string)
- .gitignore (виключити bin/, obj/, *.user)

**Не включати:**
- bin/ та obj/ папки
- User secrets файли
- Database files (.mdf, .ldf)

### 3.7. Корисні команди

```bash
# EF Core Migrations
dotnet ef migrations add MigrationName
dotnet ef migrations list
dotnet ef migrations remove
dotnet ef database update
dotnet ef database update MigrationName
dotnet ef database update 0  # Rollback all
dotnet ef database drop --force
dotnet ef migrations script --idempotent --output script.sql

# User Secrets
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "value"
dotnet user-secrets list

# Build and Run
dotnet restore
dotnet build
dotnet run

# NuGet Packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

---

## ДОДАТОК A: Порівняння Lab 2 vs Lab 3

| Аспект | Lab 2 (Database-First) | Lab 3 (Code-First) |
|--------|----------------------|-------------------|
| **Джерело істини** | SQL Database | C# Entity Classes |
| **Початок роботи** | Scaffolding існуючої БД | Створення entity classes |
| **Конфігурація** | Згенерована (OnModelCreating) | Вручну (Fluent API) |
| **Зміна схеми** | ALTER TABLE → re-scaffold | Modify entities → migration |
| **Entity classes** | Містять багато attributes | Чисті POCO classes |
| **Navigation properties** | Генеруються автоматично | Визначаються вручну |
| **Version control схеми** | SQL scripts | Migration classes |
| **Workflow** | SQL → EF | EF → SQL |
| **Контроль над SQL** | Повний | Частковий |
| **Контроль над entities** | Обмежений | Повний |
| **Підходить для** | Legacy БД | Нові проєкти |
| **Складність** | Простіше (автоматичне) | Складніше (ручне) |
| **Flexibility** | Менше | Більше |

**Висновок:**
- Database-First для legacy баз даних та DBA-managed схем
- Code-First для нових проєктів та developer-managed схем
- Обидва підходи мають своє місце в реальних проєктах

---

**Успіхів у виконанні лабораторної роботи!**
