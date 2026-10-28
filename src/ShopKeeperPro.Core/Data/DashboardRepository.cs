using System.Data;
using Microsoft.Data.SqlClient;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Data;

public class DashboardRepository
{
    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        var metrics = new DashboardMetrics();
        await using var cn = DbConfig.CreateConnection();
        await cn.OpenAsync();

        // 1. Today's sales from tblSale
        const string todaySalesSql = @"
            SELECT ISNULL(SUM(total), 0) AS total, COUNT(*) AS tr_count 
            FROM tblSale 
            WHERE CAST(sdate AS DATE) = CAST(GETDATE() AS DATE);";

        await using (var cmd = new SqlCommand(todaySalesSql, cn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                metrics.TodaySales = Convert.ToDecimal(reader["total"]);
                metrics.TodayTransactions = Convert.ToInt32(reader["tr_count"]);
            }
        }

        // 2. Month's sales
        const string monthSalesSql = @"
            SELECT ISNULL(SUM(total), 0) AS total 
            FROM tblSale 
            WHERE MONTH(sdate) = MONTH(GETDATE()) AND YEAR(sdate) = YEAR(GETDATE());";

        await using (var cmd = new SqlCommand(monthSalesSql, cn))
        {
            var result = await cmd.ExecuteScalarAsync();
            metrics.MonthSales = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
        }

        // 3. Total products & total stock on hand
        const string stockSql = @"
            SELECT COUNT(*) AS total_products, ISNULL(SUM(qty), 0) AS total_stock
            FROM tblProduct
            WHERE ISNULL(is_active, 1) = 1;";

        await using (var cmd = new SqlCommand(stockSql, cn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                metrics.TotalProducts = Convert.ToInt32(reader["total_products"]);
                metrics.TotalStockOnHand = Convert.ToInt32(reader["total_stock"]);
            }
        }

        // 4. Critical items count & list
        const string critSql = @"
            SELECT p.pcode, p.barcode, p.pdescription, ISNULL(b.brand, '') AS brand, ISNULL(c.category, '') AS category, p.price, p.qty, p.reorder
            FROM tblProduct p
            LEFT JOIN tblBrand b ON b.id = p.brand_id
            LEFT JOIN tblCategory c ON c.id = p.category_id
            WHERE p.qty <= p.reorder AND ISNULL(p.is_active, 1) = 1
            ORDER BY p.qty ASC;";

        await using (var cmd = new SqlCommand(critSql, cn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                metrics.CriticalProducts.Add(new Product
                {
                    PCode = reader["pcode"]?.ToString() ?? string.Empty,
                    Barcode = reader["barcode"]?.ToString() ?? string.Empty,
                    Description = reader["pdescription"]?.ToString() ?? string.Empty,
                    BrandName = reader["brand"]?.ToString() ?? string.Empty,
                    CategoryName = reader["category"]?.ToString() ?? string.Empty,
                    Price = Convert.ToDecimal(reader["price"]),
                    Qty = Convert.ToInt32(reader["qty"]),
                    Reorder = Convert.ToInt32(reader["reorder"])
                });
            }
            metrics.CriticalItemsCount = metrics.CriticalProducts.Count;
        }

        // 5. Top selling items
        const string topSql = @"
            SELECT TOP 6 c.pid, p.pdescription, SUM(c.qty) AS tot_qty, SUM(c.total) AS tot_sales
            FROM tblCart c
            INNER JOIN tblProduct p ON p.pcode = c.pid
            WHERE c.status = 'Sold'
            GROUP BY c.pid, p.pdescription
            ORDER BY tot_qty DESC;";

        await using (var cmd = new SqlCommand(topSql, cn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                metrics.TopSelling.Add(new TopSellingItem
                {
                    PCode = reader["pid"]?.ToString() ?? string.Empty,
                    Description = reader["pdescription"]?.ToString() ?? string.Empty,
                    TotalQty = Convert.ToInt32(reader["tot_qty"]),
                    TotalSales = Convert.ToDecimal(reader["tot_sales"])
                });
            }
        }

        return metrics;
    }
}
