using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class SaleRepository
{
    public async Task<string> GenerateTransactionNumberAsync()
    {
        string datePrefix = DateTime.Now.ToString("yyyyMMdd");
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT TOP 1 tr_no FROM tblSale 
            WHERE tr_no LIKE @prefix + '%' 
            ORDER BY tr_no DESC;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@prefix", datePrefix);
        var lastNo = (string?)await cmd.ExecuteScalarAsync();

        if (string.IsNullOrEmpty(lastNo))
        {
            return $"{datePrefix}-1001";
        }

        string[] parts = lastNo.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[1], out int sequence))
        {
            return $"{datePrefix}-{sequence + 1}";
        }

        return $"{datePrefix}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
    }

    public async Task<decimal> GetVatRateAsync()
    {
        try
        {
            await using var cn = DbConfig.CreateConnection();
            await cn.OpenAsync();
            const string sql = "SELECT TOP 1 vat FROM tblVat;";
            await using var cmd = new SqlCommand(sql, cn);
            var result = await cmd.ExecuteScalarAsync();
            return result == null || result == DBNull.Value ? 0.12m : Convert.ToDecimal(result);
        }
        catch
        {
            return 0.12m;
        }
    }

    public async Task<(bool Success, string Message)> ProcessCheckoutAsync(Sale sale, List<CartItem> items)
    {
        if (items.Count == 0)
        {
            return (false, "Cart is empty.");
        }

        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();
        await using var tx = cn.BeginTransaction();

        try
        {
            // 1. Verify and deduct product inventory
            foreach (var item in items)
            {
                const string checkStockSql = "SELECT qty, pdescription FROM tblProduct WITH (UPDLOCK) WHERE pcode = @pcode;";
                int stock;
                string desc;

                await using (var checkCmd = new SqlCommand(checkStockSql, cn, tx))
                {
                    checkCmd.Parameters.AddWithValue("@pcode", item.PCode);
                    await using var reader = await checkCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return (false, $"Product {item.PCode} not found in database.");
                    }
                    stock = Convert.ToInt32(reader["qty"]);
                    desc = reader["pdescription"]?.ToString() ?? item.PCode;
                }

                if (stock < item.Quantity)
                {
                    await tx.RollbackAsync();
                    return (false, $"Insufficient stock for {desc}. Required: {item.Quantity}, Available: {stock}");
                }

                const string deductSql = "UPDATE tblProduct SET qty = qty - @qty WHERE pcode = @pcode;";
                await using (var deductCmd = new SqlCommand(deductSql, cn, tx))
                {
                    deductCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    deductCmd.Parameters.AddWithValue("@pcode", item.PCode);
                    await deductCmd.ExecuteNonQueryAsync();
                }

                // Update tblCart item status to 'Sold'
                const string updateCartSql = "UPDATE tblCart SET status = 'Sold', sdate = GETDATE() WHERE id = @id;";
                await using (var cartCmd = new SqlCommand(updateCartSql, cn, tx))
                {
                    cartCmd.Parameters.AddWithValue("@id", item.Id);
                    await cartCmd.ExecuteNonQueryAsync();
                }
            }

            // 2. Insert master sale record in tblSale
            const string insertSaleSql = @"
                INSERT INTO tblSale (tr_no, sdate, subtotal, discount, vat, total, cash_tendered, change_due, payment_method, cashier, customer_name, item_count)
                VALUES (@tr_no, GETDATE(), @subtotal, @discount, @vat, @total, @cash_tendered, @change_due, @payment_method, @cashier, @customer_name, @item_count);";

            await using (var saleCmd = new SqlCommand(insertSaleSql, cn, tx))
            {
                saleCmd.Parameters.AddWithValue("@tr_no", sale.TransactionNo);
                saleCmd.Parameters.AddWithValue("@subtotal", sale.Subtotal);
                saleCmd.Parameters.AddWithValue("@discount", sale.Discount);
                saleCmd.Parameters.AddWithValue("@vat", sale.Vat);
                saleCmd.Parameters.AddWithValue("@total", sale.Total);
                saleCmd.Parameters.AddWithValue("@cash_tendered", sale.CashTendered);
                saleCmd.Parameters.AddWithValue("@change_due", sale.ChangeDue);
                saleCmd.Parameters.AddWithValue("@payment_method", sale.PaymentMethod);
                saleCmd.Parameters.AddWithValue("@cashier", sale.Cashier);
                saleCmd.Parameters.AddWithValue("@customer_name", (object?)sale.CustomerName ?? DBNull.Value);
                saleCmd.Parameters.AddWithValue("@item_count", items.Sum(x => x.Quantity));
                await saleCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return (true, "Sale completed successfully!");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Checkout failed: {ex.Message}");
        }
    }

    public async Task<List<Sale>> GetSalesHistoryAsync(DateTime fromDate, DateTime toDate, string? cashier = null)
    {
        var list = new List<Sale>();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        string sql = @"
            SELECT tr_no, sdate, subtotal, discount, vat, total, cash_tendered, change_due, payment_method, cashier, customer_name, item_count
            FROM tblSale
            WHERE CAST(sdate AS DATE) BETWEEN @from AND @to ";

        if (!string.IsNullOrWhiteSpace(cashier) && cashier != "All")
        {
            sql += " AND cashier = @cashier ";
        }

        sql += " ORDER BY sdate DESC;";

        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@from", fromDate.Date);
        cmd.Parameters.AddWithValue("@to", toDate.Date);
        if (!string.IsNullOrWhiteSpace(cashier) && cashier != "All")
            cmd.Parameters.AddWithValue("@cashier", cashier);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Sale
            {
                TransactionNo = reader["tr_no"]?.ToString() ?? string.Empty,
                Date = Convert.ToDateTime(reader["sdate"]),
                Subtotal = Convert.ToDecimal(reader["subtotal"]),
                Discount = Convert.ToDecimal(reader["discount"]),
                Vat = Convert.ToDecimal(reader["vat"]),
                Total = Convert.ToDecimal(reader["total"]),
                CashTendered = Convert.ToDecimal(reader["cash_tendered"]),
                ChangeDue = Convert.ToDecimal(reader["change_due"]),
                PaymentMethod = reader["payment_method"]?.ToString() ?? "Cash",
                Cashier = reader["cashier"]?.ToString() ?? string.Empty,
                CustomerName = reader["customer_name"]?.ToString(),
                ItemCount = Convert.ToInt32(reader["item_count"])
            });
        }
        return list;
    }

    public async Task<Sale?> GetSaleByTransactionAsync(string trNo)
    {
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        const string sql = @"
            SELECT tr_no, sdate, subtotal, discount, vat, total, cash_tendered, change_due, payment_method, cashier, customer_name, item_count
            FROM tblSale
            WHERE tr_no = @tr_no;";

        Sale? sale = null;
        await using (var cmd = new SqlCommand(sql, cn))
        {
            cmd.Parameters.AddWithValue("@tr_no", trNo);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                sale = new Sale
                {
                    TransactionNo = reader["tr_no"]?.ToString() ?? string.Empty,
                    Date = Convert.ToDateTime(reader["sdate"]),
                    Subtotal = Convert.ToDecimal(reader["subtotal"]),
                    Discount = Convert.ToDecimal(reader["discount"]),
                    Vat = Convert.ToDecimal(reader["vat"]),
                    Total = Convert.ToDecimal(reader["total"]),
                    CashTendered = Convert.ToDecimal(reader["cash_tendered"]),
                    ChangeDue = Convert.ToDecimal(reader["change_due"]),
                    PaymentMethod = reader["payment_method"]?.ToString() ?? "Cash",
                    Cashier = reader["cashier"]?.ToString() ?? string.Empty,
                    CustomerName = reader["customer_name"]?.ToString(),
                    ItemCount = Convert.ToInt32(reader["item_count"])
                };
            }
        }

        if (sale != null)
        {
            const string itemsSql = @"
                SELECT c.id, c.tr_no, c.pid, p.barcode, p.pdescription, c.price, c.qty, c.disc, c.total, c.sdate, c.status, c.cashier
                FROM tblCart c
                LEFT JOIN tblProduct p ON p.pcode = c.pid
                WHERE c.tr_no = @tr_no;";

            await using var itemsCmd = new SqlCommand(itemsSql, cn);
            itemsCmd.Parameters.AddWithValue("@tr_no", trNo);
            await using var itemReader = await itemsCmd.ExecuteReaderAsync();
            while (await itemReader.ReadAsync())
            {
                sale.Items.Add(new CartItem
                {
                    Id = Convert.ToInt32(itemReader["id"]),
                    TransactionNo = itemReader["tr_no"]?.ToString() ?? string.Empty,
                    PCode = itemReader["pid"]?.ToString() ?? string.Empty,
                    Barcode = itemReader["barcode"]?.ToString() ?? string.Empty,
                    Description = itemReader["pdescription"]?.ToString() ?? string.Empty,
                    UnitPrice = Convert.ToDecimal(itemReader["price"]),
                    Quantity = Convert.ToInt32(itemReader["qty"]),
                    Discount = Convert.ToDecimal(itemReader["disc"]),
                    Date = Convert.ToDateTime(itemReader["sdate"]),
                    Status = itemReader["status"]?.ToString() ?? string.Empty,
                    Cashier = itemReader["cashier"]?.ToString() ?? string.Empty
                });
            }
        }

        return sale;
    }
}
