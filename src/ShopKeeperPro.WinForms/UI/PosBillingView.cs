using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;
using ShopKeeperPro.Core.Services;

namespace ShopKeeperPro.WinForms.UI;

public class PosBillingView : UserControl
{
    private readonly ProductRepository _productRepo = new();
    private readonly CartRepository _cartRepo = new();
    private readonly SaleRepository _saleRepo = new();
    private readonly CategoryBrandVendorRepository _categoryRepo = new();
    private readonly BarcodeScannerService _scanner;

    // Active state
    private string _currentTransactionNo = string.Empty;
    private string _currentCashier = "Chamara";
    private decimal _vatRate = 0.12m;
    private List<CartItem> _currentCart = new();
    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private int? _selectedCategoryId = null;

    // Controls - Left Column
    private TableLayoutPanel _mainLayout = null!;
    private RoundedPanel _cartPanel = null!;
    private ModernTextBox _txtBarcode = null!;
    private ModernDataGridView _gridCart = null!;
    private Label _lblScannerStatus = null!;
    private Label _lblSubtotalVal = null!;
    private Label _lblDiscountVal = null!;
    private Label _lblVatVal = null!;
    private Label _lblTotalVal = null!;
    private Label _lblItemsCount = null!;
    private Label _lblTrNo = null!;
    private ModernButton _btnPay = null!;

    // Controls - Right Column
    private RoundedPanel _lookupPanel = null!;
    private ModernTextBox _txtSearch = null!;
    private FlowLayoutPanel _categoryPills = null!;
    private FlowLayoutPanel _productGrid = null!;

    public event Action? OnRequestReprint;

    public string CurrentCashier
    {
        get => _currentCashier;
        set
        {
            _currentCashier = value;
            if (_lblTrNo != null) _lblTrNo.Text = $"TR: {_currentTransactionNo} | Cashier: {_currentCashier}";
        }
    }

    public PosBillingView(BarcodeScannerService scanner)
    {
        _scanner = scanner;
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = UITheme.Background;
        Font = UITheme.FontBody;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UITheme.Background,
            Padding = new Padding(8)
        };

        // 58% Left (Cart & Summary), 42% Right (Catalog & Touch Numpad)
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildLeftCartPanel();
        BuildRightCatalogPanel();

