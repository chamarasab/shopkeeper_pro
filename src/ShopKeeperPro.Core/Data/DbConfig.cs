using System.Data;
using Microsoft.Data.SqlClient;

namespace ShopKeeperPro.Core.Data;

public static class DbConfig
{
    private static string _connectionString = "Server=.;Database=shopkeeper_db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

    public static string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value;
    }

    public static SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public static async Task<(bool Success, string Message, long ElapsedMs)> TestConnectionAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await using var cmd = new SqlCommand("SELECT @@VERSION;", connection);
            var version = await cmd.ExecuteScalarAsync();
            sw.Stop();
            return (true, $"Connected to MSSQL ({sw.ElapsedMilliseconds}ms)", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
