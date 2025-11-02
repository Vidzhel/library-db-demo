using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Infrastructure.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.Application.Repositories;

/// <summary>
/// Repository implementation for system statistics and time-series analytics
/// </summary>
public class SystemStatisticsRepository : ISystemStatisticsRepository
{
    public async Task<SystemStatistic> CreateAsync(SystemStatistic statistic, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO SystemStatistics (
                RecordedAt, ActiveLoansCount, NewLoansCount, ReturnedLoansCount,
                ActiveMembersCount, OverdueLoansCount, TotalBooksAvailable,
                DatabaseSizeMB, ActiveConnectionsCount, AvgQueryTimeMs,
                CPUUsagePercent, MemoryUsagePercent, ServerName, Notes
            )
            OUTPUT INSERTED.Id
            VALUES (
                @RecordedAt, @ActiveLoansCount, @NewLoansCount, @ReturnedLoansCount,
                @ActiveMembersCount, @OverdueLoansCount, @TotalBooksAvailable,
                @DatabaseSizeMB, @ActiveConnectionsCount, @AvgQueryTimeMs,
                @CPUUsagePercent, @MemoryUsagePercent, @ServerName, @Notes
            )";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@RecordedAt", statistic.RecordedAt);
        command.Parameters.AddWithValue("@ActiveLoansCount", (object?)statistic.ActiveLoansCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@NewLoansCount", (object?)statistic.NewLoansCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@ReturnedLoansCount", (object?)statistic.ReturnedLoansCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@ActiveMembersCount", (object?)statistic.ActiveMembersCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@OverdueLoansCount", (object?)statistic.OverdueLoansCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@TotalBooksAvailable", (object?)statistic.TotalBooksAvailable ?? DBNull.Value);
        command.Parameters.AddWithValue("@DatabaseSizeMB", (object?)statistic.DatabaseSizeMB ?? DBNull.Value);
        command.Parameters.AddWithValue("@ActiveConnectionsCount", (object?)statistic.ActiveConnectionsCount ?? DBNull.Value);
        command.Parameters.AddWithValue("@AvgQueryTimeMs", (object?)statistic.AvgQueryTimeMs ?? DBNull.Value);
        command.Parameters.AddWithValue("@CPUUsagePercent", (object?)statistic.CPUUsagePercent ?? DBNull.Value);
        command.Parameters.AddWithValue("@MemoryUsagePercent", (object?)statistic.MemoryUsagePercent ?? DBNull.Value);
        command.Parameters.AddWithValue("@ServerName", (object?)statistic.ServerName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Notes", (object?)statistic.Notes ?? DBNull.Value);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;

        return SystemStatistic.FromDatabase(
            id, statistic.RecordedAt,
            statistic.ActiveLoansCount, statistic.NewLoansCount, statistic.ReturnedLoansCount,
            statistic.ActiveMembersCount, statistic.OverdueLoansCount, statistic.TotalBooksAvailable,
            statistic.DatabaseSizeMB, statistic.ActiveConnectionsCount, statistic.AvgQueryTimeMs,
            statistic.CPUUsagePercent, statistic.MemoryUsagePercent,
            statistic.ServerName, statistic.Notes);
    }

    public async Task<List<SystemStatistic>> GetByTimeRangeAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, RecordedAt, ActiveLoansCount, NewLoansCount, ReturnedLoansCount,
                   ActiveMembersCount, OverdueLoansCount, TotalBooksAvailable,
                   DatabaseSizeMB, ActiveConnectionsCount, AvgQueryTimeMs,
                   CPUUsagePercent, MemoryUsagePercent, ServerName, Notes
            FROM SystemStatistics
            WHERE RecordedAt BETWEEN @StartDate AND @EndDate
            ORDER BY RecordedAt";

        var statistics = new List<SystemStatistic>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@StartDate", startDate);
        command.Parameters.AddWithValue("@EndDate", endDate);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            statistics.Add(MapReaderToStatistic(reader));
        }

