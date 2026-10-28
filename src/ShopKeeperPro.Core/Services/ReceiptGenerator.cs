using System.Text;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.Core.Services;

public class ReceiptGenerator
{
    public string StoreName { get; set; } = "SHOPKEEPER PRO RETAIL";
    public string StoreAddress { get; set; } = "123 Main Commercial Boulevard, Suite 400";
    public string Telephone { get; set; } = "+94 (11) 555-0199";
    public string TaxNumber { get; set; } = "TAX ID: 849-204-118-000";

    public string GenerateTextReceipt(Sale sale, List<CartItem> items)
    {
        const int width = 42;
        var sb = new StringBuilder();

        string Center(string text)
        {
            if (text.Length >= width) return text;
            int leftPad = (width - text.Length) / 2;
            return text.PadLeft(leftPad + text.Length).PadRight(width);
        }

        string Line() => new string('-', width);
        string DoubleLine() => new string('=', width);

        sb.AppendLine(DoubleLine());
        sb.AppendLine(Center(StoreName));
        sb.AppendLine(Center(StoreAddress));
        sb.AppendLine(Center($"Tel: {Telephone}"));
        sb.AppendLine(Center(TaxNumber));
        sb.AppendLine(DoubleLine());

        sb.AppendLine($"RECEIPT #: {sale.TransactionNo}");
        sb.AppendLine($"DATE     : {sale.Date:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"CASHIER  : {sale.Cashier}");
        if (!string.IsNullOrWhiteSpace(sale.CustomerName))
            sb.AppendLine($"CUSTOMER : {sale.CustomerName}");
        sb.AppendLine(Line());

        sb.AppendLine(string.Format("{0,-18} {1,4} {2,8} {3,9}", "ITEM", "QTY", "PRICE", "TOTAL"));
        sb.AppendLine(Line());

        foreach (var item in items)
        {
            string desc = item.Description.Length > 18 ? item.Description[..18] : item.Description;
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,8:N2} {3,9:N2}", desc, item.Quantity, item.UnitPrice, item.Total));
            if (item.Discount > 0)
            {
                sb.AppendLine($"  (Discount: -{item.Discount:N2})");
            }
        }

        sb.AppendLine(Line());
        sb.AppendLine(string.Format("{0,-25} {1,16:N2}", "SUBTOTAL:", sale.Subtotal));
        if (sale.Discount > 0)
            sb.AppendLine(string.Format("{0,-25} {1,16:N2}", "DISCOUNT:", -sale.Discount));
        sb.AppendLine(string.Format("{0,-25} {1,16:N2}", "VAT (TAX):", sale.Vat));
        sb.AppendLine(DoubleLine());
        sb.AppendLine(string.Format("{0,-20} {1,21:N2}", "GRAND TOTAL:", sale.Total));
        sb.AppendLine(DoubleLine());

        sb.AppendLine(string.Format("{0,-25} {1,16}", "PAYMENT METHOD:", sale.PaymentMethod));
        sb.AppendLine(string.Format("{0,-25} {1,16:N2}", "TENDERED AMOUNT:", sale.CashTendered));
        sb.AppendLine(string.Format("{0,-25} {1,16:N2}", "CHANGE DUE:", sale.ChangeDue));
        sb.AppendLine(Line());

        sb.AppendLine(Center("ITEMS SOLD: " + items.Sum(x => x.Quantity)));
        sb.AppendLine(Line());
        sb.AppendLine(Center("THANK YOU FOR SHOPPING WITH US!"));
        sb.AppendLine(Center("Goods once sold are returnable within 7 days."));
        sb.AppendLine(DoubleLine());

        return sb.ToString();
    }
}
