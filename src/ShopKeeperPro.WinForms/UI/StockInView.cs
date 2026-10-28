using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.WinForms.UI;

public class StockInView : UserControl
{
    private readonly StockRepository _stockRepo = new();
    private readonly ProductRepository _prodRepo = new();
    private readonly CategoryBrandVendorRepository _cbvRepo = new();

    private ModernTextBox _txtRefNo = null!;
    private ComboBox _cboVendor = null!;
    private ModernTextBox _txtProductSearch = null!;
    private ModernTextBox _txtQty = null!;
    private ModernDataGridView _gridStockIn = null!;
    private Label _lblSelectedProduct = null!;
    private Product? _activeProduct;
    private List<Vendor> _vendors = new();

    public string CurrentUser { get; set; } = "Chamara";

    public StockInView()
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

        // Header inputs
        var topCard = new RoundedPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BorderRadius = 8,
            BackColor = UITheme.InputBackground,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8)
        };

        var lblRef = new Label { Text = "Reference / Invoice #:", Location = new Point(8, 8), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _txtRefNo = new ModernTextBox { Location = new Point(8, 28), Size = new Size(180, 36), Text = $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now:fff}" };

        var lblVendor = new Label { Text = "Supplier / Vendor:", Location = new Point(200, 8), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _cboVendor = new ComboBox { Location = new Point(200, 32), Size = new Size(200, 32), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UITheme.InputBackground, ForeColor = UITheme.TextPrimary };

        var lblSearch = new Label { Text = "Search Product (Scan Barcode or Type):", Location = new Point(410, 8), AutoSize = true, ForeColor = UITheme.PrimaryLight };
        _txtProductSearch = new ModernTextBox { Location = new Point(410, 28), Size = new Size(260, 36), PlaceholderText = "Scan barcode or enter name..." };
        _txtProductSearch.InnerTextBox.KeyDown += TxtProductSearch_KeyDown;

        var lblQty = new Label { Text = "Quantity:", Location = new Point(680, 8), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _txtQty = new ModernTextBox { Location = new Point(680, 28), Size = new Size(80, 36), Text = "10" };

        var btnAddRow = new ModernButton
        {
            Text = "+ ADD TO BATCH",
            NormalColor = UITheme.Primary,
            HoverColor = UITheme.PrimaryDark,
            Location = new Point(770, 28),
            Size = new Size(130, 36)
        };
        btnAddRow.Click += async (_, _) => await AddItemToBatchAsync();

        _lblSelectedProduct = new Label
        {
            Text = "Selected Product: None (Search above)",
            Location = new Point(8, 76),
            AutoSize = true,
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.CyanAccent
        };

        topCard.Controls.Add(lblRef);
        topCard.Controls.Add(_txtRefNo);
        topCard.Controls.Add(lblVendor);
        topCard.Controls.Add(_cboVendor);
        topCard.Controls.Add(lblSearch);
        topCard.Controls.Add(_txtProductSearch);
        topCard.Controls.Add(lblQty);
        topCard.Controls.Add(_txtQty);
        topCard.Controls.Add(btnAddRow);
        topCard.Controls.Add(_lblSelectedProduct);

        // DataGridView
        _gridStockIn = new ModernDataGridView { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 8) };
        _gridStockIn.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColId", HeaderText = "ID", FillWeight = 10 });
        _gridStockIn.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "Product Code", FillWeight = 15 });
        _gridStockIn.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Description", FillWeight = 40 });
        _gridStockIn.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "Stock In Qty", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = UITheme.FontBodyBold, ForeColor = UITheme.Success } });
        _gridStockIn.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColVendor", HeaderText = "Vendor", FillWeight = 20 });

        // Bottom Action Bar
        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };
        var btnFinalize = new ModernButton
        {
            Text = "FINALIZE & SAVE TO INVENTORY",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Dock = DockStyle.Right,
            Size = new Size(260, 42)
        };
        btnFinalize.Click += async (_, _) => await FinalizeStockAsync();
        bottomBar.Controls.Add(btnFinalize);

        panel.Controls.Add(_gridStockIn);
        panel.Controls.Add(bottomBar);
        panel.Controls.Add(topCard);
        Controls.Add(panel);
    }

    public async Task InitializeAsync()
    {
        _vendors = await _cbvRepo.GetVendorsAsync();
        _cboVendor.Items.Clear();
        foreach (var v in _vendors) _cboVendor.Items.Add(v);
        if (_cboVendor.Items.Count > 0) _cboVendor.SelectedIndex = 0;

        await RefreshBatchGridAsync();
    }

    private async void TxtProductSearch_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            string term = _txtProductSearch.Text.Trim();
            if (string.IsNullOrEmpty(term)) return;

            var p = await _prodRepo.GetByBarcodeAsync(term) ?? await _prodRepo.GetByPCodeAsync(term);
            if (p == null)
            {
                var searchResults = await _prodRepo.SearchAsync(term, null, activeOnly: true);
                p = searchResults.FirstOrDefault();
            }

            if (p != null)
            {
                _activeProduct = p;
                _lblSelectedProduct.Text = $"Selected Product: [{p.PCode}] {p.Description} (Current Stock: {p.Qty})";
                _txtQty.FocusInput();
            }
            else
            {
                MessageBox.Show($"Product not found for '{term}'", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private async Task AddItemToBatchAsync()
    {
        if (_activeProduct == null)
        {
            MessageBox.Show("Please select a product first by scanning or searching.", "Product Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!int.TryParse(_txtQty.Text, out int qty) || qty <= 0)
        {
            MessageBox.Show("Please enter a valid positive quantity.", "Invalid Quantity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int? vendorId = (_cboVendor.SelectedItem as Vendor)?.Id;
        await _stockRepo.AddStockInItemAsync(_txtRefNo.Text.Trim(), _activeProduct.PCode, qty, CurrentUser, vendorId);

        _activeProduct = null;
        _lblSelectedProduct.Text = "Selected Product: None (Search above)";
        _txtProductSearch.Text = string.Empty;
        _txtProductSearch.FocusInput();

        await RefreshBatchGridAsync();
    }

    private async Task RefreshBatchGridAsync()
    {
        var items = await _stockRepo.GetPendingStockInAsync(_txtRefNo.Text.Trim());
        _gridStockIn.Rows.Clear();
        foreach (var item in items)
        {
            _gridStockIn.Rows.Add(item.Id, item.PCode, item.Description, item.Quantity, item.VendorName);
        }
    }

    private async Task FinalizeStockAsync()
    {
        string refNo = _txtRefNo.Text.Trim();
        var items = await _stockRepo.GetPendingStockInAsync(refNo);
        if (items.Count == 0)
        {
            MessageBox.Show("No pending items in this stock-in batch.", "Batch Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        bool ok = await _stockRepo.FinalizeStockInAsync(refNo);
        if (ok)
        {
            MessageBox.Show($"Stock-in batch {refNo} finalized! Inventory has been updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _txtRefNo.Text = $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now:fff}";
            await RefreshBatchGridAsync();
        }
        else
        {
            MessageBox.Show("Failed to finalize stock-in batch.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
