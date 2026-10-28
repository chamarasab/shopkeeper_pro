namespace ShopKeeperPro.Core.Models;

public class StockInItem
{
    public int Id { get; set; }
    public string RefNo { get; set; } = string.Empty;
    public string PCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string StockInBy { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int? VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
}

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Fax { get; set; }

    public override string ToString() => Name;
}

public class DashboardMetrics
{
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public int TodayTransactions { get; set; }
    public int TotalProducts { get; set; }
    public int TotalStockOnHand { get; set; }
    public int CriticalItemsCount { get; set; }
    public List<Product> CriticalProducts { get; set; } = new();
    public List<TopSellingItem> TopSelling { get; set; } = new();
}

public class TopSellingItem
{
    public string PCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalQty { get; set; }
    public decimal TotalSales { get; set; }
}
