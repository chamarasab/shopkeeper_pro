namespace ShopKeeperPro.Core.Models;

public class CartItem
{
    public int Id { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string PCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Discount { get; set; }
    public decimal Total => (UnitPrice * Quantity) - Discount;
    public DateTime Date { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending";
    public string Cashier { get; set; } = string.Empty;
    public int StockOnHand { get; set; }
}

public class Sale
{
    public string TransactionNo { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public decimal CashTendered { get; set; }
    public decimal ChangeDue { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string Cashier { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public int ItemCount { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
