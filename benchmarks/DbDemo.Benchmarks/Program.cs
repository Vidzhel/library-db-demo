using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DbDemo.Benchmarks;

namespace DbDemo.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("DbDemo - BenchmarkDotNet Performance Tests");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        if (args.Length == 0 || args[0] == "--help")
        {
            PrintUsage();
            return;
        }

        switch (args[0].ToLower())
        {
            case "bulk":
                Console.WriteLine("Running Bulk Insert Benchmarks...");
                Console.WriteLine();
                BenchmarkRunner.Run<BulkInsertBenchmarks>();
                break;

            case "pooling":
                Console.WriteLine("Running Connection Pooling Benchmarks...");
                Console.WriteLine();
                BenchmarkRunner.Run<ConnectionPoolingBenchmarks>();
                break;

            case "all":
                Console.WriteLine("Running All Benchmarks...");
                Console.WriteLine();
                BenchmarkRunner.Run<BulkInsertBenchmarks>();
                BenchmarkRunner.Run<ConnectionPoolingBenchmarks>();
                break;

            default:
                Console.WriteLine($"Unknown benchmark type: {args[0]}");
                Console.WriteLine();
                PrintUsage();
                break;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run --configuration Release -- <benchmark-type>");
        Console.WriteLine();
        Console.WriteLine("Benchmark Types:");
        Console.WriteLine("  bulk       - Run bulk insert benchmarks (Individual, Batched, TVP, SqlBulkCopy)");
        Console.WriteLine("  pooling    - Run connection pooling benchmarks");
        Console.WriteLine("  all        - Run all benchmarks");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --configuration Release -- bulk");
        Console.WriteLine("  dotnet run --configuration Release -- pooling");
        Console.WriteLine("  dotnet run --configuration Release -- all");
        Console.WriteLine();
        Console.WriteLine("Note: Benchmarks should always be run in Release mode for accurate results.");
    }
}
