using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class UserRepository
{
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT username, password, role, name, ISNULL(is_active, 1) AS is_active
            FROM tblUser 
            WHERE username = @username AND password = @password AND ISNULL(is_active, 1) = 1;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@username", username.Trim());
        cmd.Parameters.AddWithValue("@password", password.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Username = reader["username"]?.ToString() ?? string.Empty,
                Password = reader["password"]?.ToString() ?? string.Empty,
                Role = reader["role"]?.ToString() ?? "Cashier",
                Name = reader["name"]?.ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }
        return null;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var list = new List<User>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "SELECT username, password, role, name, ISNULL(is_active, 1) AS is_active FROM tblUser ORDER BY name ASC;";
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new User
            {
                Username = reader["username"]?.ToString() ?? string.Empty,
                Password = reader["password"]?.ToString() ?? string.Empty,
                Role = reader["role"]?.ToString() ?? "Cashier",
                Name = reader["name"]?.ToString() ?? string.Empty,
                IsActive = Convert.ToBoolean(reader["is_active"])
            });
        }
        return list;
    }

    public async Task<bool> SaveUserAsync(User user, bool isNew)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        string sql;
        if (isNew)
        {
            sql = "INSERT INTO tblUser (username, password, role, name, is_active) VALUES (@username, @password, @role, @name, @is_active);";
        }
        else
        {
            sql = "UPDATE tblUser SET password = @password, role = @role, name = @name, is_active = @is_active WHERE username = @username;";
        }

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@username", user.Username.Trim());
        cmd.Parameters.AddWithValue("@password", user.Password.Trim());
        cmd.Parameters.AddWithValue("@role", user.Role);
        cmd.Parameters.AddWithValue("@name", user.Name.Trim());
        cmd.Parameters.AddWithValue("@is_active", user.IsActive ? 1 : 0);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "UPDATE tblUser SET is_active = 0 WHERE username = @username;";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@username", username);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
