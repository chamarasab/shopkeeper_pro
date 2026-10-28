using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class CategoryBrandVendorRepository
{
    public async Task<List<Category>> GetCategoriesAsync()
    {
        var list = new List<Category>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "SELECT id, category FROM tblCategory ORDER BY category ASC;";
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Category
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["category"]?.ToString() ?? string.Empty
            });
        }
        return list;
    }

    public async Task<List<Brand>> GetBrandsAsync()
    {
        var list = new List<Brand>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "SELECT id, brand FROM tblBrand ORDER BY brand ASC;";
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Brand
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["brand"]?.ToString() ?? string.Empty
            });
        }
        return list;
    }

    public async Task<List<Vendor>> GetVendorsAsync()
    {
        var list = new List<Vendor>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "SELECT id, vendor, address, contactperson, telephone, email, fax FROM tblVendor ORDER BY vendor ASC;";
        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Vendor
            {
                Id = Convert.ToInt32(reader["id"]),
                Name = reader["vendor"]?.ToString() ?? string.Empty,
                Address = reader["address"]?.ToString(),
                ContactPerson = reader["contactperson"]?.ToString(),
                Telephone = reader["telephone"]?.ToString(),
                Email = reader["email"]?.ToString(),
                Fax = reader["fax"]?.ToString()
            });
        }
        return list;
    }

    public async Task<bool> AddCategoryAsync(string name)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = "INSERT INTO tblCategory (category) VALUES (@name);";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> AddBrandAsync(string name)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = "INSERT INTO tblBrand (brand) VALUES (@name);";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> AddVendorAsync(Vendor vendor)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = @"
            INSERT INTO tblVendor (vendor, address, contactperson, telephone, email, fax)
            VALUES (@vendor, @address, @contactperson, @telephone, @email, @fax);";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@vendor", vendor.Name.Trim());
        cmd.Parameters.AddWithValue("@address", (object?)vendor.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@contactperson", (object?)vendor.ContactPerson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@telephone", (object?)vendor.Telephone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)vendor.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fax", (object?)vendor.Fax ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
