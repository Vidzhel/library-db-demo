# МЕТОДИЧНІ РЕКОМЕНДАЦІЇ
## до лабораторної роботи
# "РОБОТА З БАЗАМИ ДАНИХ ЧЕРЕЗ ADO.NET"
### для студентів спеціальності "Комп'ютерні науки"
### .NET 8/9 + SQL Server Express

---

## 📚 ЗМІСТ

1. [Цілі та завдання лабораторної роботи](#1-цілі-та-завдання-лабораторної-роботи)
2. [Теоретичні відомості](#2-теоретичні-відомості)
3. [Структура лабораторної роботи](#3-структура-лабораторної-роботи)
4. [Практичні завдання](#4-практичні-завдання)
5. [Приклади коду](#5-приклади-коду)
6. [Корисні ресурси та документація](#6-корисні-ресурси-та-документація)
7. [Критерії оцінювання](#7-критерії-оцінювання)

---

## 1. ЦІЛІ ТА ЗАВДАННЯ ЛАБОРАТОРНОЇ РОБОТИ

### Мета роботи
Освоєння основ роботи з базами даних через ADO.NET у сучасному .NET середовищі, розуміння низькорівневого доступу до даних та його зв'язку з Entity Framework Core.

### Після виконання лабораторної роботи студенти зможуть:

1. ✅ Пояснити, що таке ADO.NET та як він пов'язаний з EF Core
2. ✅ Використовувати `Microsoft.Data.SqlClient` для підключення та виконання CRUD операцій
3. ✅ Застосовувати параметризовані команди для захисту від SQL-ін'єкцій
4. ✅ Працювати з транзакціями та рівнями ізоляції
5. ✅ Використовувати асинхронні операції з `CancellationToken`
6. ✅ Реалізувати пакетні операції через TVP та `SqlBulkCopy`
7. ✅ Вимірювати та порівнювати продуктивність різних підходів

---

## 2. ТЕОРЕТИЧНІ ВІДОМОСТІ

### 2.1. Що таке ADO.NET?

**ADO.NET** (Active Data Objects для .NET) — це фундаментальна технологія доступу до даних у екосистемі .NET від Microsoft. Вона надає низькорівневий, універсальний інтерфейс для взаємодії з базами даних через підключення, команди, рідери та набори даних.

> 📖 **Докладніше:** [ADO.NET Overview | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)

**Важливо розуміти:** ADO.NET — це база, на якій побудовані всі високорівневі фреймворки для роботи з даними, включаючи Entity Framework Core. Навіть у .NET 8/9 ADO.NET залишається основним рівнем для будь-яких операцій з базами даних.

### 2.2. Microsoft.Data.SqlClient vs System.Data.SqlClient

Починаючи з 2019 року, Microsoft перенесла розробку SQL Server провайдера з `System.Data.SqlClient` на `Microsoft.Data.SqlClient`. Новий пакет активно розвивається та підтримує всі сучасні функції SQL Server.

> 📖 **Міграція на новий провайдер:** [Introducing the new Microsoft.Data.SqlClient](https://devblogs.microsoft.com/dotnet/introducing-the-new-microsoftdatasqlclient/)

### 2.3. Основні компоненти ADO.NET

| Компонент | Призначення | Документація |
|-----------|-------------|--------------|
| **SqlConnection** | Встановлює з'єднання з базою даних | [SqlConnection Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection) |
| **SqlCommand** | Виконує SQL-оператори або збережені процедури | [SqlCommand Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcommand) |
| **SqlDataReader** | Читає дані рядок за рядком (forward-only, read-only) | [SqlDataReader Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqldatareader) |
| **SqlDataAdapter** | Керує від'єднаними даними через DataSet | [SqlDataAdapter Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqldataadapter) |
| **SqlTransaction** | Керує транзакціями | [SqlTransaction Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqltransaction) |

### Приклад базового використання:

```csharp
using (SqlConnection conn = new SqlConnection(connectionString))
{
    conn.Open();
    SqlCommand cmd = new SqlCommand("SELECT * FROM Students", conn);
    SqlDataReader reader = cmd.ExecuteReader();
    
    while (reader.Read())
    {
        Console.WriteLine($"{reader["Id"]}: {reader["Name"]}");
    }
}
```

### 2.4. Підключення та пул з'єднань

Відкриття з'єднань — дорога операція. ADO.NET автоматично використовує **Connection Pooling**, який повторно використовує з'єднання на основі ідентичних рядків підключення.

> 📖 **Детально про пулінг:** [SQL Server Connection Pooling](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling)

**Приклад рядка підключення для SQL Server Express:**
```
Server=localhost\SQLEXPRESS;
Database=LabDb;
Trusted_Connection=True;
TrustServerCertificate=True;
```

**Параметри пулінгу:**
- `Min Pool Size=0` (мінімальний розмір пулу)
- `Max Pool Size=100` (максимальний розмір пулу)
- `Pooling=true/false` (увімкнення/вимкнення пулінгу)

> 💡 **Порада:** Для тестування впливу пулінгу додайте `Pooling=false` до рядка підключення та порівняйте продуктивність.

### 2.5. Параметризовані команди (захист від SQL-ін'єкцій)

**🔴 КРИТИЧНО ВАЖЛИВО:** Завжди використовуйте параметри для передачі даних користувача в SQL-запити!

> 📖 **Про SQL-ін'єкції:** [SQL Injection Prevention Cheat Sheet | OWASP](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)

**❌ Неправильно (вразливо до SQL-ін'єкцій):**
```csharp
string sql = $"SELECT * FROM Users WHERE Name = '{userInput}'"; // НІКОЛИ ТАК НЕ РОБІТЬ!
```

**✅ Правильно (безпечно):**
```csharp
const string sql = "SELECT * FROM Users WHERE Name = @name";
cmd.Parameters.AddWithValue("@name", userInput);
```

> 📖 **Докладніше про параметри:** [Configuring Parameters and Parameter Data Types](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types)

### 2.6. Транзакції та рівні ізоляції

Транзакції забезпечують **ACID** властивості:
- **A**tomicity (Атомарність)
- **C**onsistency (Узгодженість)
- **I**solation (Ізоляція)
- **D**urability (Довговічність)

> 📖 **Детально про транзакції:** [Transactions and Concurrency](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/transactions-and-concurrency)

**Рівні ізоляції:**

| Рівень | Опис | Проблеми, які дозволяє |
|--------|------|------------------------|
| **ReadUncommitted** | Найнижчий рівень | Dirty reads, Non-repeatable reads, Phantom reads |
| **ReadCommitted** | За замовчуванням для SQL Server | Non-repeatable reads, Phantom reads |
| **RepeatableRead** | Блокує рядки до кінця транзакції | Phantom reads |
| **Snapshot** | Версіонування рядків | Немає |
| **Serializable** | Найвищий рівень | Немає |

> 📖 **Рівні ізоляції в SQL Server:** [Transaction Isolation Levels](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql)

### 2.7. Асинхронне програмування

Всі основні методи ADO.NET мають асинхронні версії з підтримкою `CancellationToken`:

```csharp
await conn.OpenAsync(cancellationToken);
await cmd.ExecuteReaderAsync(cancellationToken);
await reader.ReadAsync(cancellationToken);
```

> 📖 **Асинхронне програмування в ADO.NET:** [Asynchronous Programming](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/asynchronous-programming)

### 2.8. Пакетні операції

#### SqlBulkCopy
Найшвидший спосіб вставки великої кількості записів:

> 📖 **Документація SqlBulkCopy:** [SqlBulkCopy Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlbulkcopy)

#### Table-Valued Parameters (TVP)
Дозволяє передавати таблиці як параметри в збережені процедури:

> 📖 **Використання TVP:** [Table-Valued Parameters](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters)

---

## 3. СТРУКТУРА ЛАБОРАТОРНОЇ РОБОТИ

### 3.1. Підготовка середовища

#### Необхідне програмне забезпечення:

1. **.NET SDK 8 або 9**
   - 📥 [Завантажити .NET](https://dotnet.microsoft.com/download)
   - Перевірка: `dotnet --version`

2. **SQL Server Express (остання версія)**
   - 📥 [SQL Server Express 2022](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
   - Включає LocalDB для розробки

3. **SQL Server Management Studio (SSMS)**
   - 📥 [Завантажити SSMS](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

4. **IDE (на вибір):**
   - 📥 [Visual Studio 2022](https://visualstudio.microsoft.com/)
   - 📥 [Visual Studio Code](https://code.visualstudio.com/) + [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
   - 📥 [JetBrains Rider](https://www.jetbrains.com/rider/)

#### Створення проекту:

```bash
# Створити новий консольний проект
dotnet new console -n AdoNetLab

# Перейти до папки проекту
cd AdoNetLab

# Додати пакет Microsoft.Data.SqlClient
dotnet add package Microsoft.Data.SqlClient

# Додати пакет для конфігурації
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.UserSecrets
dotnet add package Microsoft.Extensions.Configuration.Json
```

### 3.2. Зберігання секретів

> ⚠️ **ВАЖЛИВО:** Ніколи не зберігайте паролі та рядки підключення в коді!

#### Використання User Secrets:

```bash
# Ініціалізація User Secrets
dotnet user-secrets init

# Встановлення connection string
dotnet user-secrets set "ConnectionStrings:LabDb" "Server=localhost\SQLEXPRESS;Database=LabDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

> 📖 **Докладніше про User Secrets:** [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

### 3.3. Структура проекту

```
AdoNetLab/
│
├── /migrations/              # SQL-скрипти міграцій
│   ├── 001_init.sql
│   ├── 002_add_indexes.sql
│   └── 003_seed_data.sql
│
├── /src/
│   ├── /Models/             # Моделі даних
│   │   └── Product.cs
│   ├── /Repositories/       # Репозиторії для роботи з БД
│   │   └── ProductRepository.cs
│   ├── /Services/           # Бізнес-логіка
│   │   └── MigrationRunner.cs
│   └── Program.cs           # Точка входу
│
├── appsettings.json         # Конфігурація
├── AdoNetLab.csproj        # Файл проекту
└── README.md               # Документація
```

---

## 4. ПРАКТИЧНІ ЗАВДАННЯ

### Частина A: Проектування схеми (вибір студента)

Оберіть одну з предметних областей та спроектуйте схему бази даних:

- 📚 **Бібліотека** (книги, читачі, видачі)
- 🎓 **Навчальний заклад** (студенти, курси, оцінки)  
- 🛒 **Інтернет-магазин** (товари, замовлення, клієнти)
- 🎫 **Система тікетів** (проекти, завдання, користувачі)
- ✍️ **Блог** (автори, статті, коментарі)

**Вимоги до схеми:**
- 3-5 таблиць з правильними первинними та зовнішніми ключами
- Мінімум 1-2 індекси для оптимізації
- Використання відповідних типів даних
- CHECK constraints для валідації
- DEFAULT значення де доцільно

### Частина B: Налаштування проекту

1. Створіть новий Console проект
2. Додайте `Microsoft.Data.SqlClient`
3. Налаштуйте User Secrets для зберігання connection string
4. Створіть структуру папок проекту

### Частина C: CRUD з параметрами

Реалізуйте репозиторій з методами:
- `CreateAsync` - створення запису
- `GetByIdAsync` - отримання за ID
- `GetPagedAsync` - пагінація з фільтрацією
- `UpdateAsync` - оновлення
- `DeleteAsync` - видалення

**Обов'язкові вимоги:**
- ✅ Всі SQL-запити параметризовані
- ✅ Використання `async/await`
- ✅ Правильна обробка `null` значень
- ✅ Використання `using` для автоматичного закриття з'єднань

### Частина D: Транзакції

Реалізуйте багатокрокову операцію в транзакції (наприклад, оформлення замовлення):
1. Створення заголовка замовлення
2. Додавання позицій замовлення
3. Оновлення залишків на складі
4. Обчислення загальної суми

**Продемонструйте:**
- Різні рівні ізоляції
- Rollback при помилці
- Правильне використання блокувань

### Частина E: Асинхронність та відміна

Створіть метод для виконання довгої операції:
- Використайте `WAITFOR DELAY` для симуляції
- Реалізуйте підтримку `CancellationToken`
- Обробіть `OperationCanceledException`

### Частина F: Пакетні операції (виберіть один варіант)

#### Варіант 1: Table-Valued Parameters (TVP)

1. Створіть користувацький табличний тип:
```sql
CREATE TYPE dbo.ProductTableType AS TABLE (
    Name nvarchar(100),
    Price decimal(18,2)
);
```

2. Створіть збережену процедуру:
```sql
CREATE PROCEDURE sp_BulkInsertProducts
    @Products dbo.ProductTableType READONLY
AS
BEGIN
    INSERT INTO Products (Name, Price)
    SELECT Name, Price FROM @Products;
END
```

3. Викличте з C# коду

#### Варіант 2: SqlBulkCopy

Реалізуйте пакетну вставку через `SqlBulkCopy` та порівняйте продуктивність зі звичайними INSERT

### Частина G: Міграції схеми

Реалізуйте систему версіонування схеми бази даних:

1. Створіть папку `/migrations` з SQL-скриптами
2. Реалізуйте `MigrationRunner` для автоматичного застосування
3. Забезпечте ідемпотентність скриптів
4. Ведіть таблицю версій з хешами файлів

---

## 5. ПРИКЛАДИ КОДУ

### 5.1. Параметризовані запити

```csharp
// ✅ ПРАВИЛЬНО - використання параметрів
public async Task<Product?> GetByIdAsync(int id)
{
    const string sql = @"
        SELECT Id, Name, Price, StockQuantity 
        FROM Products 
        WHERE Id = @id";
    
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    
    await using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", id);
    
    await using var reader = await cmd.ExecuteReaderAsync();
    
    if (await reader.ReadAsync())
    {
        return new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2),
            StockQuantity = reader.GetInt32(3)
        };
    }
    
    return null;
}
```

### 5.2. Транзакції з обробкою помилок

```csharp
public async Task<int> PlaceOrderAsync(int customerId, List<OrderItem> items)
{
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    
    await using var transaction = await conn.BeginTransactionAsync();
    
    try
    {
        // 1. Створюємо замовлення
        const string insertOrderSql = @"
            INSERT INTO Orders (CustomerId, OrderDate, Status) 
            VALUES (@customerId, GETDATE(), 'Pending');
            SELECT SCOPE_IDENTITY();";
        
        await using var orderCmd = new SqlCommand(insertOrderSql, conn, (SqlTransaction)transaction);
        orderCmd.Parameters.AddWithValue("@customerId", customerId);
        
        var orderId = Convert.ToInt32(await orderCmd.ExecuteScalarAsync());
        
        // 2. Додаємо позиції замовлення та оновлюємо залишки
        foreach (var item in items)
        {
            // Перевіряємо наявність товару
            const string checkStockSql = @"
                SELECT StockQuantity FROM Products WHERE Id = @productId";
            
            await using var checkCmd = new SqlCommand(checkStockSql, conn, (SqlTransaction)transaction);
            checkCmd.Parameters.AddWithValue("@productId", item.ProductId);
            
            var stock = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            
            if (stock < item.Quantity)
            {
                throw new InvalidOperationException($"Недостатньо товару {item.ProductId} на складі");
            }
            
            // Додаємо позицію
            const string insertItemSql = @"
                INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice) 
                VALUES (@orderId, @productId, @quantity, @price)";
            
            await using var itemCmd = new SqlCommand(insertItemSql, conn, (SqlTransaction)transaction);
            itemCmd.Parameters.AddWithValue("@orderId", orderId);
            itemCmd.Parameters.AddWithValue("@productId", item.ProductId);
            itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
            itemCmd.Parameters.AddWithValue("@price", item.UnitPrice);
            
            await itemCmd.ExecuteNonQueryAsync();
            
            // Оновлюємо залишки
            const string updateStockSql = @"
                UPDATE Products 
                SET StockQuantity = StockQuantity - @quantity 
                WHERE Id = @productId";
            
            await using var updateCmd = new SqlCommand(updateStockSql, conn, (SqlTransaction)transaction);
            updateCmd.Parameters.AddWithValue("@productId", item.ProductId);
            updateCmd.Parameters.AddWithValue("@quantity", item.Quantity);
            
            await updateCmd.ExecuteNonQueryAsync();
        }
        
        await transaction.CommitAsync();
        return orderId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 5.3. Асинхронні операції з CancellationToken

```csharp
public async Task<List<Product>> SearchProductsAsync(
    string searchTerm, 
    CancellationToken cancellationToken)
{
    const string sql = @"
        -- Симуляція довгої операції
        WAITFOR DELAY '00:00:03';
        
        SELECT Id, Name, Price, StockQuantity
        FROM Products
        WHERE Name LIKE @search
        ORDER BY Name";
    
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync(cancellationToken);
    
    await using var cmd = new SqlCommand(sql, conn);
    cmd.CommandTimeout = 30;
    cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");
    
    var products = new List<Product>();
    
    try
    {
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2),
                StockQuantity = reader.GetInt32(3)
            });
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Операцію було скасовано користувачем");
        throw;
    }
    
    return products;
}

// Використання:
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(2)); // Скасувати через 2 секунди

try
{
    var products = await SearchProductsAsync("laptop", cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Пошук було скасовано через таймаут");
}
```

### 5.4. SqlBulkCopy для масової вставки

```csharp
public async Task BulkInsertProductsAsync(List<Product> products)
{
    // Створюємо DataTable з такою ж структурою як таблиця Products
    var dataTable = new DataTable();
    dataTable.Columns.Add("Name", typeof(string));
    dataTable.Columns.Add("Description", typeof(string));
    dataTable.Columns.Add("Price", typeof(decimal));
    dataTable.Columns.Add("StockQuantity", typeof(int));
    
    // Заповнюємо DataTable даними
    foreach (var product in products)
    {
        dataTable.Rows.Add(
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity
        );
    }
    
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    
    using var bulkCopy = new SqlBulkCopy(conn)
    {
        DestinationTableName = "Products",
        BatchSize = 1000, // Розмір пакету
        BulkCopyTimeout = 60 // Таймаут в секундах
    };
    
    // Налаштовуємо відповідність колонок
    bulkCopy.ColumnMappings.Add("Name", "Name");
    bulkCopy.ColumnMappings.Add("Description", "Description");
    bulkCopy.ColumnMappings.Add("Price", "Price");
    bulkCopy.ColumnMappings.Add("StockQuantity", "StockQuantity");
    
    // Виконуємо масову вставку
    await bulkCopy.WriteToServerAsync(dataTable);
    
    Console.WriteLine($"Успішно вставлено {products.Count} записів");
}
```

### 5.5. Table-Valued Parameters (TVP)

```csharp
public async Task BulkInsertWithTVPAsync(List<Product> products)
{
    // Створюємо DataTable для TVP
    var tvp = new DataTable();
    tvp.Columns.Add("Name", typeof(string));
    tvp.Columns.Add("Price", typeof(decimal));
    
    foreach (var product in products)
    {
        tvp.Rows.Add(product.Name, product.Price);
    }
    
    await using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    
    await using var cmd = new SqlCommand("sp_BulkInsertProducts", conn);
    cmd.CommandType = CommandType.StoredProcedure;
    
    // Передаємо DataTable як параметр типу Structured
    var param = cmd.Parameters.AddWithValue("@Products", tvp);
    param.SqlDbType = SqlDbType.Structured;
    param.TypeName = "dbo.ProductTableType";
    
    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Вставлено {rowsAffected} записів через TVP");
}
```

### 5.6. Тестування Connection Pooling

```csharp
public async Task CompareConnectionPoolingPerformanceAsync()
{
    var stopwatch = new Stopwatch();
    
    // Тест З пулінгом
    stopwatch.Start();
    for (int i = 0; i < 100; i++)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        await using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync();
    }
    stopwatch.Stop();
    
    Console.WriteLine($"З пулінгом: {stopwatch.ElapsedMilliseconds} мс");
    
    // Тест БЕЗ пулінгу
    var noPoolConnectionString = _connectionString + ";Pooling=false";
    stopwatch.Restart();
    
    for (int i = 0; i < 100; i++)
    {
        await using var conn = new SqlConnection(noPoolConnectionString);
        await conn.OpenAsync();
        
        await using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync();
    }
    stopwatch.Stop();
    
    Console.WriteLine($"Без пулінгу: {stopwatch.ElapsedMilliseconds} мс");
}
```

---

## 6. КОРИСНІ РЕСУРСИ ТА ДОКУМЕНТАЦІЯ

### 📚 Офіційна документація Microsoft

#### ADO.NET та Microsoft.Data.SqlClient
- 📖 [ADO.NET Overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview) - загальний огляд технології
- 📖 [Microsoft.Data.SqlClient Namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient) - API reference
- 📖 [Migrating to Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) - міграція з System.Data.SqlClient
- 📖 [Connection Strings](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-strings-and-configuration-files) - налаштування підключення

#### Продуктивність та оптимізація
- 📖 [SQL Server Connection Pooling](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling) - детально про пулінг
- 📖 [SqlBulkCopy Performance](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/bulk-copy-operations-in-sql-server) - масові операції
- 📖 [Table-Valued Parameters](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters) - TVP

#### Безпека
- 📖 [SQL Injection Prevention](https://learn.microsoft.com/en-us/sql/relational-databases/security/sql-injection) - захист від SQL-ін'єкцій
- 📖 [Configuring Parameters](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/configuring-parameters-and-parameter-data-types) - правильне використання параметрів
- 🔒 [OWASP SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)

#### Транзакції та паралелізм
- 📖 [Transactions in ADO.NET](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/transactions-and-concurrency) - робота з транзакціями
- 📖 [Transaction Isolation Levels](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql) - рівні ізоляції
- 📖 [Deadlock Prevention](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide) - уникнення дедлоків

#### Асинхронне програмування
- 📖 [Asynchronous Programming in ADO.NET](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/asynchronous-programming) - async/await з БД
- 📖 [Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) - CancellationToken

### 📹 Відео-туторіали та курси

- 🎥 [ADO.NET Tutorial for Beginners](https://www.youtube.com/watch?v=aoFDyt8oG0k) - IAmTimCorey
- 🎥 [Raw SQL, SQL Injection, and ADO.NET](https://www.youtube.com/watch?v=8Jh2fq5HFQY) - Nick Chapsas
- 🎓 [Data Access in C# Fundamentals](https://www.pluralsight.com/courses/data-access-csharp-fundamentals) - Pluralsight

### 📝 Блоги та статті

- 📝 [ADO.NET Best Practices](https://www.c-sharpcorner.com/article/best-practices-for-using-ado-net/) - C# Corner
- 📝 [Performance Tips for ADO.NET](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-7/#ado-net) - .NET Blog
- 📝 [Understanding Connection Pooling](https://www.red-gate.com/simple-talk/development/dotnet-development/understanding-sql-server-connection-pooling/) - Red Gate

### 🛠️ Інструменти та бібліотеки

#### Альтернативи для роботи з БД
- 🔧 [Dapper](https://github.com/DapperLib/Dapper) - мікро-ORM, побудований на ADO.NET
- 🔧 [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) - повноцінний ORM
- 🔧 [RepoDB](https://github.com/mikependon/RepoDB) - гібридний ORM

#### Міграції БД
- 🔧 [DbUp](https://dbup.readthedocs.io/) - проста бібліотека для міграцій
- 🔧 [FluentMigrator](https://github.com/fluentmigrator/fluentmigrator) - міграції через C# код
- 🔧 [RoundhousE](https://github.com/chucknorris/roundhouse) - інструмент версіонування БД

#### Профілювання та діагностика
- 🔍 [SQL Server Profiler](https://learn.microsoft.com/en-us/sql/tools/sql-server-profiler/sql-server-profiler) - аналіз запитів
- 🔍 [MiniProfiler](https://miniprofiler.com/) - профілювання ADO.NET
- 🔍 [Glimpse](https://github.com/Glimpse/Glimpse) - діагностика веб-додатків

### 💡 Додаткові ресурси

#### Патерни та архітектура
- 📐 [Repository Pattern with ADO.NET](https://www.c-sharpcorner.com/article/repository-pattern-using-ado-net/) 
- 📐 [Unit of Work Pattern](https://www.c-sharpcorner.com/article/unit-of-work-in-repository-pattern/)
- 📐 [CQRS with ADO.NET](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

#### Тестування
- 🧪 [Testing with In-Memory Databases](https://learn.microsoft.com/en-us/ef/core/testing/)
- 🧪 [Mocking SqlConnection](https://stackoverflow.com/questions/1085097/how-to-unit-test-a-method-that-uses-sqlconnection-and-sqlcommand)

---

## 7. КРИТЕРІЇ ОЦІНЮВАННЯ

### Розподіл балів (максимум 20 балів)

| Критерій | Бали | Деталі оцінювання |
|----------|------|-------------------|
| **Міграції схеми** | 6 | • Таблиця версій (1б)<br>• Ідемпотентні скрипти (2б)<br>• Робочий migration runner (2б)<br>• Демонстрація змін між версіями (1б) |
| **Коректність інтеграції** | 6 | • Параметризовані команди (2б)<br>• Правильні типи даних (1б)<br>• Повний CRUD (2б)<br>• Обробка null значень (1б) |
| **Транзакції/Асинхронність** | 4 | • Багатокрокова транзакція (2б)<br>• CancellationToken (1б)<br>• Різні рівні ізоляції (1б) |
| **Якість коду та документація** | 4 | • Чиста архітектура (1б)<br>• README з інструкціями (1б)<br>• Коментарі в коді (1б)<br>• Звіт REPORT.md (1б) |

### Бонусні бали (до +3)

- ✨ Реалізація обох варіантів пакетних операцій (TVP + SqlBulkCopy) - **+1 бал**
- ✨ Benchmarking з вимірюванням продуктивності - **+1 бал**
- ✨ Unit тести для репозиторіїв - **+1 бал**

### Штрафні бали

- ❌ SQL-ін'єкції (конкатенація рядків замість параметрів) - **-3 бали**
- ❌ Відсутність using для з'єднань - **-2 бали**
- ❌ Зберігання паролів у коді - **-2 бали**
- ❌ Відсутність обробки помилок - **-1 бал**

---

## 📋 КОНТРОЛЬНИЙ СПИСОК

Перед здачею лабораторної роботи переконайтеся, що виконано:

### Обов'язкові пункти
- [ ] Створено схему БД з 3-5 таблицями
- [ ] Реалізовано migration runner
- [ ] Всі SQL-запити параметризовані
- [ ] Реалізовано повний CRUD для однієї сутності
- [ ] Створено багатокрокову транзакцію
- [ ] Додано підтримку CancellationToken
- [ ] Реалізовано один з варіантів пакетних операцій
- [ ] Написано README.md з інструкціями запуску
- [ ] Створено REPORT.md зі звітом про виконану роботу

### Додаткові пункти
- [ ] Протестовано різні рівні ізоляції транзакцій
- [ ] Виміряно продуктивність з/без connection pooling
- [ ] Додано логування операцій
- [ ] Реалізовано обробку помилок
- [ ] Код розділено на шари (Models, Repositories, Services)

---

## 🎯 ВИСНОВОК

Ця лабораторна робота дає фундаментальне розуміння роботи з базами даних у .NET через ADO.NET. 

**Отримані навички допоможуть вам:**
- ✅ Краще розуміти, що відбувається «під капотом» Entity Framework Core
- ✅ Оптимізувати критичні за продуктивністю ділянки коду
- ✅ Працювати з legacy-системами, де EF Core недоступний
- ✅ Реалізовувати специфічні сценарії, які складно виразити через ORM
- ✅ Писати безпечний код, захищений від SQL-ін'єкцій

**Наступні кроки:**
1. Виконання лабораторної роботи з Entity Framework Core
2. Порівняння підходів ADO.NET vs EF Core
3. Вивчення патернів Repository та Unit of Work
4. Знайомство з мікро-ORM (Dapper)

---

**Успіхів у виконанні лабораторної роботи! 🚀**

*При виникненні питань звертайтесь до викладача або використовуйте ресурси з розділу документації.*
