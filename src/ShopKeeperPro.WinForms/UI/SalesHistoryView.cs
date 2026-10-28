using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;
using ShopKeeperPro.Core.Services;

namespace ShopKeeperPro.WinForms.UI;

public class SalesHistoryView : UserControl
{
    private readonly SaleRepository _repo = new();
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private ComboBox _cboCashier = null!;
    private ModernDataGridView _gridSales = null!;
    private Label _lblTotalRevenue = null!;
    private Label _lblTotalTransactions = null!;
    private List<Sale> _sales = new();

    public SalesHistoryView()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = UITheme.Background;
        Font = UITheme.FontBody;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12),
            BorderRadius = 10,
            BackColor = UITheme.CardSurface,
            Padding = new Padding(12)
        };

        // Top Filter Bar
        var topBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };

        var lblFrom = new Label { Text = "From Date:", Location = new Point(4, 14), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _dtpFrom = new DateTimePicker { Location = new Point(74, 10), Size = new Size(130, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };

        var lblTo = new Label { Text = "To Date:", Location = new Point(214, 14), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _dtpTo = new DateTimePicker { Location = new Point(270, 10), Size = new Size(130, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        _cboCashier = new ComboBox { Location = new Point(410, 10), Size = new Size(140, 30), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UITheme.InputBackground, ForeColor = UITheme.TextPrimary };
        _cboCashier.Items.AddRange(new object[] { "All", "Chamara", "Malith", "Waruna" });
        _cboCashier.SelectedIndex = 0;

        var btnSearch = new ModernButton
        {
            Text = "FILTER SALES",
            NormalColor = UITheme.Primary,
            HoverColor = UITheme.PrimaryDark,
            Location = new Point(560, 8),
            Size = new Size(130, 34)
        };
        btnSearch.Click += async (_, _) => await RefreshListAsync();

        var btnReprint = new ModernButton
        {
            Text = "REPRINT RECEIPT",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Location = new Point(700, 8),
            Size = new Size(150, 34)
        };
        btnReprint.Click += async (_, _) => await ReprintSelectedReceiptAsync();

        topBar.Controls.Add(lblFrom);
        topBar.Controls.Add(_dtpFrom);
        topBar.Controls.Add(lblTo);
        topBar.Controls.Add(_dtpTo);
        topBar.Controls.Add(_cboCashier);
        topBar.Controls.Add(btnSearch);
        topBar.Controls.Add(btnReprint);

        // Grid
        _gridSales = new ModernDataGridView { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8) };
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTr", HeaderText = "Transaction #", FillWeight = 18 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDate", HeaderText = "Date / Time", FillWeight = 20 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColItems", HeaderText = "Items", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSubtotal", HeaderText = "Subtotal", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDisc", HeaderText = "Disc", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColVat", HeaderText = "VAT", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "Grand Total", FillWeight = 16, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", Font = UITheme.FontBodyBold, ForeColor = UITheme.CyanAccent } });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPayment", HeaderText = "Payment", FillWeight = 14 });
        _gridSales.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCashier", HeaderText = "Cashier", FillWeight = 14 });

        // Bottom Summary Bar
        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 42, BackColor = Color.Transparent };
        _lblTotalTransactions = new Label { Text = "Transactions: 0", Location = new Point(4, 12), AutoSize = true, Font = UITheme.FontBodyBold, ForeColor = UITheme.TextSecondary };
        _lblTotalRevenue = new Label { Text = "Total Sales: රු.0.00", Location = new Point(200, 12), AutoSize = true, Font = UITheme.FontHeader, ForeColor = UITheme.Success };
        bottomBar.Controls.Add(_lblTotalTransactions);
        bottomBar.Controls.Add(_lblTotalRevenue);

        panel.Controls.Add(_gridSales);
        panel.Controls.Add(bottomBar);
        panel.Controls.Add(topBar);
        Controls.Add(panel);
    }

    public async Task InitializeAsync()
    {
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        string? cashier = _cboCashier.SelectedItem?.ToString();
        _sales = await _repo.GetSalesHistoryAsync(_dtpFrom.Value, _dtpTo.Value, cashier);
        _gridSales.Rows.Clear();

        foreach (var s in _sales)
        {
            _gridSales.Rows.Add(
                s.TransactionNo,
                s.Date.ToString("yyyy-MM-dd HH:mm"),
                s.ItemCount,
                s.Subtotal,
                s.Discount,
                s.Vat,
                s.Total,
                s.PaymentMethod,
                s.Cashier
            );
        }

        decimal totalRev = _sales.Sum(x => x.Total);
        _lblTotalTransactions.Text = $"Transactions: {_sales.Count}";
        _lblTotalRevenue.Text = $"Total Revenue: රු.{totalRev:N2}";
    }

    private async Task ReprintSelectedReceiptAsync()
    {
        if (_gridSales.SelectedRows.Count == 0) return;
        int idx = _gridSales.SelectedRows[0].Index;
        if (idx >= _sales.Count) return;

        var briefSale = _sales[idx];
        var fullSale = await _repo.GetSaleByTransactionAsync(briefSale.TransactionNo);
        if (fullSale == null)
        {
            MessageBox.Show("Could not load transaction details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var generator = new ReceiptGenerator();
        string text = generator.GenerateTextReceipt(fullSale, fullSale.Items);
        using var dlg = new ReceiptDialog(text);
        dlg.ShowDialog();
    }
}
