using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace DbDemo.Infrastructure.Repositories;

using DbDemo.Domain.Entities;
using DbDemo.Application.DTOs;
using DbDemo.Application.Repositories;

/// <summary>
/// Repository implementation for library branches with spatial queries
/// </summary>
public class LibraryBranchRepository : ILibraryBranchRepository
{
    public async Task<LibraryBranch> CreateAsync(LibraryBranch branch, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO LibraryBranches (BranchName, Address, City, PostalCode, PhoneNumber, Email, Location)
            OUTPUT INSERTED.Id, INSERTED.CreatedAt, INSERTED.UpdatedAt
            VALUES (@BranchName, @Address, @City, @PostalCode, @PhoneNumber, @Email,
                    geography::Point(@Latitude, @Longitude, 4326))";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@BranchName", branch.BranchName);
        command.Parameters.AddWithValue("@Address", branch.Address);
        command.Parameters.AddWithValue("@City", branch.City);
        command.Parameters.AddWithValue("@PostalCode", (object?)branch.PostalCode ?? DBNull.Value);
        command.Parameters.AddWithValue("@PhoneNumber", (object?)branch.PhoneNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)branch.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@Latitude", (object?)branch.Latitude ?? DBNull.Value);
        command.Parameters.AddWithValue("@Longitude", (object?)branch.Longitude ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var createdAt = reader.GetDateTime(1);
            var updatedAt = reader.GetDateTime(2);

            return LibraryBranch.FromDatabase(
                id, branch.BranchName, branch.Address, branch.City, branch.PostalCode,
                branch.PhoneNumber, branch.Email, branch.Latitude, branch.Longitude,
                createdAt, updatedAt, false);
        }

        throw new InvalidOperationException("Failed to create library branch");
    }

    public async Task<LibraryBranch?> GetByIdAsync(int id, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, BranchName, Address, City, PostalCode, PhoneNumber, Email,
                   Location.Lat AS Latitude, Location.Long AS Longitude,
                   CreatedAt, UpdatedAt, IsDeleted
            FROM LibraryBranches
            WHERE Id = @Id AND IsDeleted = 0";

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapReaderToBranch(reader);
        }

        return null;
    }

    public async Task<List<LibraryBranch>> GetAllAsync(SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, BranchName, Address, City, PostalCode, PhoneNumber, Email,
                   Location.Lat AS Latitude, Location.Long AS Longitude,
                   CreatedAt, UpdatedAt, IsDeleted
            FROM LibraryBranches
            WHERE IsDeleted = 0
            ORDER BY BranchName";

        var branches = new List<LibraryBranch>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            branches.Add(MapReaderToBranch(reader));
        }

        return branches;
    }

    public async Task<List<(LibraryBranch Branch, double DistanceKm)>> FindWithinDistanceAsync(
        double latitude, double longitude, double radiusKm, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_FindBranchesWithinDistance
                @Latitude = @Lat,
                @Longitude = @Lon,
                @RadiusKm = @Radius";

        var results = new List<(LibraryBranch, double)>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Lat", latitude);
        command.Parameters.AddWithValue("@Lon", longitude);
        command.Parameters.AddWithValue("@Radius", radiusKm);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var branch = MapReaderToBranchFromProc(reader);
            var distance = reader.GetDouble(reader.GetOrdinal("DistanceKm"));
            results.Add((branch, distance));
        }

        return results;
    }

    public async Task<List<(LibraryBranch Branch, double DistanceKm)>> FindNearestAsync(
        double latitude, double longitude, int topN, SqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            EXEC sp_FindNearestBranches
                @Latitude = @Lat,
                @Longitude = @Lon,
                @TopN = @Top";

        var results = new List<(LibraryBranch, double)>();

        await using var command = new SqlCommand(sql, transaction.Connection, transaction);
        command.Parameters.AddWithValue("@Lat", latitude);
        command.Parameters.AddWithValue("@Lon", longitude);
        command.Parameters.AddWithValue("@Top", topN);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var branchName = reader.GetString(1);
            var address = reader.GetString(2);
            var city = reader.GetString(3);
            var postalCode = reader.IsDBNull(4) ? null : reader.GetString(4);
            var phoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5);
            var email = reader.IsDBNull(6) ? null : reader.GetString(6);
            var lat = reader.IsDBNull(7) ? null : (double?)reader.GetDouble(7);
            var lon = reader.IsDBNull(8) ? null : (double?)reader.GetDouble(8);
            var distance = reader.GetDouble(9);

            var branch = LibraryBranch.FromDatabase(
                id, branchName, address, city, postalCode, phoneNumber, email,
                lat, lon, DateTime.UtcNow, DateTime.UtcNow, false);

            results.Add((branch, distance));
        }

        return results;
    }

    private static LibraryBranch MapReaderToBranch(SqlDataReader reader)
    {
        return LibraryBranch.FromDatabase(
            id: reader.GetInt32(0),
            branchName: reader.GetString(1),
            address: reader.GetString(2),
            city: reader.GetString(3),
            postalCode: reader.IsDBNull(4) ? null : reader.GetString(4),
            phoneNumber: reader.IsDBNull(5) ? null : reader.GetString(5),
            email: reader.IsDBNull(6) ? null : reader.GetString(6),
            latitude: reader.IsDBNull(7) ? null : (double?)reader.GetDouble(7),
            longitude: reader.IsDBNull(8) ? null : (double?)reader.GetDouble(8),
            createdAt: reader.GetDateTime(9),
            updatedAt: reader.GetDateTime(10),
            isDeleted: reader.GetBoolean(11)
        );
    }

    private static LibraryBranch MapReaderToBranchFromProc(SqlDataReader reader)
    {
        return LibraryBranch.FromDatabase(
            id: reader.GetInt32(0),
            branchName: reader.GetString(1),
            address: reader.GetString(2),
            city: reader.GetString(3),
            postalCode: reader.IsDBNull(4) ? null : reader.GetString(4),
            phoneNumber: reader.IsDBNull(5) ? null : reader.GetString(5),
            email: reader.IsDBNull(6) ? null : reader.GetString(6),
            latitude: null,
            longitude: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            isDeleted: false
        );
    }
}
