using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.WinForms.UI;

public class DashboardView : UserControl
{
    private readonly DashboardRepository _repo = new();

    private Label _lblTodaySales = null!;
    private Label _lblTodayTr = null!;
    private Label _lblMonthSales = null!;
    private Label _lblTotalStock = null!;
    private Label _lblCriticalCount = null!;
    private ModernDataGridView _gridCritical = null!;
    private ModernDataGridView _gridTop = null!;

    public DashboardView()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = UITheme.Background;
        Font = UITheme.FontBody;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UITheme.Background,
            Padding = new Padding(12)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f)); // KPI Cards
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));   // Critical stock table
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));   // Top selling items table

        // Row 0: KPI Metrics Cards
        var kpiFlow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        for (int i = 0; i < 5; i++) kpiFlow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        var cardToday = CreateKpiCard("TODAY'S REVENUE", "රු.0.00", UITheme.Success, out _lblTodaySales);
        var cardTr = CreateKpiCard("TODAY'S TRANSACTIONS", "0", UITheme.CyanAccent, out _lblTodayTr);
        var cardMonth = CreateKpiCard("MONTHLY REVENUE", "රු.0.00", UITheme.PrimaryLight, out _lblMonthSales);
        var cardStock = CreateKpiCard("TOTAL STOCK UNITS", "0", UITheme.TextPrimary, out _lblTotalStock);
        var cardCrit = CreateKpiCard("LOW STOCK ALERTS", "0 items", UITheme.Warning, out _lblCriticalCount);

        kpiFlow.Controls.Add(cardToday, 0, 0);
        kpiFlow.Controls.Add(cardTr, 1, 0);
        kpiFlow.Controls.Add(cardMonth, 2, 0);
        kpiFlow.Controls.Add(cardStock, 3, 0);
        kpiFlow.Controls.Add(cardCrit, 4, 0);

        mainLayout.Controls.Add(kpiFlow, 0, 0);

        // Row 1: Low Stock Alerts Card
        var critCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 4),
            Padding = new Padding(10),
            BorderRadius = 10,
            BackColor = UITheme.CardSurface
        };

        var lblCritHeader = new Label
        {
            Text = "CRITICAL / LOW STOCK ALERTS (REQUIRES REORDER)",
            Font = UITheme.FontHeader,
            ForeColor = UITheme.Warning,
            Dock = DockStyle.Top,
            Height = 28
        };

        _gridCritical = new ModernDataGridView { Dock = DockStyle.Fill };
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "PCode", FillWeight = 15 });
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBarcode", HeaderText = "Barcode", FillWeight = 20 });
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Product Description", FillWeight = 40 });
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCategory", HeaderText = "Category", FillWeight = 20 });
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "Qty on Hand", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold, ForeColor = UITheme.Danger } });
        _gridCritical.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColReorder", HeaderText = "Reorder Point", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

        critCard.Controls.Add(_gridCritical);
        critCard.Controls.Add(lblCritHeader);
        mainLayout.Controls.Add(critCard, 0, 1);

        // Row 2: Top Selling Products
        var topCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(10),
            BorderRadius = 10,
            BackColor = UITheme.CardSurface
        };

        var lblTopHeader = new Label
        {
            Text = "TOP SELLING PRODUCTS (BY VOLUME)",
            Font = UITheme.FontHeader,
            ForeColor = UITheme.CyanAccent,
            Dock = DockStyle.Top,
            Height = 28
        };

        _gridTop = new ModernDataGridView { Dock = DockStyle.Fill };
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPCode", HeaderText = "Product Code", FillWeight = 20 });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColName", HeaderText = "Description", FillWeight = 45 });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSold", HeaderText = "Units Sold", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold } });
        _gridTop.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSales", HeaderText = "Total Revenue", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", Font = UITheme.FontBodyBold, ForeColor = UITheme.Success } });

        topCard.Controls.Add(_gridTop);
        topCard.Controls.Add(lblTopHeader);
        mainLayout.Controls.Add(topCard, 0, 2);

        Controls.Add(mainLayout);
    }

    private static RoundedPanel CreateKpiCard(string title, string initialValue, Color valueColor, out Label valueLabel)
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(10),
            BorderRadius = 8,
            BackColor = UITheme.CardSurface
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(8, 8),
            AutoSize = true
        };

        valueLabel = new Label
        {
            Text = initialValue,
            Font = UITheme.FontTitle,
            ForeColor = valueColor,
            Location = new Point(8, 38),
            AutoSize = true
        };

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(valueLabel);
        return panel;
    }

    public async Task RefreshDashboardAsync()
    {
        var metrics = await _repo.GetMetricsAsync();

        _lblTodaySales.Text = $"රු.{metrics.TodaySales:N2}";
        _lblTodayTr.Text = $"{metrics.TodayTransactions}";
        _lblMonthSales.Text = $"රු.{metrics.MonthSales:N2}";
        _lblTotalStock.Text = $"{metrics.TotalStockOnHand:N0}";
        _lblCriticalCount.Text = $"{metrics.CriticalItemsCount} items";

        _gridCritical.Rows.Clear();
        foreach (var p in metrics.CriticalProducts)
        {
            _gridCritical.Rows.Add(p.PCode, p.Barcode, p.Description, p.CategoryName, p.Qty, p.Reorder);
        }

        _gridTop.Rows.Clear();
        foreach (var item in metrics.TopSelling)
        {
            _gridTop.Rows.Add(item.PCode, item.Description, item.TotalQty, item.TotalSales);
        }
    }
}