        return statistics;
    }

    public async Task<List<SystemStatistic>> GetRecentAsync(int count, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        var sql = $@"
            SELECT TOP (@Count) Id, RecordedAt, ActiveLoansCount, NewLoansCount, ReturnedLoansCount,
                   ActiveMembersCount, OverdueLoansCount, TotalBooksAvailable,
                   DatabaseSizeMB, ActiveConnectionsCount, AvgQueryTimeMs,
                   CPUUsagePercent, MemoryUsagePercent, ServerName, Notes
            FROM SystemStatistics
            ORDER BY RecordedAt DESC";

        var statistics = new List<SystemStatistic>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Count", count);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            statistics.Add(MapReaderToStatistic(reader));
        }

        return statistics;
    }

    public async Task<List<HourlyStatistic>> GetHourlyStatisticsAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 100
                HourBucket, SampleCount, AvgActiveLoans, AvgNewLoans,
                AvgCPUUsage, AvgMemoryUsage, MinActiveLoans, MaxActiveLoans
            FROM vw_HourlyStatistics
            ORDER BY HourBucket DESC";

        var statistics = new List<HourlyStatistic>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            statistics.Add(new HourlyStatistic
            {
                HourBucket = reader.GetDateTime(0),
                SampleCount = reader.GetInt32(1),
                AvgActiveLoans = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                AvgNewLoans = reader.IsDBNull(3) ? null : Convert.ToDouble(reader.GetValue(3)),
                AvgCPUUsage = reader.IsDBNull(4) ? null : Convert.ToDouble(reader.GetValue(4)),
                AvgMemoryUsage = reader.IsDBNull(5) ? null : Convert.ToDouble(reader.GetValue(5)),
                MinActiveLoans = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                MaxActiveLoans = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        return statistics;
    }

    public async Task<List<DailyStatistic>> GetDailyStatisticsAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP 100
                DayDate, SampleCount, AvgActiveLoans, MinActiveLoans, MaxActiveLoans,
                TotalNewLoans, TotalReturnedLoans, AvgCPUUsage, AvgMemoryUsage,
                MinCPUUsage, MaxCPUUsage, StdDevCPUUsage
            FROM vw_DailyStatistics
            ORDER BY DayDate DESC";

        var statistics = new List<DailyStatistic>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            statistics.Add(new DailyStatistic
            {
                DayDate = reader.GetDateTime(0),
                SampleCount = reader.GetInt32(1),
                AvgActiveLoans = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                MinActiveLoans = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                MaxActiveLoans = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                TotalNewLoans = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                TotalReturnedLoans = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                AvgCPUUsage = reader.IsDBNull(7) ? null : Convert.ToDouble(reader.GetValue(7)),
                AvgMemoryUsage = reader.IsDBNull(8) ? null : Convert.ToDouble(reader.GetValue(8)),
                MinCPUUsage = reader.IsDBNull(9) ? null : Convert.ToDouble(reader.GetValue(9)),
                MaxCPUUsage = reader.IsDBNull(10) ? null : Convert.ToDouble(reader.GetValue(10)),
                StdDevCPUUsage = reader.IsDBNull(11) ? null : Convert.ToDouble(reader.GetValue(11))
            });
        }

        return statistics;
    }

    public async Task<List<MovingAverageResult>> GetMovingAveragesAsync(DateTime startDate, DateTime endDate, int windowSize, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_GetMovingAverages
                @StartDate = @Start,
                @EndDate = @End,
                @WindowSize = @Window";

        var results = new List<MovingAverageResult>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Start", startDate);
        command.Parameters.AddWithValue("@End", endDate);
        command.Parameters.AddWithValue("@Window", windowSize);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new MovingAverageResult
            {
                RecordedAt = reader.GetDateTime(0),
                ActiveLoansCount = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                CPUUsagePercent = reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                MemoryUsagePercent = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                MovingAvgActiveLoans = reader.IsDBNull(4) ? null : Convert.ToDouble(reader.GetValue(4)),
                MovingAvgCPU = reader.IsDBNull(5) ? null : Convert.ToDouble(reader.GetValue(5)),
                MovingAvgMemory = reader.IsDBNull(6) ? null : Convert.ToDouble(reader.GetValue(6)),
                RunningTotalNewLoans = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                RowNum = reader.GetInt64(8)
            });
        }

        return results;
    }

    public async Task<List<PercentileResult>> GetPercentilesAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_GetPercentiles
                @StartDate = @Start,
                @EndDate = @End";

        var results = new List<PercentileResult>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Start", startDate);
        command.Parameters.AddWithValue("@End", endDate);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PercentileResult
            {
                MetricName = reader.GetString(0),
                MinValue = reader.IsDBNull(1) ? null : Convert.ToDouble(reader.GetValue(1)),
                P25 = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                P50_Median = reader.IsDBNull(3) ? null : Convert.ToDouble(reader.GetValue(3)),
                P75 = reader.IsDBNull(4) ? null : Convert.ToDouble(reader.GetValue(4)),
                P95 = reader.IsDBNull(5) ? null : Convert.ToDouble(reader.GetValue(5)),
                P99 = reader.IsDBNull(6) ? null : Convert.ToDouble(reader.GetValue(6)),
                MaxValue = reader.IsDBNull(7) ? null : Convert.ToDouble(reader.GetValue(7)),
                AvgValue = reader.IsDBNull(8) ? null : Convert.ToDouble(reader.GetValue(8)),
                StdDev = reader.IsDBNull(9) ? null : Convert.ToDouble(reader.GetValue(9))
            });
        }

        return results;
    }

    public async Task<List<AnomalyResult>> DetectAnomaliesAsync(DateTime startDate, DateTime endDate, double standardDeviations, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_DetectAnomalies
                @StartDate = @Start,
                @EndDate = @End,
                @StandardDeviations = @StdDevs";

        var results = new List<AnomalyResult>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Start", startDate);
        command.Parameters.AddWithValue("@End", endDate);
        command.Parameters.AddWithValue("@StdDevs", standardDeviations);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AnomalyResult
            {
                RecordedAt = reader.GetDateTime(0),
                CPUUsagePercent = reader.IsDBNull(1) ? null : reader.GetDecimal(1),
                AvgCPU = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                CPUAnomaly = reader.IsDBNull(3) ? null : reader.GetString(3),
                CPUZScore = reader.IsDBNull(4) ? null : Convert.ToDouble(reader.GetValue(4)),
                MemoryUsagePercent = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                AvgMemory = reader.IsDBNull(6) ? null : Convert.ToDouble(reader.GetValue(6)),
                MemoryAnomaly = reader.IsDBNull(7) ? null : reader.GetString(7),
                MemoryZScore = reader.IsDBNull(8) ? null : Convert.ToDouble(reader.GetValue(8)),
                ActiveLoansCount = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                AvgLoans = reader.IsDBNull(10) ? null : Convert.ToDouble(reader.GetValue(10)),
                LoansAnomaly = reader.IsDBNull(11) ? null : reader.GetString(11),
                LoansZScore = reader.IsDBNull(12) ? null : Convert.ToDouble(reader.GetValue(12))
            });
        }

        return results;
    }

    public async Task<List<TrendAnalysisResult>> GetTrendAnalysisAsync(DateTime startDate, DateTime endDate, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_GetTrendAnalysis
                @StartDate = @Start,
                @EndDate = @End";

        var results = new List<TrendAnalysisResult>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Start", startDate);
        command.Parameters.AddWithValue("@End", endDate);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TrendAnalysisResult
            {
                DayDate = reader.GetDateTime(0),
                AvgActiveLoans = reader.IsDBNull(1) ? null : Convert.ToDouble(reader.GetValue(1)),
                AvgCPU = reader.IsDBNull(2) ? null : Convert.ToDouble(reader.GetValue(2)),
                AvgMemory = reader.IsDBNull(3) ? null : Convert.ToDouble(reader.GetValue(3)),
                TotalNewLoans = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                DayOverDayLoansChange = reader.IsDBNull(5) ? null : Convert.ToDouble(reader.GetValue(5)),
                DayOverDayLoansChangePercent = reader.IsDBNull(6) ? null : Convert.ToDouble(reader.GetValue(6)),
                SevenDayAvgLoans = reader.IsDBNull(7) ? null : Convert.ToDouble(reader.GetValue(7)),
                CumulativeNewLoans = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                RankByActiveLoans = reader.IsDBNull(9) ? null : reader.GetInt64(9),
                RankByCPU = reader.IsDBNull(10) ? null : reader.GetInt64(10)
            });
        }

        return results;
    }

    private static SystemStatistic MapReaderToStatistic(SqlDataReader reader)
    {
        return SystemStatistic.FromDatabase(
            id: reader.GetInt64(0),
            recordedAt: reader.GetDateTime(1),
            activeLoansCount: reader.IsDBNull(2) ? null : reader.GetInt32(2),
            newLoansCount: reader.IsDBNull(3) ? null : reader.GetInt32(3),
            returnedLoansCount: reader.IsDBNull(4) ? null : reader.GetInt32(4),
            activeMembersCount: reader.IsDBNull(5) ? null : reader.GetInt32(5),
            overdueLoansCount: reader.IsDBNull(6) ? null : reader.GetInt32(6),
            totalBooksAvailable: reader.IsDBNull(7) ? null : reader.GetInt32(7),
            databaseSizeMB: reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            activeConnectionsCount: reader.IsDBNull(9) ? null : reader.GetInt32(9),
            avgQueryTimeMs: reader.IsDBNull(10) ? null : reader.GetDecimal(10),
            cpuUsagePercent: reader.IsDBNull(11) ? null : reader.GetDecimal(11),
            memoryUsagePercent: reader.IsDBNull(12) ? null : reader.GetDecimal(12),
            serverName: reader.IsDBNull(13) ? null : reader.GetString(13),
            notes: reader.IsDBNull(14) ? null : reader.GetString(14)
        );
    }
}
