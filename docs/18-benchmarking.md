# Chapter 18: Performance Benchmarking with BenchmarkDotNet

## Table of Contents
- [Introduction](#introduction)
- [What is BenchmarkDotNet?](#what-is-benchmarkdotnet)
- [Project Setup](#project-setup)
- [Bulk Insert Benchmarks](#bulk-insert-benchmarks)
- [Connection Pooling Benchmarks](#connection-pooling-benchmarks)
- [Running Benchmarks](#running-benchmarks)
- [Interpreting Results](#interpreting-results)
- [Best Practices](#best-practices)
- [Common Pitfalls](#common-pitfalls)

## Introduction

Performance benchmarking is essential for making data-driven decisions about database operations. This chapter demonstrates how to use BenchmarkDotNet to accurately measure and compare different ADO.NET approaches.

**What We'll Benchmark:**
- Bulk insert methods (Batched, TVP, SqlBulkCopy) with 50 and 100 record datasets
- Connection pooling vs. non-pooled connections
- Sequential operations and connection overhead

**Why BenchmarkDotNet?**
- Industry-standard benchmarking framework
- Accurate micro-benchmarking with statistical analysis
- Automatic warm-up and iteration management
- Memory allocation diagnostics
- Cross-platform support

**Performance Optimizations:**
- Uses `SimpleJob` for faster execution (1 warm-up, 3 iterations vs. default 3 warm-ups, 15 iterations)
- Focused on practical dataset sizes (50, 100 records)
- Synchronous IterationSetup to avoid BenchmarkDotNet async compatibility issues
- Total execution time: ~3-5 minutes instead of 15-20 minutes

## What is BenchmarkDotNet?

BenchmarkDotNet is a powerful .NET library for benchmarking code with high accuracy. It handles many complexities of micro-benchmarking:

### Key Features

1. **Accurate Measurements**
   - Multiple warm-up iterations to eliminate JIT effects
   - Statistical analysis with median, mean, standard deviation
   - Outlier detection and removal

2. **Memory Diagnostics**
   - GC collection counts (Gen 0, Gen 1, Gen 2)
   - Allocated memory per operation
   - Memory pressure analysis

3. **Rich Reporting**
   - Console output with formatted tables
   - HTML reports with charts
   - Markdown export for documentation
   - CSV/JSON for further analysis

4. **Flexible Configuration**
   - Custom iterations and warm-up counts
   - Different runtime configurations
   - Job parameterization
   - Category filtering

### How It Works

```
1. Pilot Stage: Determine optimal iteration count
2. Warm-up: Execute benchmarks to eliminate cold-start effects
3. Actual Workload: Run multiple iterations
4. Statistical Analysis: Calculate median, mean, std dev
5. Report Generation: Format and display results
```

## Project Setup

### 1. Create Benchmark Project

```bash
# Create new console project
dotnet new console -n DbDemo.Benchmarks -o benchmarks/DbDemo.Benchmarks

# Add BenchmarkDotNet package
cd benchmarks/DbDemo.Benchmarks
dotnet add package BenchmarkDotNet
dotnet add package Microsoft.Data.SqlClient
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json

# Reference main project
dotnet add reference ../../src/DbDemo.ConsoleApp/DbDemo.ConsoleApp.csproj
```

### 2. Project Structure

```
benchmarks/
└── DbDemo.Benchmarks/
    ├── DbDemo.Benchmarks.csproj
    ├── Program.cs
    ├── BulkInsertBenchmarks.cs
    ├── ConnectionPoolingBenchmarks.cs
    └── appsettings.json
```

### 3. Configuration File

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "LibraryDb": "Server=localhost,1453;Database=LibraryDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

## Bulk Insert Benchmarks

### Overview

Compare three bulk insert approaches with practical dataset sizes:
- Batched INSERT statements (20 records per batch)
- Table-Valued Parameters (TVP) with stored procedure
- SqlBulkCopy (native bulk insert API)

**Dataset Sizes:** 50 and 100 records

**Why these sizes?** They demonstrate clear performance differences while keeping execution time reasonable. Individual INSERTs are excluded (too slow for benchmarking) and 500+ records are excluded (too time-consuming for frequent testing).

### Benchmark Class Structure

```csharp
[MemoryDiagnoser]                          // Track memory allocations
[Orderer(SummaryOrderPolicy.FastestToSlowest)]  // Sort results
[RankColumn]                               // Add rank column
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]  // Fast execution
public class BulkInsertBenchmarks
{
    private BulkBookImporter _bulkImporter;
    private TvpBookImporter _tvpImporter;
    private List<Book> _books50;
    private List<Book> _books100;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Initialize once before all benchmarks
        // Load configuration, create importers, generate test data
        // Pre-generate test data to avoid measuring data generation
        _books50 = BulkBookImporter.GenerateSampleBooks(50, _categoryId);
        _books100 = BulkBookImporter.GenerateSampleBooks(100, _categoryId);
        CleanupBooks();  // Initial cleanup
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // IMPORTANT: Must be synchronous (BenchmarkDotNet doesn't support async)
        // Clean up database for consistent state before each iteration
        CleanupBooks();
    }

    [Benchmark(Description = "SqlBulkCopy - 100 records", Baseline = true)]
    [BenchmarkCategory("100 Records")]
    public async Task<int> SqlBulkCopy_100Records()
    {
        var (count, _) = await _bulkImporter.BulkInsertWithSqlBulkCopyAsync(_books100);
        return count;
    }
}
```

**Key Changes from Standard Setup:**
- `[SimpleJob]` reduces execution time from 15-20 minutes to 3-5 minutes
- `IterationSetup` is synchronous (BenchmarkDotNet compatibility requirement)
- Pre-generated test data in `GlobalSetup` (don't measure data generation)
- `[Baseline = true]` on SqlBulkCopy to show relative performance

### Benchmark Attributes

**`[MemoryDiagnoser]`**
- Tracks memory allocations and GC collections
- Shows allocated bytes per operation
- Identifies memory-intensive operations

**`[Orderer(SummaryOrderPolicy.FastestToSlowest)]`**
- Sorts results by performance
- Makes it easy to identify fastest/slowest methods

**`[RankColumn]`**
- Adds ranking (1st, 2nd, 3rd, etc.)
- Quick visual comparison

**`[BenchmarkCategory("Category")]`**
- Groups related benchmarks
- Enables selective execution
- Organizes results

### Setup Methods

**`[GlobalSetup]`**
- Executes once before all benchmarks
- Use for expensive initialization:
  - Database connections
  - Test data generation
  - Configuration loading
- **NOT included in benchmark timings**

**`[IterationSetup]`**
- Executes before each benchmark iteration
- Use for per-iteration preparation:
  - Database cleanup
  - Resetting state
- **IMPORTANT:** Must be synchronous - BenchmarkDotNet doesn't support `async Task` in setup methods
- Use synchronous ADO.NET methods (`connection.Open()` not `OpenAsync()`)
- **Included in benchmark timings** (minimal impact if fast)

**`[IterationCleanup]`**
- Executes after each iteration
- Use for cleanup that shouldn't be timed

**`[GlobalCleanup]`**
- Executes once after all benchmarks
- Use for final cleanup

### Benchmark Categories

We organize benchmarks by dataset size:

**50 Records** - Practical small dataset
```csharp
[BenchmarkCategory("50 Records")]
public async Task<int> BatchedInserts_50Records()
public async Task<int> TvpInsert_50Records()
public async Task<int> SqlBulkCopy_50Records()
```

**100 Records** - Practical medium dataset
```csharp
[BenchmarkCategory("100 Records")]
public async Task<int> BatchedInserts_100Records()
public async Task<int> TvpInsert_100Records()
public async Task<int> SqlBulkCopy_100Records()
```

**Total:** 6 benchmarks (3 methods × 2 dataset sizes)

### Expected Results

Based on typical performance characteristics with optimized benchmarks:

| Method           | 50 Records | 100 Records | Best For              |
|------------------|------------|-------------|-----------------------|
| Batched          | ~125ms     | ~250ms      | 10-50 records         |
| TVP              | ~40ms      | ~85ms       | 50-1,000 + logic      |
| SqlBulkCopy      | ~14ms      | ~28ms       | >100 pure bulk        |

**Key Insights:**
- SqlBulkCopy is consistently fastest (~3x faster than TVP)
- TVP is ~3x faster than Batched INSERTs
- Performance differences become more pronounced with larger datasets
- Individual INSERTs excluded (>10x slower, not practical for benchmarking)

## Connection Pooling Benchmarks

### Overview

Compare performance of pooled vs. non-pooled connections across practical scenarios:
- Single query execution
- Multiple sequential queries (10 iterations)
- Connection open/close operations (50 iterations)

**Simplified for Speed:** Removed concurrent queries and pool size comparison benchmarks to reduce execution time from ~5 minutes to ~2 minutes.

### Benchmark Structure

```csharp
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class ConnectionPoolingBenchmarks
{
    private string _pooledConnectionString;
    private string _nonPooledConnectionString;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Pooled: "...;Pooling=True;Min Pool Size=5;Max Pool Size=100;"
        // Non-pooled: "...;Pooling=False;"
    }

    [Benchmark(Description = "10 sequential queries - Pooled")]
    public async Task<int> TenSequentialQueries_Pooled()
    {
        // Open/close connection 10 times (reuses from pool)
    }

    [Benchmark(Description = "10 sequential queries - Non-pooled")]
    public async Task<int> TenSequentialQueries_NonPooled()
    {
        // Open/close connection 10 times (creates new each time)
    }
}
```

### Connection String Configurations

**Pooled Connection:**
```csharp
"Server=...;Pooling=True;Min Pool Size=5;Max Pool Size=100;"
```
- Maintains 5 connections ready
- Can grow to 100 connections
- Reuses existing connections

**Non-Pooled Connection:**
```csharp
"Server=...;Pooling=False;"
```
- Creates new connection each time
- No connection reuse
- Higher overhead

**Small Pool:**
```csharp
"Server=...;Pooling=True;Min Pool Size=1;Max Pool Size=10;"
```

**Large Pool:**
```csharp
"Server=...;Pooling=True;Min Pool Size=10;Max Pool Size=200;"
```

### Benchmark Scenarios

#### 1. Single Query
Measures overhead of single operation:
```csharp
await using var connection = new SqlConnection(_pooledConnectionString);
await connection.OpenAsync();
await using var command = new SqlCommand("SELECT COUNT(*) FROM Books", connection);
return (int)await command.ExecuteScalarAsync()!;
```

**Expected:** Pooled is 2-3x faster due to avoided TCP handshake

#### 2. Multiple Sequential Queries
Opens/closes connection 10 times sequentially:
```csharp
for (int i = 0; i < 10; i++)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    // Execute query
}
```

**Expected:** Pooled is 5-10x faster due to connection reuse

#### 3. Open/Close Operations
Pure connection overhead (50 iterations):
```csharp
for (int i = 0; i < 50; i++)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    // No query execution
}
```

**Expected:** Pooled is 10-20x faster (most dramatic difference)

### Expected Results

**Total:** 6 benchmarks (3 scenarios × 2 connection types)

| Scenario                  | Pooled | Non-Pooled | Speedup |
|---------------------------|--------|------------|---------|
| Single query              | ~2ms   | ~6ms       | 3x      |
| 10 sequential queries     | ~18ms  | ~120ms     | 6.7x    |
| 50 open/close             | ~90ms  | ~1,500ms   | 16.7x   |

**Key Insights:**
- Pooling provides dramatic benefits for repeated operations
- Open/close overhead is most expensive without pooling (~16x faster with pooling)
- Benefits compound with multiple sequential operations
- Default pool settings (Min=0, Max=100) work well for most cases

**Removed Benchmarks (for speed):**
- Concurrent operations comparison (pools still help, but adds 2+ minutes)
- Different pool size comparisons (marginal differences, adds 3+ minutes)

## Running Benchmarks

### Command Line Usage

**Run All Benchmarks:**
```bash
cd benchmarks/DbDemo.Benchmarks
dotnet run --configuration Release -- all
```

**Run Specific Benchmark:**
```bash
# Bulk insert benchmarks only
dotnet run --configuration Release -- bulk

# Connection pooling benchmarks only
dotnet run --configuration Release -- pooling
```

**IMPORTANT:** Always use `--configuration Release` for accurate results. Debug builds include overhead that skews measurements.

### BenchmarkDotNet CLI Arguments

**Filter by category:**
```bash
dotnet run -c Release -- bulk --filter *100Records*
```

**Export results:**
```bash
dotnet run -c Release -- bulk --exporters html,markdown,csv
```

**Custom job configuration:**
```bash
dotnet run -c Release -- bulk --job short  # Faster, less accurate
dotnet run -c Release -- bulk --job long   # Slower, more accurate
```

### Execution Flow

1. **Compilation:** BenchmarkDotNet recompiles in Release mode
2. **Pilot Stage:** Determines iteration count
3. **Warm-up:** 1 iteration (with SimpleJob) to JIT compile code
4. **Actual Workload:** 3 iterations (with SimpleJob, vs. default 15-20)
5. **Analysis:** Statistical calculation
6. **Reporting:** Display results

**Typical execution time with SimpleJob:**
- Bulk Insert Benchmarks: ~2-3 minutes (6 benchmarks)
- Connection Pooling Benchmarks: ~1-2 minutes (6 benchmarks)
- All Benchmarks: ~3-5 minutes total

**Note:** Default job would take 15-20 minutes. SimpleJob significantly reduces execution time while maintaining statistical validity for database operations (high consistency, low variance).

## Interpreting Results

### Console Output

BenchmarkDotNet produces detailed console output:

```
// * Summary *

BenchmarkDotNet v0.14.0, Ubuntu 22.04.3 LTS (Jammy Jellyfish)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0, X64 RyuJIT AVX2
  Job-ABCDEF : .NET 9.0.0, X64 RyuJIT AVX2 (SimpleJob: IterationCount=3, WarmupCount=1)

| Method                        | Mean      | Error    | StdDev   | Median    | Rank | Allocated |
|------------------------------ |----------:|---------:|---------:|----------:|-----:|----------:|
| SqlBulkCopy_100Records        |  28.45 ms | 0.42 ms  | 0.39 ms  |  28.32 ms |    1 |   2.3 KB  |
| TvpInsert_100Records          |  85.32 ms | 1.21 ms  | 1.13 ms  |  84.89 ms |    2 |   4.7 KB  |
| BatchedInserts_100Records     | 247.81 ms | 3.82 ms  | 3.57 ms  | 246.92 ms |    3 |   8.2 KB  |
```

### Understanding Columns

**Mean:**
- Average execution time across all iterations
- Primary metric for comparison

**Error:**
- Standard error of the mean
- Smaller = more consistent

**StdDev (Standard Deviation):**
- Variability of measurements
- Lower = more predictable performance

**Median:**
- Middle value when sorted
- Less affected by outliers than mean

**Rank:**
- Relative performance ranking
- 1 = fastest

**Allocated:**
- Memory allocated per operation
- Includes GC Gen0/Gen1/Gen2 counts
- Lower = more memory efficient

### Statistical Significance

BenchmarkDotNet performs statistical analysis:

- **If difference > 3x standard error:** Statistically significant
- **If difference < standard error:** Not significantly different
- **Outliers:** Automatically detected and can be removed

### Memory Diagnostics

When using `[MemoryDiagnoser]`:

```
| Method            | Allocated | Gen0  | Gen1 | Gen2 |
|------------------ |----------:|------:|-----:|-----:|
| SqlBulkCopy       |   2.3 KB  | 0.001 |    - |    - |
| TvpInsert         |   4.7 KB  | 0.002 |    - |    - |
| BatchedInserts    |   8.2 KB  | 0.004 |    - |    - |
| IndividualInserts |  12.5 KB  | 0.006 |    - |    - |
```

**Gen0/Gen1/Gen2:**
- Number of garbage collections per 1000 operations
- Gen0: Fast, short-lived objects
- Gen1: Medium-lived objects
- Gen2: Long-lived objects (expensive to collect)

**Lower is better** for both allocation and GC counts.

### Comparing Results

**Performance Improvement Calculation:**
```
Speedup = Baseline Time / Optimized Time

Example:
Individual INSERTs: 982.14 ms
SqlBulkCopy:         28.45 ms
Speedup: 982.14 / 28.45 = 34.5x faster
```

**Memory Improvement:**
```
Memory Reduction = (Baseline - Optimized) / Baseline * 100%

Example:
Individual INSERTs: 12.5 KB
SqlBulkCopy:         2.3 KB
Reduction: (12.5 - 2.3) / 12.5 * 100% = 81.6% less memory
```

## Best Practices

### 1. Always Use Release Configuration

```bash
# ✅ CORRECT
dotnet run --configuration Release -- bulk

# ❌ WRONG - Debug builds have overhead
dotnet run -- bulk
```

**Why:** Debug builds include:
- Debug symbols
- Disabled optimizations
- Additional runtime checks
- 2-10x slower than Release

### 2. Let BenchmarkDotNet Control Iterations

```csharp
// ✅ CORRECT - Let BenchmarkDotNet decide
[Benchmark]
public async Task<int> MyBenchmark()
{
    return await DoWork();
}

// ❌ WRONG - Manual iterations skew results
[Benchmark]
public async Task<int> MyBenchmark()
{
    for (int i = 0; i < 100; i++)  // DON'T DO THIS
    {
        await DoWork();
    }
}
```

### 3. Use Consistent Database State

```csharp
[IterationSetup]
public async Task IterationSetup()
{
    // Clean database before each iteration
    await CleanupBooksAsync();
}
```

**Why:** Ensures each benchmark starts with same data volume and index state.

### 4. Minimize Setup Overhead

```csharp
// ✅ CORRECT - Expensive work in GlobalSetup
[GlobalSetup]
public async Task GlobalSetup()
{
    _books = GenerateSampleBooks(1000);  // Once
}

[Benchmark]
public async Task MyBenchmark()
{
    return await Insert(_books);
}

// ❌ WRONG - Expensive work in benchmark
[Benchmark]
public async Task MyBenchmark()
{
    var books = GenerateSampleBooks(1000);  // Every iteration!
    return await Insert(books);
}
```

### 5. Return Values from Benchmarks

```csharp
// ✅ CORRECT - Return value prevents dead code elimination
[Benchmark]
public async Task<int> MyBenchmark()
{
    return await DoWork();
}

// ❌ WRONG - Compiler might optimize away
[Benchmark]
public async Task MyBenchmark()
{
    await DoWork();  // Result unused
}
```

### 6. Use MemoryDiagnoser for Full Picture

```csharp
[MemoryDiagnoser]  // Always add this
public class MyBenchmarks
{
    // Shows both time AND memory impact
}
```

### 7. Organize with Categories

```csharp
[BenchmarkCategory("Small")]
public async Task Small() { }

[BenchmarkCategory("Large")]
public async Task Large() { }
```

Run specific category:
```bash
dotnet run -c Release -- bulk --filter *Small*
```

### 8. Close Database Connections

```csharp
// ✅ CORRECT - await using ensures disposal
[Benchmark]
public async Task<int> MyBenchmark()
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    // Work
    return result;
}
```

### 9. Test Realistic Workloads

```csharp
// ✅ CORRECT - Test realistic data volumes
_books100 = GenerateSampleBooks(100);
_books1000 = GenerateSampleBooks(1000);

// ❌ WRONG - Unrealistic sizes
_books5 = GenerateSampleBooks(5);  // Too small to show differences
_books1000000 = GenerateSampleBooks(1000000);  // Impractical for benchmarking
```

### 10. Document Baseline Configuration

```csharp
// Document environment in comments or docs
// CPU: Intel Xeon 2.80GHz
// RAM: 16GB
// SQL Server: 2022 Developer Edition
// Network: localhost (no network latency)
```

## Common Pitfalls

### 1. Running in Debug Mode

**Problem:**
```bash
dotnet run -- bulk  # Defaults to Debug!
```

**Solution:**
```bash
dotnet run --configuration Release -- bulk
```

**Impact:** 2-10x slower measurements, unusable for comparison.

### 2. Using Async IterationSetup

**Problem:**
```csharp
[IterationSetup]
public async Task IterationSetup()  // ❌ BenchmarkDotNet doesn't support this!
{
    await CleanupBooksAsync();
}
```

**Error:**
```
error CS0407: 'Task BulkInsertBenchmarks.IterationSetup()' has the wrong return type
```

**Solution:**
```csharp
[IterationSetup]
public void IterationSetup()  // ✅ Must be synchronous
{
    CleanupBooks();  // Use synchronous ADO.NET methods
}

private void CleanupBooks()
{
    using var connection = new SqlConnection(_connectionString);
    connection.Open();  // Not OpenAsync()
    using var command = new SqlCommand("DELETE FROM Books", connection);
    command.ExecuteNonQuery();  // Not ExecuteNonQueryAsync()
}
```

**Impact:** Build failure in BenchmarkDotNet's auto-generated code. Setup/cleanup methods must be synchronous.

### 3. Not Cleaning Database Between Iterations

**Problem:**
```csharp
[Benchmark]
public async Task Insert()
{
    await _importer.Insert(_books);
    // Database keeps growing every iteration!
}
```

**Solution:**
```csharp
[IterationSetup]
public void IterationSetup()
{
    CleanupBooks();  // Reset state before each iteration
}
```

**Impact:** Later iterations slower due to larger tables and fragmented indexes.

### 4. Including Setup in Benchmark

**Problem:**
```csharp
[Benchmark]
public async Task Insert()
{
    var books = GenerateSampleBooks(1000);  // Expensive!
    await _importer.Insert(books);
}
```

**Solution:**
```csharp
[GlobalSetup]
public void Setup()
{
    _books = GenerateSampleBooks(1000);
}

[Benchmark]
public async Task Insert()
{
    await _importer.Insert(_books);
}
```

**Impact:** Measures data generation + insertion instead of just insertion.

### 5. Not Returning Values

**Problem:**
```csharp
[Benchmark]
public async Task Insert()
{
    var count = await _importer.Insert(_books);
    // count unused - might be optimized away
}
```

**Solution:**
```csharp
[Benchmark]
public async Task<int> Insert()
{
    return await _importer.Insert(_books);  // Return forces execution
}
```

**Impact:** Dead code elimination might skip actual work.

### 6. Micro-Benchmarking Too Small Operations

**Problem:**
```csharp
[Benchmark]
public int AddNumbers()
{
    return 1 + 2;  // Too fast to measure accurately
}
```

**Solution:** Benchmark operations that take at least 100-1000 nanoseconds. For database operations, this is rarely an issue.

**Impact:** High measurement error due to timer resolution.

### 7. Ignoring Warm-up

**Problem:** Looking at first few iterations which include JIT overhead.

**Solution:** BenchmarkDotNet handles this automatically with warm-up phase. Never skip warm-up (but SimpleJob reduces warm-ups from 3 to 1 for speed).

### 8. Not Using MemoryDiagnoser

**Problem:** Only measuring time, missing memory issues.

**Solution:**
```csharp
[MemoryDiagnoser]  // Always add
public class MyBenchmarks { }
```

**Impact:** Miss memory leaks or excessive allocations.

### 9. Comparing Across Different Machines

**Problem:** Comparing results from different environments.

**Solution:** Always benchmark on target hardware or document environment. Performance characteristics vary by CPU, RAM, SQL Server version, etc.

### 10. Network Latency Not Considered

**Problem:** Benchmarking against localhost doesn't reflect production network latency.

**Solution:** Document that benchmarks are against localhost. Add 1-10ms per round-trip for network scenarios.

### 11. Forgetting Connection Pooling

**Problem:** Disabling connection pooling without realizing it.

**Solution:** Verify connection string includes `Pooling=True` (default) for realistic benchmarks.

## Conclusion

BenchmarkDotNet provides scientific accuracy for performance measurement:

✅ **Use for:**
- Comparing different implementations
- Validating optimization claims
- Making data-driven architecture decisions
- Detecting performance regressions

❌ **Don't use for:**
- Production monitoring (use APM tools)
- Real-time performance tracking
- Benchmarking in CI/CD (too slow)

**Key Takeaways:**
1. Always use Release configuration
2. Let BenchmarkDotNet control iterations (or use SimpleJob for faster testing)
3. **IterationSetup must be synchronous** - BenchmarkDotNet doesn't support async
4. Clean database state between iterations
5. Return values from benchmarks to prevent dead code elimination
6. Use MemoryDiagnoser for complete picture
7. Test realistic workloads (avoid trivially small or impractically large datasets)
8. Document environment and baseline

**Performance Hierarchy (from our benchmarks):**
1. **SqlBulkCopy** - Fastest for bulk operations (~28ms for 100 records)
2. **TVP** - Best balance of speed + logic (~85ms for 100 records)
3. **Batched INSERTs** - Good for small-medium datasets (~248ms for 100 records)
4. **Individual INSERTs** - Avoid for bulk operations (>10x slower, not included in benchmarks)

**Connection Pooling:** Always enable for production. Provides 3-20x performance improvement depending on workload.

**Optimization Trade-offs:**
- SimpleJob reduces execution time from 15-20 minutes to 3-5 minutes
- Fewer iterations (3 vs. 15-20) still provides statistical validity for database ops
- Focused dataset sizes (50, 100 records) balance demonstration value with execution time

---

**Next Steps:**
- Run benchmarks in your environment
- Compare results with these baselines
- Use data to choose optimal approach for your workload
- Monitor production performance to validate benchmark conclusions
