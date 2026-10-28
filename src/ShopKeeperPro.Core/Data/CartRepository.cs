using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class CartRepository
{
    public async Task<List<CartItem>> GetCartItemsAsync(string trNo)
    {
        var list = new List<CartItem>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT c.id, c.tr_no, c.pid, p.barcode, p.pdescription, c.price, c.qty, c.disc, c.total, c.sdate, c.status, c.cashier, p.qty AS stock_on_hand
            FROM tblCart c
            INNER JOIN tblProduct p ON p.pcode = c.pid
            WHERE c.tr_no = @tr_no AND c.status = 'Pending'
            ORDER BY c.id ASC;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@tr_no", trNo);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CartItem
            {
                Id = Convert.ToInt32(reader["id"]),
                TransactionNo = reader["tr_no"]?.ToString() ?? string.Empty,
                PCode = reader["pid"]?.ToString() ?? string.Empty,
                Barcode = reader["barcode"]?.ToString() ?? string.Empty,
                Description = reader["pdescription"]?.ToString() ?? string.Empty,
                UnitPrice = reader["price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["price"]),
                Quantity = reader["qty"] == DBNull.Value ? 1 : Convert.ToInt32(reader["qty"]),
                Discount = reader["disc"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["disc"]),
                Date = reader["sdate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["sdate"]),
                Status = reader["status"]?.ToString() ?? "Pending",
                Cashier = reader["cashier"]?.ToString() ?? string.Empty,
                StockOnHand = reader["stock_on_hand"] == DBNull.Value ? 0 : Convert.ToInt32(reader["stock_on_hand"])
            });
        }
        return list;
    }

    public async Task<(bool Success, string Message, CartItem? Item)> AddToCartAsync(string trNo, Product product, int quantity, string cashier)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        // 1. Check if item already exists in active pending cart
        const string checkSql = @"
            SELECT id, qty, disc FROM tblCart 
            WHERE tr_no = @tr_no AND pid = @pid AND status = 'Pending';";

        int? existingId = null;
        int currentCartQty = 0;
        decimal currentDisc = 0m;

        await using (var checkCmd = new SqlCommand(checkSql, cn))
        {
            checkCmd.Parameters.AddWithValue("@tr_no", trNo);
            checkCmd.Parameters.AddWithValue("@pid", product.PCode);
            await using var reader = await checkCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                existingId = Convert.ToInt32(reader["id"]);
                currentCartQty = Convert.ToInt32(reader["qty"]);
                currentDisc = reader["disc"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["disc"]);
            }
        }

        int targetQty = (existingId.HasValue ? currentCartQty : 0) + quantity;
        if (targetQty > product.Qty)
        {
            return (false, $"Only {product.Qty} in stock for {product.Description} (Current cart has {currentCartQty})", null);
        }

        if (existingId.HasValue)
        {
            const string updateSql = @"
                UPDATE tblCart 
                SET qty = @qty, total = (@qty * price) - disc
                WHERE id = @id;";
            await using var updateCmd = new SqlCommand(updateSql, cn);
            updateCmd.Parameters.AddWithValue("@qty", targetQty);
            updateCmd.Parameters.AddWithValue("@id", existingId.Value);
            await updateCmd.ExecuteNonQueryAsync();
        }
        else
        {
            const string insertSql = @"
                INSERT INTO tblCart (tr_no, pid, price, qty, disc, total, sdate, status, cashier)
                VALUES (@tr_no, @pid, @price, @qty, @disc, @total, GETDATE(), 'Pending', @cashier);";
            await using var insertCmd = new SqlCommand(insertSql, cn);
            insertCmd.Parameters.AddWithValue("@tr_no", trNo);
            insertCmd.Parameters.AddWithValue("@pid", product.PCode);
            insertCmd.Parameters.AddWithValue("@price", product.Price);
            insertCmd.Parameters.AddWithValue("@qty", targetQty);
            insertCmd.Parameters.AddWithValue("@disc", 0m);
            insertCmd.Parameters.AddWithValue("@total", product.Price * targetQty);
            insertCmd.Parameters.AddWithValue("@cashier", cashier);
            await insertCmd.ExecuteNonQueryAsync();
        }

        var updatedItems = await GetCartItemsAsync(trNo);
        var activeItem = updatedItems.FirstOrDefault(x => x.PCode == product.PCode);
        return (true, "Item added to cart", activeItem);
    }

    public async Task<bool> UpdateItemQuantityAsync(int cartId, int newQty, int stockOnHand)
    {
        if (newQty <= 0 || newQty > stockOnHand) return false;

        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            UPDATE tblCart 
            SET qty = @qty, total = (@qty * price) - disc 
            WHERE id = @id AND status = 'Pending';";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@qty", newQty);
        cmd.Parameters.AddWithValue("@id", cartId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> UpdateItemDiscountAsync(int cartId, decimal discount)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            UPDATE tblCart 
            SET disc = @disc, total = (qty * price) - @disc 
            WHERE id = @id AND status = 'Pending';";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@disc", discount);
        cmd.Parameters.AddWithValue("@id", cartId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> RemoveItemAsync(int cartId)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "DELETE FROM tblCart WHERE id = @id AND status = 'Pending';";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", cartId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ClearCartAsync(string trNo)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = "DELETE FROM tblCart WHERE tr_no = @tr_no AND status = 'Pending';";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@tr_no", trNo);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
