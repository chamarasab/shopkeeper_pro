using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class StockRepository
{
    public async Task<List<StockInItem>> GetPendingStockInAsync(string refNo)
    {
        var list = new List<StockInItem>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT s.id, s.refno, s.pcode, p.pdescription, s.qty, s.sdate, s.stockinby, s.status, s.vendorid, ISNULL(v.vendor, '') AS vendor_name
            FROM tblStockIn s
            INNER JOIN tblProduct p ON p.pcode = s.pcode
            LEFT JOIN tblVendor v ON v.id = s.vendorid
            WHERE s.refno = @refno AND s.status = 'Pending'
            ORDER BY s.id ASC;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@refno", refNo);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new StockInItem
            {
                Id = Convert.ToInt32(reader["id"]),
                RefNo = reader["refno"]?.ToString() ?? string.Empty,
                PCode = reader["pcode"]?.ToString() ?? string.Empty,
                Description = reader["pdescription"]?.ToString() ?? string.Empty,
                Quantity = Convert.ToInt32(reader["qty"]),
                Date = Convert.ToDateTime(reader["sdate"]),
                StockInBy = reader["stockinby"]?.ToString() ?? string.Empty,
                Status = reader["status"]?.ToString() ?? string.Empty,
                VendorId = reader["vendorid"] == DBNull.Value ? null : Convert.ToInt32(reader["vendorid"]),
                VendorName = reader["vendor_name"]?.ToString() ?? string.Empty
            });
        }
        return list;
    }

    public async Task<bool> AddStockInItemAsync(string refNo, string pcode, int qty, string user, int? vendorId)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            INSERT INTO tblStockIn (refno, pcode, qty, sdate, stockinby, status, vendorid)
            VALUES (@refno, @pcode, @qty, GETDATE(), @stockinby, 'Pending', @vendorid);";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@refno", refNo);
        cmd.Parameters.AddWithValue("@pcode", pcode);
        cmd.Parameters.AddWithValue("@qty", qty);
        cmd.Parameters.AddWithValue("@stockinby", user);
        cmd.Parameters.AddWithValue("@vendorid", (object?)vendorId ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> FinalizeStockInAsync(string refNo)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        await using var tx = cn.BeginTransaction();

        try
        {
            const string getItemsSql = "SELECT pcode, qty, id FROM tblStockIn WHERE refno = @refno AND status = 'Pending';";
            var items = new List<(string PCode, int Qty, int Id)>();

            await using (var getCmd = new SqlCommand(getItemsSql, cn, tx))
            {
                getCmd.Parameters.AddWithValue("@refno", refNo);
                await using var reader = await getCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add((reader["pcode"].ToString()!, Convert.ToInt32(reader["qty"]), Convert.ToInt32(reader["id"])));
                }
            }

            foreach (var item in items)
            {
                const string updateProductSql = "UPDATE tblProduct SET qty = qty + @qty WHERE pcode = @pcode;";
                await using (var updateProdCmd = new SqlCommand(updateProductSql, cn, tx))
                {
                    updateProdCmd.Parameters.AddWithValue("@qty", item.Qty);
                    updateProdCmd.Parameters.AddWithValue("@pcode", item.PCode);
                    await updateProdCmd.ExecuteNonQueryAsync();
                }

                const string updateStockSql = "UPDATE tblStockIn SET status = 'Done' WHERE id = @id;";
                await using (var updateStockCmd = new SqlCommand(updateStockSql, cn, tx))
                {
                    updateStockCmd.Parameters.AddWithValue("@id", item.Id);
                    await updateStockCmd.ExecuteNonQueryAsync();
                }
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> AdjustStockAsync(string pcode, int qty, string action, string remarks, string user, string refNo)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        await using var tx = cn.BeginTransaction();

        try
        {
            string productSql = action.Equals("Add", StringComparison.OrdinalIgnoreCase)
                ? "UPDATE tblProduct SET qty = qty + @qty WHERE pcode = @pcode;"
                : "UPDATE tblProduct SET qty = qty - @qty WHERE pcode = @pcode;";

            await using (var prodCmd = new SqlCommand(productSql, cn, tx))
            {
                prodCmd.Parameters.AddWithValue("@qty", qty);
                prodCmd.Parameters.AddWithValue("@pcode", pcode);
                await prodCmd.ExecuteNonQueryAsync();
            }

            const string logSql = @"
                INSERT INTO tblAdjustment (referenceno, pcode, qty, action, remarks, sdate, [user])
                VALUES (@ref, @pcode, @qty, @action, @remarks, GETDATE(), @user);";

            await using (var logCmd = new SqlCommand(logSql, cn, tx))
            {
                logCmd.Parameters.AddWithValue("@ref", refNo);
                logCmd.Parameters.AddWithValue("@pcode", pcode);
                logCmd.Parameters.AddWithValue("@qty", qty);
                logCmd.Parameters.AddWithValue("@action", action);
                logCmd.Parameters.AddWithValue("@remarks", remarks);
                logCmd.Parameters.AddWithValue("@user", user);
                await logCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }
}
