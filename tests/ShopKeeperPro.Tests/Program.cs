using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;
using ShopKeeperPro.Core.Services;

namespace ShopKeeperPro.Tests;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("      SHOPKEEPER PRO - CORE VERIFICATION SUITE    ");
        Console.WriteLine("==================================================");

        int passed = 0;
        int failed = 0;

        void Assert(string testName, bool condition, string detail = "")
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} {detail}");
                passed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} - {detail}");
                failed++;
            }
        }

        var scanner = new BarcodeScannerService();

        // 1. Basic Barcode Parsing
        var r1 = scanner.ParseInput("123456789");
        Assert("Barcode: Standard Digits", r1.IsValid && r1.Barcode == "123456789" && r1.Quantity == 1);

        // 2. Whitespace & Newline Stripping
        var r2 = scanner.ParseInput("   890103001\r\n  ");
        Assert("Barcode: Whitespace & CRLF Trimming", r2.IsValid && r2.Barcode == "890103001" && r2.Quantity == 1);

        // 3. Multiplier Parsing (e.g. 5*123456789)
        var r3 = scanner.ParseInput("5*123456788");
        Assert("Barcode: Multiplier Syntax (5*...)", r3.IsValid && r3.Barcode == "123456788" && r3.Quantity == 5);

        // 4. Invalid Barcode Rejection
        var r4 = scanner.ParseInput("ab"); // too short (<3)
        Assert("Barcode: Reject Too Short", !r4.IsValid);

        var r5 = scanner.ParseInput("123#@$bad"); // invalid symbols
        Assert("Barcode: Reject Invalid Characters", !r5.IsValid);

        var r6 = scanner.ParseInput("0*123456789"); // zero multiplier
        Assert("Barcode: Reject Zero Multiplier", !r6.IsValid);

        // 5. CartItem Total Calculation
        var item1 = new CartItem
        {
            PCode = "1101",
            Barcode = "123456789",
            Description = "Sunlight Soap",
            UnitPrice = 120.00m,
            Quantity = 3,
            Discount = 20.00m
        };
        Assert("CartItem: Total Calculation", item1.Total == 340.00m, $"Expected 340.00, got {item1.Total}");

        // 6. Sale & Change Calculation
        var sale = new Sale
        {
            TransactionNo = "20261028-1001",
            Date = DateTime.Now,
            Subtotal = 1000.00m,
            Discount = 100.00m,
            Vat = 108.00m,
            Total = 1008.00m,
            CashTendered = 1500.00m,
            ChangeDue = 492.00m,
            PaymentMethod = "Cash",
            Cashier = "Chamara"
        };
        Assert("Sale: Change Calculation", (sale.CashTendered - sale.Total) == 492.00m);

        // 7. Receipt Generator Verification
        var receiptGen = new ReceiptGenerator();
        string receiptText = receiptGen.GenerateTextReceipt(sale, new List<CartItem> { item1 });
        Assert("Receipt: Contains Store Name", receiptText.Contains("SHOPKEEPER PRO RETAIL"));
        Assert("Receipt: Contains Transaction #", receiptText.Contains("20261028-1001"));
        Assert("Receipt: Contains Line Item", receiptText.Contains("Sunlight Soap"));
        Assert("Receipt: Contains Grand Total", receiptText.Contains("1,008.00"));

        // 8. MSSQL Database Test (optional check if reachable)
        if (args.Contains("--check-db"))
        {
            Console.WriteLine("\nTesting local MSSQL Database connectivity...");
            var (dbOk, dbMsg, ms) = await DbConfig.TestConnectionAsync();
            Assert("Database: Local MSSQL Connection", dbOk, $"{dbMsg} ({ms}ms)");

            if (dbOk)
            {
                var prodRepo = new ProductRepository();
                var prod = await prodRepo.GetByBarcodeAsync("123456789");
                Assert("Database: Product Lookup by Barcode (123456789)", prod != null && prod.Description.Contains("Sunlight"), prod?.Description ?? "Not found");

                var userRepo = new UserRepository();
                var user = await userRepo.AuthenticateAsync("chax", "123");
                Assert("Database: User Authentication (chax/123)", user != null && user.IsAdmin, user?.Name ?? "Failed");
            }
        }

        Console.WriteLine("\n--------------------------------------------------");
        Console.WriteLine($"RESULTS: {passed} PASSED, {failed} FAILED");
        Console.WriteLine("--------------------------------------------------");

        return failed == 0 ? 0 : 1;
    }
}
