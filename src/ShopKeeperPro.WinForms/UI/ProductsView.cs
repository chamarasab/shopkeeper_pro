using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.WinForms.UI;

public class ProductsView : UserControl
{
    private readonly ProductRepository _repo = new();
    private readonly CategoryBrandVendorRepository _cbvRepo = new();

    private ModernTextBox _txtSearch = null!;
    private ComboBox _cboCategory = null!;
    private ModernDataGridView _gridProducts = null!;
    private List<Product> _products = new();
    private List<Category> _categories = new();
    private List<Brand> _brands = new();

    public ProductsView()
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
        var topBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };

        _txtSearch = new ModernTextBox
        {
            Location = new Point(4, 4),
            Size = new Size(320, 38),
            PlaceholderText = "Search by description, barcode or code..."
        };
        _txtSearch.InnerTextBox.TextChanged += async (_, _) => await RefreshListAsync();

        _cboCategory = new ComboBox
        {
            Location = new Point(334, 8),
            Size = new Size(180, 32),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = UITheme.InputBackground,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.FontBody
        };
        _cboCategory.SelectedIndexChanged += async (_, _) => await RefreshListAsync();

        var btnAdd = new ModernButton
        {
            Text = "+ ADD PRODUCT",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Location = new Point(530, 4),
            Size = new Size(130, 38)
        };
        btnAdd.Click += (_, _) => OpenProductDialog(null);

        var btnEdit = new ModernButton
        {
            Text = "EDIT",
            NormalColor = UITheme.Primary,
            HoverColor = UITheme.PrimaryDark,
            Location = new Point(668, 4),
            Size = new Size(90, 38)
        };
        btnEdit.Click += (_, _) => EditSelected();

        var btnDelete = new ModernButton
        {
            Text = "DELETE",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Location = new Point(766, 4),
            Size = new Size(90, 38)
        };
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        topBar.Controls.Add(_txtSearch);
        topBar.Controls.Add(_cboCategory);
        topBar.Controls.Add(btnAdd);
        topBar.Controls.Add(btnEdit);
        topBar.Controls.Add(btnDelete);

        // DataGridView
        _gridProducts = new ModernDataGridView { Dock = DockStyle.Fill };
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCode", HeaderText = "PCode", FillWeight = 12 });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBarcode", HeaderText = "Barcode", FillWeight = 18 });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDesc", HeaderText = "Description", FillWeight = 32 });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColBrand", HeaderText = "Brand", FillWeight = 15 });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColCategory", HeaderText = "Category", FillWeight = 15 });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColPrice", HeaderText = "Price", FillWeight = 14, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2", Font = UITheme.FontBodyBold, ForeColor = UITheme.CyanAccent } });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColQty", HeaderText = "Stock", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _gridProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColReorder", HeaderText = "Reorder", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });

        _gridProducts.CellDoubleClick += (_, _) => EditSelected();

        panel.Controls.Add(_gridProducts);
        panel.Controls.Add(topBar);
        Controls.Add(panel);
    }

    public async Task InitializeAsync()
    {
        _categories = await _cbvRepo.GetCategoriesAsync();
        _brands = await _cbvRepo.GetBrandsAsync();

        _cboCategory.Items.Clear();
        _cboCategory.Items.Add("All Categories");
        foreach (var c in _categories) _cboCategory.Items.Add(c);
        _cboCategory.SelectedIndex = 0;

        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        int? catId = null;
        if (_cboCategory.SelectedItem is Category cat) catId = cat.Id;

        _products = await _repo.SearchAsync(_txtSearch.Text, catId, activeOnly: true);
        _gridProducts.Rows.Clear();

        foreach (var p in _products)
        {
            _gridProducts.Rows.Add(
                p.PCode,
                p.Barcode,
                p.Description,
                p.BrandName,
                p.CategoryName,
                p.Price,
                p.Qty,
                p.Reorder
            );
        }
    }

    private void EditSelected()
    {
        if (_gridProducts.SelectedRows.Count == 0) return;
        int idx = _gridProducts.SelectedRows[0].Index;
        if (idx >= _products.Count) return;

        OpenProductDialog(_products[idx]);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_gridProducts.SelectedRows.Count == 0) return;
        int idx = _gridProducts.SelectedRows[0].Index;
        if (idx >= _products.Count) return;

        var p = _products[idx];
        var confirm = MessageBox.Show($"Are you sure you want to deactivate product '{p.Description}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            await _repo.DeleteAsync(p.PCode);
            await RefreshListAsync();
        }
    }

    private void OpenProductDialog(Product? product)
    {
        using var dlg = new ProductEditDialog(product, _categories, _brands);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _ = SaveProductAsync(dlg.Product, product == null);
        }
    }

    private async Task SaveProductAsync(Product product, bool isNew)
    {
        bool success = await _repo.SaveAsync(product, isNew);
        if (success)
        {
            await RefreshListAsync();
        }
        else
        {
            MessageBox.Show("Failed to save product. Please check values.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class ProductEditDialog : Form
{
    private readonly Product? _original;
    private readonly List<Category> _categories;
    private readonly List<Brand> _brands;

    private ModernTextBox _txtCode = null!;
    private ModernTextBox _txtBarcode = null!;
    private ModernTextBox _txtDesc = null!;
    private ComboBox _cboCategory = null!;
    private ComboBox _cboBrand = null!;
    private ModernTextBox _txtPrice = null!;
    private ModernTextBox _txtCost = null!;
    private ModernTextBox _txtQty = null!;
    private ModernTextBox _txtReorder = null!;

    public Product Product { get; private set; } = null!;

    public ProductEditDialog(Product? product, List<Category> categories, List<Brand> brands)
    {
        _original = product;
        _categories = categories;
        _brands = brands;

        Text = _original == null ? "Add New Product" : $"Edit Product - {_original.Description}";
        Size = new Size(520, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardSurface;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBody;

        UITheme.ApplyModernWindowStyle(this);

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface,
            Padding = new Padding(16)
        };

        int y = 14;
        _txtCode = AddField(panel, "Product Code (SKU):", ref y, _original?.PCode ?? $"P{DateTime.Now:fffss}");
        if (_original != null) _txtCode.InnerTextBox.ReadOnly = true;

        _txtBarcode = AddField(panel, "Barcode (Scan or Type):", ref y, _original?.Barcode ?? DateTime.Now.ToString("yyMMddHHmmss"));
        _txtDesc = AddField(panel, "Product Description / Name:", ref y, _original?.Description ?? "");

        var lblCat = new Label { Text = "Category:", Location = new Point(16, y), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _cboCategory = new ComboBox { Location = new Point(16, y + 20), Size = new Size(220, 32), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UITheme.InputBackground, ForeColor = UITheme.TextPrimary };
        foreach (var c in _categories) _cboCategory.Items.Add(c);
        if (_original?.CategoryId != null) _cboCategory.SelectedItem = _categories.FirstOrDefault(c => c.Id == _original.CategoryId);

        var lblBrand = new Label { Text = "Brand:", Location = new Point(250, y), AutoSize = true, ForeColor = UITheme.TextSecondary };
        _cboBrand = new ComboBox { Location = new Point(250, y + 20), Size = new Size(220, 32), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UITheme.InputBackground, ForeColor = UITheme.TextPrimary };
        foreach (var b in _brands) _cboBrand.Items.Add(b);
        if (_original?.BrandId != null) _cboBrand.SelectedItem = _brands.FirstOrDefault(b => b.Id == _original.BrandId);

        panel.Controls.Add(lblCat);
        panel.Controls.Add(_cboCategory);
        panel.Controls.Add(lblBrand);
        panel.Controls.Add(_cboBrand);
        y += 62;

        _txtPrice = AddField(panel, "Selling Retail Price (රු.):", ref y, _original?.Price.ToString("0.00") ?? "0.00");
        _txtCost = AddField(panel, "Cost / Wholesale Price (රු.):", ref y, _original?.CostPrice.ToString("0.00") ?? "0.00");
        _txtQty = AddField(panel, "Stock on Hand (Units):", ref y, _original?.Qty.ToString() ?? "0");
        _txtReorder = AddField(panel, "Reorder Alert Point:", ref y, _original?.Reorder.ToString() ?? "10");

        var btnSave = new ModernButton
        {
            Text = "[↵] SAVE PRODUCT",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Font = UITheme.FontBodyBold,
            Location = new Point(250, y + 10),
            Size = new Size(220, 42)
        };
        btnSave.Click += (_, _) => Save();

        var btnCancel = new ModernButton
        {
            Text = "[ESC] CANCEL",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Font = UITheme.FontBodyBold,
            Location = new Point(16, y + 10),
            Size = new Size(220, 42)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.Add(btnCancel);
        panel.Controls.Add(btnSave);
        Controls.Add(panel);
    }

    private ModernTextBox AddField(Panel parent, string label, ref int y, string value)
    {
        var lbl = new Label { Text = label, Location = new Point(16, y), AutoSize = true, ForeColor = UITheme.TextSecondary };
        var txt = new ModernTextBox { Location = new Point(16, y + 20), Size = new Size(454, 38), Text = value };
        parent.Controls.Add(lbl);
        parent.Controls.Add(txt);
        y += 62;
        return txt;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_txtCode.Text) || string.IsNullOrWhiteSpace(_txtBarcode.Text) || string.IsNullOrWhiteSpace(_txtDesc.Text))
        {
            MessageBox.Show("Product code, barcode, and description are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Product = new Product
        {
            PCode = _txtCode.Text.Trim(),
            Barcode = _txtBarcode.Text.Trim(),
            Description = _txtDesc.Text.Trim(),
            CategoryId = (_cboCategory.SelectedItem as Category)?.Id,
            BrandId = (_cboBrand.SelectedItem as Brand)?.Id,
            Price = decimal.TryParse(_txtPrice.Text, out decimal p) ? p : 0m,
            CostPrice = decimal.TryParse(_txtCost.Text, out decimal cp) ? cp : 0m,
            Qty = int.TryParse(_txtQty.Text, out int q) ? q : 0,
            Reorder = int.TryParse(_txtReorder.Text, out int r) ? r : 10,
            IsActive = true
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
