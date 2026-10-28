namespace ShopKeeperPro.Core.Models;

public class Product
{
    public string PCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int Qty { get; set; }
    public int Reorder { get; set; } = 10;
    public bool IsActive { get; set; } = true;

    public bool IsLowStock => Qty <= Reorder;
    public bool IsOutOfStock => Qty <= 0;
}
