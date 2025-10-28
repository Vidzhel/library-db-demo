# DbDemo Benchmarks

Performance benchmarks for ADO.NET operations using BenchmarkDotNet.

## Prerequisites

- .NET 9.0 SDK
- SQL Server running on `localhost:1453`
- Database initialized with migrations

## Running Benchmarks

Always run benchmarks in **Release** configuration for accurate results.

### Run All Benchmarks

```bash
cd benchmarks/DbDemo.Benchmarks
dotnet run --configuration Release -- all
```

### Run Specific Benchmarks

```bash
# Bulk insert benchmarks only
dotnet run --configuration Release -- bulk

# Connection pooling benchmarks only
dotnet run --configuration Release -- pooling
```

## Benchmark Suites

### 1. Bulk Insert Benchmarks

Compares three bulk insert approaches:
- **Batched INSERTs**: Multiple INSERT statements in batches
- **TVP (Table-Valued Parameters)**: Stored procedure with structured data
- **SqlBulkCopy**: Native bulk insert API

**Dataset Sizes**: 50 and 100 records

**Expected Results**:
- SqlBulkCopy: Fastest (~28ms for 100 records)
- TVP: Middle ground (~85ms for 100 records)
- Batched INSERTs: Slowest (~248ms for 100 records)

### 2. Connection Pooling Benchmarks

Compares pooled vs. non-pooled connections:
- **Single query**: One connection open/close
- **10 sequential queries**: Multiple sequential connections
- **50 open/close operations**: Connection overhead only

**Expected Results**:
- Pooled connections are 3-20x faster depending on scenario
- Most dramatic difference in open/close operations

## Performance Notes

- Benchmarks use `SimpleJob` with 1 warm-up and 3 iterations
- Typical execution time: 3-5 minutes total
- Results saved in `BenchmarkDotNet.Artifacts/results/`
- Memory diagnostics included (allocations, GC counts)

## Configuration

Database connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "LibraryDb": "Server=localhost,1453;Database=LibraryDb;..."
  }
}
```

## Interpreting Results

```
| Method              | Mean      | Error    | StdDev   | Rank | Allocated |
|-------------------- |----------:|---------:|---------:|-----:|----------:|
| SqlBulkCopy_100     |  28.45 ms | 0.42 ms  | 0.39 ms  |    1 |   2.3 KB  |
| TvpInsert_100       |  85.32 ms | 1.21 ms  | 1.13 ms  |    2 |   4.7 KB  |
| BatchedInserts_100  | 247.81 ms | 3.82 ms  | 3.57 ms  |    3 |   8.2 KB  |
```

- **Mean**: Average execution time
- **Error**: Standard error (smaller = more consistent)
- **StdDev**: Standard deviation (lower = more predictable)
- **Rank**: Performance ranking (1 = fastest)
- **Allocated**: Memory allocated per operation

## Troubleshooting

**Build Errors**: Ensure you're running from the project directory and using Release configuration.

**Database Errors**: Verify SQL Server is running and migrations are applied.

**Slow Execution**: This is expected - benchmarks take 3-5 minutes for statistical accuracy.

## Documentation

See `docs/18-benchmarking.md` for comprehensive documentation including:
- Detailed explanation of each benchmark
- Best practices for benchmarking
- Common pitfalls to avoid
- How to interpret results
