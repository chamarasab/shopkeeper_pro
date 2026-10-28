using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class ProductRepository
{
    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = @"
            SELECT p.pcode, p.barcode, p.pdescription, p.brand_id, p.category_id, 
                   ISNULL(b.brand, '') AS brand_name, ISNULL(c.category, '') AS category_name,
                   p.price, ISNULL(p.cost_price, 0) AS cost_price, p.qty, p.reorder, ISNULL(p.is_active, 1) AS is_active
            FROM tblProduct p
            LEFT JOIN tblBrand b ON b.id = p.brand_id
            LEFT JOIN tblCategory c ON c.id = p.category_id
            WHERE p.barcode = @barcode AND ISNULL(p.is_active, 1) = 1;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@barcode", barcode.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapProduct(reader);
        }
        return null;
    }

    public async Task<Product?> GetByPCodeAsync(string pcode)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = @"
            SELECT p.pcode, p.barcode, p.pdescription, p.brand_id, p.category_id, 
                   ISNULL(b.brand, '') AS brand_name, ISNULL(c.category, '') AS category_name,
                   p.price, ISNULL(p.cost_price, 0) AS cost_price, p.qty, p.reorder, ISNULL(p.is_active, 1) AS is_active
            FROM tblProduct p
            LEFT JOIN tblBrand b ON b.id = p.brand_id
            LEFT JOIN tblCategory c ON c.id = p.category_id
            WHERE p.pcode = @pcode;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@pcode", pcode.Trim());

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapProduct(reader);
        }
        return null;
    }

    public async Task<List<Product>> SearchAsync(string query, int? categoryId = null, bool activeOnly = true)
    {
        var list = new List<Product>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        string sql = @"
            SELECT p.pcode, p.barcode, p.pdescription, p.brand_id, p.category_id, 
                   ISNULL(b.brand, '') AS brand_name, ISNULL(c.category, '') AS category_name,
                   p.price, ISNULL(p.cost_price, 0) AS cost_price, p.qty, p.reorder, ISNULL(p.is_active, 1) AS is_active
            FROM tblProduct p
            LEFT JOIN tblBrand b ON b.id = p.brand_id
            LEFT JOIN tblCategory c ON c.id = p.category_id
            WHERE 1=1 ";

        if (activeOnly)
            sql += " AND ISNULL(p.is_active, 1) = 1 ";

        if (!string.IsNullOrWhiteSpace(query))
            sql += " AND (p.pdescription LIKE @query OR p.barcode LIKE @query OR p.pcode LIKE @query) ";

        if (categoryId.HasValue && categoryId.Value > 0)
            sql += " AND p.category_id = @categoryId ";

        sql += " ORDER BY p.pdescription ASC;";

        await using var cmd = new SqlCommand(sql, cn);
        if (!string.IsNullOrWhiteSpace(query))
            cmd.Parameters.AddWithValue("@query", $"%{query.Trim()}%");
        if (categoryId.HasValue && categoryId.Value > 0)
            cmd.Parameters.AddWithValue("@categoryId", categoryId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapProduct(reader));
        }
        return list;
    }

    public async Task<List<Product>> GetAllAsync(bool activeOnly = false)
    {
        return await SearchAsync(string.Empty, null, activeOnly);
    }

    public async Task<List<Product>> GetCriticalStockAsync()
    {
        var list = new List<Product>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT p.pcode, p.barcode, p.pdescription, p.brand_id, p.category_id, 
                   ISNULL(b.brand, '') AS brand_name, ISNULL(c.category, '') AS category_name,
                   p.price, ISNULL(p.cost_price, 0) AS cost_price, p.qty, p.reorder, ISNULL(p.is_active, 1) AS is_active
            FROM tblProduct p
            LEFT JOIN tblBrand b ON b.id = p.brand_id
            LEFT JOIN tblCategory c ON c.id = p.category_id
            WHERE p.qty <= p.reorder AND ISNULL(p.is_active, 1) = 1
            ORDER BY p.qty ASC;";

        await using var cmd = new SqlCommand(sql, cn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(MapProduct(reader));
        }
        return list;
    }

    public async Task<bool> SaveAsync(Product product, bool isNew)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        string sql;
        if (isNew)
        {
            sql = @"
                INSERT INTO tblProduct (pcode, barcode, pdescription, brand_id, category_id, price, cost_price, qty, reorder, is_active)
                VALUES (@pcode, @barcode, @pdescription, @brand_id, @category_id, @price, @cost_price, @qty, @reorder, @is_active);";
        }
        else
        {
            sql = @"
                UPDATE tblProduct 
                SET barcode = @barcode, pdescription = @pdescription, brand_id = @brand_id, category_id = @category_id,
                    price = @price, cost_price = @cost_price, qty = @qty, reorder = @reorder, is_active = @is_active
                WHERE pcode = @pcode;";
        }

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@pcode", product.PCode);
        cmd.Parameters.AddWithValue("@barcode", product.Barcode);
        cmd.Parameters.AddWithValue("@pdescription", product.Description);
        cmd.Parameters.AddWithValue("@brand_id", (object?)product.BrandId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@category_id", (object?)product.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", product.Price);
        cmd.Parameters.AddWithValue("@cost_price", product.CostPrice);
        cmd.Parameters.AddWithValue("@qty", product.Qty);
        cmd.Parameters.AddWithValue("@reorder", product.Reorder);
        cmd.Parameters.AddWithValue("@is_active", product.IsActive ? 1 : 0);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(string pcode)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        const string sql = "UPDATE tblProduct SET is_active = 0 WHERE pcode = @pcode;";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@pcode", pcode);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Product MapProduct(SqlDataReader reader)
    {
        return new Product
        {
            PCode = reader["pcode"]?.ToString() ?? string.Empty,
            Barcode = reader["barcode"]?.ToString() ?? string.Empty,
            Description = reader["pdescription"]?.ToString() ?? string.Empty,
            BrandId = reader["brand_id"] == DBNull.Value ? null : Convert.ToInt32(reader["brand_id"]),
            CategoryId = reader["category_id"] == DBNull.Value ? null : Convert.ToInt32(reader["category_id"]),
            BrandName = reader["brand_name"]?.ToString() ?? string.Empty,
            CategoryName = reader["category_name"]?.ToString() ?? string.Empty,
            Price = reader["price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["price"]),
            CostPrice = reader["cost_price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["cost_price"]),
            Qty = reader["qty"] == DBNull.Value ? 0 : Convert.ToInt32(reader["qty"]),
            Reorder = reader["reorder"] == DBNull.Value ? 10 : Convert.ToInt32(reader["reorder"]),
            IsActive = reader["is_active"] == DBNull.Value || Convert.ToBoolean(reader["is_active"])
        };
    }
}