        Controls.Add(_mainLayout);
    }

    private void BuildLeftCartPanel()
    {
        _cartPanel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(10),
            BorderRadius = 10,
            BackColor = UITheme.CardSurface
        };

        var leftTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f)); // Barcode & Status
        leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f)); // Action Bar
        leftTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Cart Grid
        leftTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f)); // Totals & Pay

        // Row 0: Barcode Scanner Header Card
        var barcodeHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        barcodeHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        barcodeHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var barcodeTopLine = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var lblBarcodeTitle = new Label
        {
            Text = "BARCODE SCANNER (AUTO-FOCUS)",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.PrimaryLight,
            Dock = DockStyle.Left,
            AutoSize = true
        };

        _lblScannerStatus = new Label
        {
            Text = "SCANNER READY",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.Success,
            Location = new Point(230, 2),
            AutoSize = true
        };

        _lblTrNo = new Label
        {
            Text = "TR: LOADING...",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Dock = DockStyle.Right,
            AutoSize = true
        };

        barcodeTopLine.Controls.Add(_lblScannerStatus);
        barcodeTopLine.Controls.Add(_lblTrNo);
        barcodeTopLine.Controls.Add(lblBarcodeTitle);

        _txtBarcode = new ModernTextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Scan barcode or type & press [↵] (e.g. 123456789 or 3*123456789)..."
        };
        _txtBarcode.InnerTextBox.KeyDown += TxtBarcode_KeyDown;

        barcodeHeader.Controls.Add(barcodeTopLine, 0, 0);
        barcodeHeader.Controls.Add(_txtBarcode, 0, 1);
        leftTable.Controls.Add(barcodeHeader, 0, 0);

        // Row 1: Action Bar with Hotkeys (6-column responsive table)
        var actionBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 4)
        };
        for (int i = 0; i < 6; i++) actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666f));
        actionBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var btnF1 = CreateActionButton("[F1] New", UITheme.PrimaryDark, () => StartNewTransactionAsync());
        var btnF2 = CreateActionButton("[F2] Qty", UITheme.CardSurfaceHover, () => PromptChangeQuantity());
        var btnF3 = CreateActionButton("[F3] Disc", UITheme.CardSurfaceHover, () => PromptDiscount());
        var btnF4 = CreateActionButton("[F4] Remove", UITheme.Danger, () => RemoveSelectedCartItemAsync());
        var btnF5 = CreateActionButton("[F5] Settle", UITheme.SuccessDark, () => OpenSettleDialogAsync());
        var btnF6 = CreateActionButton("[F6] Clear", Color.FromArgb(71, 85, 105), () => ClearCartAsync());

        actionBar.Controls.Add(btnF1, 0, 0);
        actionBar.Controls.Add(btnF2, 1, 0);
        actionBar.Controls.Add(btnF3, 2, 0);
        actionBar.Controls.Add(btnF4, 3, 0);
        actionBar.Controls.Add(btnF5, 4, 0);
        actionBar.Controls.Add(btnF6, 5, 0);
        leftTable.Controls.Add(actionBar, 0, 1);

        // Row 2: DataGridView for Cart Items
        _gridCart = new ModernDataGridView
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };

        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColIndex", HeaderText = "#", FillWeight = 8, ReadOnly = true });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBarcode", HeaderText = "Barcode", FillWeight = 20, ReadOnly = true });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Product Description", FillWeight = 38, ReadOnly = true });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrice", HeaderText = "Price", FillWeight = 16, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "Qty", FillWeight = 10, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDisc", HeaderText = "Disc", FillWeight = 12, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
        _gridCart.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTotal", HeaderText = "Total", FillWeight = 18, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", Font = UITheme.FontBodyBold, ForeColor = UITheme.CyanAccent } });

        leftTable.Controls.Add(_gridCart, 0, 2);

        // Row 3: Totals Summary & Large Checkout Button
        var bottomTotals = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BorderRadius = 8,
            BackColor = UITheme.InputBackground,
            Padding = new Padding(12),
            Margin = new Padding(0, 4, 0, 0)
        };

        var totalsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        totalsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        totalsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        totalsTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Subtotals Breakdown (Left)
        var breakdownPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        breakdownPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        breakdownPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _lblItemsCount = CreateTotalLabel("Items Count: 0", UITheme.TextSecondary, UITheme.FontSmallBold);
        var lblSubtotalTitle = CreateTotalLabel("Subtotal:", UITheme.TextSecondary, UITheme.FontBody);
        _lblSubtotalVal = CreateTotalLabel("රු.0.00", UITheme.TextPrimary, UITheme.FontBodyBold, ContentAlignment.MiddleRight);
        var lblDiscountTitle = CreateTotalLabel("Discount:", UITheme.Warning, UITheme.FontBody);
        _lblDiscountVal = CreateTotalLabel("-රු.0.00", UITheme.Warning, UITheme.FontBodyBold, ContentAlignment.MiddleRight);
        var lblVatTitle = CreateTotalLabel("VAT (12%):", UITheme.TextSecondary, UITheme.FontBody);
        _lblVatVal = CreateTotalLabel("රු.0.00", UITheme.TextSecondary, UITheme.FontBody, ContentAlignment.MiddleRight);

        breakdownPanel.Controls.Add(lblSubtotalTitle, 0, 0);
        breakdownPanel.Controls.Add(_lblSubtotalVal, 1, 0);
        breakdownPanel.Controls.Add(lblDiscountTitle, 0, 1);
        breakdownPanel.Controls.Add(_lblDiscountVal, 1, 1);
        breakdownPanel.Controls.Add(lblVatTitle, 0, 2);
        breakdownPanel.Controls.Add(_lblVatVal, 1, 2);
        breakdownPanel.Controls.Add(_lblItemsCount, 0, 3);
        breakdownPanel.SetColumnSpan(_lblItemsCount, 2);

        // Grand Total Card & Pay Button (Right)
        var grandTotalPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        grandTotalPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        grandTotalPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        grandTotalPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

        var lblGrandTotalHeader = new Label
        {
            Text = "TOTAL PAYABLE",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.Success,
            Dock = DockStyle.Top,
            AutoSize = true
        };

        _lblTotalVal = new Label
        {
            Text = "රු.0.00",
            Font = UITheme.FontGrandTotal,
            ForeColor = UITheme.Success,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        _btnPay = new ModernButton
        {
            Text = "[F5] PAY & PRINT RECEIPT",
            Font = UITheme.FontHeader,
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            PressedColor = Color.FromArgb(4, 120, 87),
            Dock = DockStyle.Fill,
            BorderRadius = 8
        };
        _btnPay.Click += async (_, _) => await OpenSettleDialogAsync();

        grandTotalPanel.Controls.Add(lblGrandTotalHeader, 0, 0);
        grandTotalPanel.Controls.Add(_lblTotalVal, 0, 1);
        grandTotalPanel.Controls.Add(_btnPay, 0, 2);

        totalsTable.Controls.Add(breakdownPanel, 0, 0);
        totalsTable.Controls.Add(grandTotalPanel, 1, 0);
        bottomTotals.Controls.Add(totalsTable);

        leftTable.Controls.Add(bottomTotals, 0, 3);
        _cartPanel.Controls.Add(leftTable);
        _mainLayout.Controls.Add(_cartPanel, 0, 0);
    }

    private void BuildRightCatalogPanel()
    {
        _lookupPanel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Padding = new Padding(10),
            BorderRadius = 10,
            BackColor = UITheme.CardSurface
        };

        var rightTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        rightTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f)); // Search box
        rightTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Category chips
        rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Product tiles grid
        rightTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f)); // Numpad & Quick Cash

        // Row 0: Search input
        _txtSearch = new ModernTextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search product by name or barcode (F7)...",
            Height = 40
        };
        _txtSearch.InnerTextBox.TextChanged += async (_, _) => await SearchProductsAsync(_txtSearch.Text);
        rightTable.Controls.Add(_txtSearch, 0, 0);

        // Row 1: Category Filter Pills
        _categoryPills = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0, 2, 0, 2)
        };
        rightTable.Controls.Add(_categoryPills, 0, 1);

        // Row 2: Product Tiles Grid
        _productGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true,
            Padding = new Padding(2),
            Margin = new Padding(0, 4, 0, 4)
        };
        rightTable.Controls.Add(_productGrid, 0, 2);

        // Row 3: Touch Numpad & Quick Cash
        var numpadTable = BuildNumpadTable();
        rightTable.Controls.Add(numpadTable, 0, 3);

        _lookupPanel.Controls.Add(rightTable);
        _mainLayout.Controls.Add(_lookupPanel, 1, 0);
    }

    private TableLayoutPanel BuildNumpadTable()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0)
        };

        for (int c = 0; c < 5; c++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        for (int r = 0; r < 3; r++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        string[,] keys = {
            { "7", "8", "9", "+100", "+500" },
            { "4", "5", "6", "+1000", "+2000" },
            { "1", "2", "3", "CLR", "+5000" }
        };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                string keyText = keys[row, col];
                var btn = new ModernButton
                {
                    Text = keyText,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    Font = UITheme.FontBodyBold,
                    BorderRadius = 6,
                    NormalColor = keyText.StartsWith("+") ? UITheme.SuccessDark : (keyText == "CLR" ? UITheme.Danger : UITheme.CardSurfaceHover),
                    HoverColor = UITheme.Primary
                };
                btn.Click += (_, _) => HandleNumpadKey(keyText);
                table.Controls.Add(btn, col, row);
            }
        }

        return table;
    }

    private void HandleNumpadKey(string key)
    {
        if (key == "CLR")
        {
            _txtBarcode.Text = string.Empty;
        }
        else if (key.StartsWith("+"))
        {
            // Quick cash tender helper -> opens settlement dialog with amount
            if (decimal.TryParse(key.Replace("+", ""), out decimal cashAmount))
            {
                _ = OpenSettleDialogWithTenderAsync(cashAmount);
            }
        }
        else
        {
            _txtBarcode.Text += key;
        }
        _txtBarcode.FocusInput();
    }

    private ModernButton CreateActionButton(string text, Color bg, Action onClick)
    {
        var btn = new ModernButton
        {
            Text = text,
            NormalColor = bg,
            HoverColor = UITheme.Primary,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 0, 2, 0),
            Font = UITheme.FontSmallBold,
            BorderRadius = 6
        };
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private static Label CreateTotalLabel(string text, Color color, Font font, ContentAlignment align = ContentAlignment.MiddleLeft)
    {
        return new Label
        {
            Text = text,
            ForeColor = color,
            Font = font,
            TextAlign = align,
            Dock = DockStyle.Fill,
            AutoSize = false
        };
    }

    public async Task InitializeAsync()
    {
        _vatRate = await _saleRepo.GetVatRateAsync();
        await StartNewTransactionAsync();
        await LoadCategoriesAsync();
        await LoadProductsAsync();
        _txtBarcode.FocusInput();
    }

    public async Task StartNewTransactionAsync()
    {
        _currentTransactionNo = await _saleRepo.GenerateTransactionNumberAsync();
        _lblTrNo.Text = $"TR: {_currentTransactionNo} | Cashier: {_currentCashier}";
        _currentCart.Clear();
        _gridCart.Rows.Clear();
        UpdateTotalsDisplay();
        _txtBarcode.Text = string.Empty;
        _txtBarcode.FocusInput();
    }

    private async Task LoadCategoriesAsync()
    {
        _categoryPills.Controls.Clear();
        var categories = await _categoryRepo.GetCategoriesAsync();

        var allBtn = CreatePillButton("All Items", null);
        _categoryPills.Controls.Add(allBtn);

        foreach (var cat in categories)
        {
            _categoryPills.Controls.Add(CreatePillButton(cat.Name, cat.Id));
        }
    }

    private ModernButton CreatePillButton(string text, int? categoryId)
    {
        var btn = new ModernButton
        {
            Text = text,
            Height = 30,
            AutoSize = true,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(2, 2, 4, 2),
            Font = UITheme.FontSmallBold,
            BorderRadius = 14,
            NormalColor = _selectedCategoryId == categoryId ? UITheme.Primary : UITheme.CardSurfaceHover,
            HoverColor = UITheme.PrimaryDark
        };
        btn.Click += async (_, _) =>
        {
            _selectedCategoryId = categoryId;
            foreach (Control c in _categoryPills.Controls)
            {
                if (c is ModernButton mb)
                    mb.NormalColor = UITheme.CardSurfaceHover;
            }
            btn.NormalColor = UITheme.Primary;
            await FilterProductsAsync();
        };
        return btn;
    }

    private async Task LoadProductsAsync()
    {
        _allProducts = await _productRepo.GetAllAsync(activeOnly: true);
        await FilterProductsAsync();
    }

    private async Task SearchProductsAsync(string query)
    {
        _filteredProducts = await _productRepo.SearchAsync(query, _selectedCategoryId, activeOnly: true);
        RenderProductTiles();
    }

    private Task FilterProductsAsync()
    {
        if (_selectedCategoryId.HasValue)
        {
            _filteredProducts = _allProducts.Where(p => p.CategoryId == _selectedCategoryId.Value).ToList();
        }
        else
        {
            _filteredProducts = _allProducts.ToList();
        }
        RenderProductTiles();
        return Task.CompletedTask;
    }

    private void RenderProductTiles()
    {
        _productGrid.SuspendLayout();
        _productGrid.Controls.Clear();

        foreach (var p in _filteredProducts)
        {
            var card = new RoundedPanel
            {
                Size = new Size(138, 100),
                Margin = new Padding(4),
                Padding = new Padding(8),
                BorderRadius = 8,
                BackColor = UITheme.InputBackground,
                Cursor = Cursors.Hand
            };

            var lblName = new Label
            {
                Text = p.Description,
                Font = UITheme.FontBodyBold,
                ForeColor = UITheme.TextPrimary,
                Location = new Point(6, 6),
                Size = new Size(124, 38),
                AutoEllipsis = true
            };

            var lblPrice = new Label
            {
                Text = $"රු.{p.Price:N2}",
                Font = UITheme.FontHeader,
                ForeColor = UITheme.CyanAccent,
                Location = new Point(6, 46),
                AutoSize = true
            };

            var badge = new StatusBadge
            {
                Text = $"{p.Qty} left",
                BadgeColor = p.IsOutOfStock ? UITheme.Danger : (p.IsLowStock ? UITheme.Warning : UITheme.Success),
                Location = new Point(6, 72),
                Size = new Size(74, 20)
            };

            card.Controls.Add(lblName);
            card.Controls.Add(lblPrice);
            card.Controls.Add(badge);

            void HandleClick(object? s, EventArgs e) => _ = AddProductToCartDirectAsync(p, 1);
            card.Click += HandleClick;
            lblName.Click += HandleClick;
            lblPrice.Click += HandleClick;
            badge.Click += HandleClick;

            _productGrid.Controls.Add(card);
        }

        _productGrid.ResumeLayout();
    }

    private async void TxtBarcode_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            string input = _txtBarcode.Text;
            if (!string.IsNullOrWhiteSpace(input))
            {
                await ProcessBarcodeScanAsync(input);
            }
        }
    }

    public async Task ProcessBarcodeScanAsync(string input)
    {
        var result = _scanner.ParseInput(input);
        if (!result.IsValid)
        {
            _scanner.NotifyScanAlert();
            FlashScannerStatus($"ERROR: {result.ErrorMessage}", UITheme.Danger);
            return;
        }

        var product = await _productRepo.GetByBarcodeAsync(result.Barcode);
        if (product == null)
        {
            // Try lookup by PCode as fallback
            product = await _productRepo.GetByPCodeAsync(result.Barcode);
        }

        if (product == null)
        {
            _scanner.NotifyScanAlert();
            FlashScannerStatus($"BARCODE NOT FOUND: {result.Barcode}", UITheme.Warning);
            return;
        }

        await AddProductToCartDirectAsync(product, result.Quantity);
    }

    private async Task AddProductToCartDirectAsync(Product product, int quantity)
    {
        var (success, message, item) = await _cartRepo.AddToCartAsync(_currentTransactionNo, product, quantity, _currentCashier);
        if (!success)
        {
            _scanner.NotifyScanAlert();
            FlashScannerStatus(message, UITheme.Warning);
            MessageBox.Show(message, "Stock Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _scanner.NotifyScanSuccess();
        FlashScannerStatus($"SCANNED: {product.Description} (x{quantity})", UITheme.Success);
        _txtBarcode.Text = string.Empty;
        _txtBarcode.FocusInput();

        await RefreshCartDisplayAsync();
    }

    private void FlashScannerStatus(string message, Color color)
    {
        _lblScannerStatus.Text = message;
        _lblScannerStatus.ForeColor = color;
    }

    private async Task RefreshCartDisplayAsync()
    {
        _currentCart = await _cartRepo.GetCartItemsAsync(_currentTransactionNo);
        _gridCart.Rows.Clear();

        int index = 1;
        foreach (var item in _currentCart)
        {
            _gridCart.Rows.Add(
                index++,
                item.Barcode,
                item.Description,
                item.UnitPrice,
                item.Quantity,
                item.Discount,
                item.Total
            );
        }

        if (_gridCart.Rows.Count > 0)
        {
            _gridCart.ClearSelection();
            _gridCart.Rows[^1].Selected = true;
            _gridCart.FirstDisplayedScrollingRowIndex = _gridCart.Rows.Count - 1;
        }

        UpdateTotalsDisplay();
    }

    private void UpdateTotalsDisplay()
    {
        decimal subtotal = _currentCart.Sum(x => x.UnitPrice * x.Quantity);
        decimal discount = _currentCart.Sum(x => x.Discount);
        decimal vat = (subtotal - discount) * _vatRate;
        decimal grandTotal = (subtotal - discount) + vat;
        int totalItems = _currentCart.Sum(x => x.Quantity);

        _lblItemsCount.Text = $"Total Items in Cart: {totalItems}";
        _lblSubtotalVal.Text = $"රු.{subtotal:N2}";
        _lblDiscountVal.Text = $"-රු.{discount:N2}";
        _lblVatVal.Text = $"රු.{vat:N2}";
        _lblTotalVal.Text = $"රු.{grandTotal:N2}";
    }

    private void PromptChangeQuantity()
    {
        if (_gridCart.SelectedRows.Count == 0) return;
        int rowIndex = _gridCart.SelectedRows[0].Index;
        if (rowIndex >= _currentCart.Count) return;

        var item = _currentCart[rowIndex];
        using var dialog = new QuantityDiscountDialog("Change Quantity", "Enter new quantity:", item.Quantity.ToString());
        if (dialog.ShowDialog() == DialogResult.OK && int.TryParse(dialog.Value, out int newQty))
        {
            _ = UpdateCartItemQuantityAsync(item.Id, newQty, item.StockOnHand);
        }
    }

    private async Task UpdateCartItemQuantityAsync(int cartId, int newQty, int stock)
    {
        if (newQty <= 0)
        {
            await _cartRepo.RemoveItemAsync(cartId);
        }
        else
        {
            bool ok = await _cartRepo.UpdateItemQuantityAsync(cartId, newQty, stock);
            if (!ok)
            {
                MessageBox.Show($"Cannot set quantity to {newQty}. Only {stock} available in stock.", "Stock Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        await RefreshCartDisplayAsync();
    }

    private void PromptDiscount()
    {
        if (_gridCart.SelectedRows.Count == 0) return;
        int rowIndex = _gridCart.SelectedRows[0].Index;
        if (rowIndex >= _currentCart.Count) return;

        var item = _currentCart[rowIndex];
        using var dialog = new QuantityDiscountDialog("Apply Item Discount", "Enter discount amount (රු.):", item.Discount.ToString("0.00"));
        if (dialog.ShowDialog() == DialogResult.OK && decimal.TryParse(dialog.Value, out decimal disc))
        {
            _ = UpdateCartItemDiscountAsync(item.Id, disc);
        }
    }

    private async Task UpdateCartItemDiscountAsync(int cartId, decimal discount)
    {
        await _cartRepo.UpdateItemDiscountAsync(cartId, Math.Max(0, discount));
        await RefreshCartDisplayAsync();
    }

    private async Task RemoveSelectedCartItemAsync()
    {
        if (_gridCart.SelectedRows.Count == 0) return;
        int rowIndex = _gridCart.SelectedRows[0].Index;
        if (rowIndex >= _currentCart.Count) return;

        var item = _currentCart[rowIndex];
        await _cartRepo.RemoveItemAsync(item.Id);
        await RefreshCartDisplayAsync();
    }

    private async Task ClearCartAsync()
    {
        if (_currentCart.Count == 0) return;
        var confirm = MessageBox.Show("Are you sure you want to cancel and clear the active cart?", "Clear Cart", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _cartRepo.ClearCartAsync(_currentTransactionNo);
            await StartNewTransactionAsync();
        }
    }

    public async Task OpenSettleDialogAsync()
    {
        if (_currentCart.Count == 0)
        {
            MessageBox.Show("Cannot settle an empty cart. Please scan or add items first.", "Cart Empty", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        decimal subtotal = _currentCart.Sum(x => x.UnitPrice * x.Quantity);
        decimal discount = _currentCart.Sum(x => x.Discount);
        decimal vat = (subtotal - discount) * _vatRate;
        decimal grandTotal = (subtotal - discount) + vat;

        using var settleDialog = new SettleDialog(grandTotal, _currentTransactionNo);
        if (settleDialog.ShowDialog() == DialogResult.OK)
        {
            var sale = new Sale
            {
                TransactionNo = _currentTransactionNo,
                Date = DateTime.Now,
                Subtotal = subtotal,
                Discount = discount,
                Vat = vat,
                Total = grandTotal,
                CashTendered = settleDialog.TenderedAmount,
                ChangeDue = settleDialog.ChangeDue,
                PaymentMethod = settleDialog.PaymentMethod,
                Cashier = _currentCashier,
                CustomerName = settleDialog.CustomerName
            };

            var (success, message) = await _saleRepo.ProcessCheckoutAsync(sale, _currentCart);
            if (!success)
            {
                MessageBox.Show(message, "Checkout Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Print / Preview Receipt
            var receiptGen = new ReceiptGenerator();
            string receiptText = receiptGen.GenerateTextReceipt(sale, _currentCart);
            using var receiptDialog = new ReceiptDialog(receiptText);
            receiptDialog.ShowDialog();

            // Refresh stock tiles in catalog
            await LoadProductsAsync();

            // Reset for next sale immediately!
            await StartNewTransactionAsync();
        }
    }

    public async Task OpenSettleDialogWithTenderAsync(decimal initialTender)
    {
        if (_currentCart.Count == 0) return;
        decimal subtotal = _currentCart.Sum(x => x.UnitPrice * x.Quantity);
        decimal discount = _currentCart.Sum(x => x.Discount);
        decimal vat = (subtotal - discount) * _vatRate;
        decimal grandTotal = (subtotal - discount) + vat;

        using var settleDialog = new SettleDialog(grandTotal, _currentTransactionNo, initialTender);
        if (settleDialog.ShowDialog() == DialogResult.OK)
        {
            var sale = new Sale
            {
                TransactionNo = _currentTransactionNo,
                Date = DateTime.Now,
                Subtotal = subtotal,
                Discount = discount,
                Vat = vat,
                Total = grandTotal,
                CashTendered = settleDialog.TenderedAmount,
                ChangeDue = settleDialog.ChangeDue,
                PaymentMethod = settleDialog.PaymentMethod,
                Cashier = _currentCashier,
                CustomerName = settleDialog.CustomerName
            };

            var (success, message) = await _saleRepo.ProcessCheckoutAsync(sale, _currentCart);
            if (success)
            {
                var receiptGen = new ReceiptGenerator();
                string receiptText = receiptGen.GenerateTextReceipt(sale, _currentCart);
                using var receiptDialog = new ReceiptDialog(receiptText);
                receiptDialog.ShowDialog();

                await LoadProductsAsync();
                await StartNewTransactionAsync();
            }
            else
            {
                MessageBox.Show(message, "Checkout Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public void FocusBarcodeInput()
    {
        _txtBarcode.FocusInput();
    }

    public void FocusSearchInput()
    {
        _txtSearch.FocusInput();
    }
}
