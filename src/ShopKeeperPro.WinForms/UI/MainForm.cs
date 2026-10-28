using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;
using ShopKeeperPro.Core.Services;

namespace ShopKeeperPro.WinForms.UI;

public class MainForm : Form
{
    private readonly BarcodeScannerService _scannerService;
    private readonly WinFormsSoundPlayer _soundPlayer = new();

    // Top Navigation Header
    private Panel _headerPanel = null!;
    private FlowLayoutPanel _navTabs = null!;
    private StatusBadge _badgeDbStatus = null!;
    private Label _lblClock = null!;
    private Label _lblUserBadge = null!;
    private System.Windows.Forms.Timer _clockTimer = null!;

    // Views
    private Panel _viewContainer = null!;
    private PosBillingView _posBillingView = null!;
    private DashboardView _dashboardView = null!;
    private ProductsView _productsView = null!;
    private StockInView _stockInView = null!;
    private SalesHistoryView _salesHistoryView = null!;
    private UserControl _activeView = null!;

    // Current State
    private User _currentUser = new() { Username = "chax", Name = "Chamara", Role = "Administrator" };
    private bool _isFullScreen = false;

    public MainForm()
    {
        _scannerService = new BarcodeScannerService(_soundPlayer);

        DoubleBuffered = true;
        Text = "ShopKeeper Pro - Modern Retail Point of Sale";
        BackColor = UITheme.Background;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBody;

        // Responsive minimum bounds (fits 1024x768 side-by-side, 1366x768, and 2K)
        MinimumSize = new Size(960, 580);
        Size = new Size(1366, 768);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        KeyPreview = true;

        UITheme.ApplyModernWindowStyle(this);

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // 1. Header Panel
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = UITheme.HeaderBg,
            Padding = new Padding(12, 6, 12, 6)
        };

        var headerTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var lblBrand = new Label
        {
            Text = "🛒 SHOPKEEPER PRO",
            Font = UITheme.FontTitle,
            ForeColor = UITheme.CyanAccent,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 10, 0)
        };

        _navTabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0, 4, 0, 0)
        };

        var btnTabPos = CreateNavTab("POS BILLING", true);
        var btnTabDash = CreateNavTab("DASHBOARD", false);
        var btnTabProd = CreateNavTab("PRODUCTS", false);
        var btnTabStock = CreateNavTab("STOCK IN", false);
        var btnTabSales = CreateNavTab("SALES HISTORY", false);

        _navTabs.Controls.Add(btnTabPos);
        _navTabs.Controls.Add(btnTabDash);
        _navTabs.Controls.Add(btnTabProd);
        _navTabs.Controls.Add(btnTabStock);
        _navTabs.Controls.Add(btnTabSales);

        // Right side indicators (Clock, Cashier, Database Status)
        var rightInfo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0)
        };

        var btnLogout = new ModernButton
        {
            Text = "SIGN OUT",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Size = new Size(92, 34),
            Font = UITheme.FontSmallBold,
            BorderRadius = 4,
            Margin = new Padding(4, 1, 0, 1)
        };
        btnLogout.Click += (_, _) => PromptSignOut();

        _lblClock = new Label
        {
            Text = DateTime.Now.ToString("ddd, MMM dd - HH:mm:ss"),
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Margin = new Padding(6, 8, 6, 2)
        };

        _lblUserBadge = new Label
        {
            Text = $"👤 {_currentUser.Name} ({_currentUser.Role})",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.PrimaryLight,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true,
            Margin = new Padding(6, 8, 6, 2)
        };

        _badgeDbStatus = new StatusBadge
        {
            Text = "MSSQL: CHECKING",
            BadgeColor = UITheme.Warning,
            Size = new Size(130, 26),
            Margin = new Padding(4, 5, 4, 2)
        };

        rightInfo.Controls.Add(btnLogout);
        rightInfo.Controls.Add(_lblClock);
        rightInfo.Controls.Add(_lblUserBadge);
        rightInfo.Controls.Add(_badgeDbStatus);

        headerTable.Controls.Add(lblBrand, 0, 0);
        headerTable.Controls.Add(_navTabs, 1, 0);
        headerTable.Controls.Add(rightInfo, 2, 0);
        _headerPanel.Controls.Add(headerTable);

        // 2. Main View Container
        _viewContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.Background
        };

        // Initialize Views
        _posBillingView = new PosBillingView(_scannerService) { CurrentCashier = _currentUser.Name };
        _dashboardView = new DashboardView();
        _productsView = new ProductsView();
        _stockInView = new StockInView { CurrentUser = _currentUser.Name };
        _salesHistoryView = new SalesHistoryView();

        _activeView = _posBillingView;
        _viewContainer.Controls.Add(_posBillingView);

        // Hook Tab Events
        btnTabPos.Click += (_, _) => SwitchView(_posBillingView, btnTabPos);
        btnTabDash.Click += async (_, _) =>
        {
            SwitchView(_dashboardView, btnTabDash);
            await _dashboardView.RefreshDashboardAsync();
        };
        btnTabProd.Click += async (_, _) =>
        {
            SwitchView(_productsView, btnTabProd);
            await _productsView.InitializeAsync();
        };
        btnTabStock.Click += async (_, _) =>
        {
            SwitchView(_stockInView, btnTabStock);
            await _stockInView.InitializeAsync();
        };
        btnTabSales.Click += async (_, _) =>
        {
            SwitchView(_salesHistoryView, btnTabSales);
            await _salesHistoryView.InitializeAsync();
        };

        Controls.Add(_viewContainer);
        Controls.Add(_headerPanel);

        // Clock Timer
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) => _lblClock.Text = DateTime.Now.ToString("ddd, MMM dd - HH:mm:ss");
        _clockTimer.Start();

        KeyDown += MainForm_KeyDown;
        Shown += async (_, _) => await OnAppShownAsync();
    }

    private ModernButton CreateNavTab(string text, bool isActive)
    {
        return new ModernButton
        {
            Text = text,
            Size = new Size(100, 36),
            Margin = new Padding(2, 0, 2, 0),
            Font = UITheme.FontSmallBold,
            BorderRadius = 6,
            NormalColor = isActive ? UITheme.Primary : UITheme.CardSurfaceHover,
            HoverColor = UITheme.PrimaryDark
        };
    }

    private void SwitchView(UserControl view, ModernButton activeTab)
    {
        if (_activeView == view) return;

        foreach (Control c in _navTabs.Controls)
        {
            if (c is ModernButton mb)
                mb.NormalColor = UITheme.CardSurfaceHover;
        }
        activeTab.NormalColor = UITheme.Primary;

        _viewContainer.SuspendLayout();
        _viewContainer.Controls.Clear();
        _activeView = view;
        _viewContainer.Controls.Add(view);
        _viewContainer.ResumeLayout();

        if (view is PosBillingView pbv)
        {
            pbv.FocusBarcodeInput();
        }
    }

    private async Task OnAppShownAsync()
    {
        try
        {
            Activate();
            BringToFront();
        }
        catch { }

        // Check MSSQL Connection
        var (ok, msg, ms) = await DbConfig.TestConnectionAsync();
        if (ok)
        {
            _badgeDbStatus.Text = "MSSQL: CONNECTED";
            _badgeDbStatus.BadgeColor = UITheme.Success;
        }
        else
        {
            _badgeDbStatus.Text = "MSSQL: OFFLINE";
            _badgeDbStatus.BadgeColor = UITheme.Danger;
            MessageBox.Show($"Could not connect to MSSQL database 'shopkeeper_db':\n{msg}\n\nPlease verify local SQL Server is running.", "Database Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        await _posBillingView.InitializeAsync();
        try
        {
            var logs = VerifyInteraction();
            File.WriteAllText("live-layout.log", string.Join(Environment.NewLine, logs));
        }
        catch { }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // Global POS Function Keys
        switch (e.KeyCode)
        {
            case Keys.F1:
                e.Handled = true;
                _ = _posBillingView.StartNewTransactionAsync();
                break;
            case Keys.F5:
                e.Handled = true;
                _ = _posBillingView.OpenSettleDialogAsync();
                break;
            case Keys.F7:
                e.Handled = true;
                _posBillingView.FocusSearchInput();
                break;
            case Keys.F11:
                e.Handled = true;
                ToggleFullScreen();
                break;
        }
    }

    private void ToggleFullScreen()
    {
        _isFullScreen = !_isFullScreen;
        if (_isFullScreen)
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            Size = new Size(1366, 768);
        }
    }

    private void PromptSignOut()
    {
        using var login = new LoginDialog();
        if (login.ShowDialog() == DialogResult.OK && login.AuthenticatedUser != null)
        {
            _currentUser = login.AuthenticatedUser;
            _lblUserBadge.Text = $"👤 {_currentUser.Name} ({_currentUser.Role})";
            _posBillingView.CurrentCashier = _currentUser.Name;
            _stockInView.CurrentUser = _currentUser.Name;
        }
    }

    public List<string> VerifyInteraction()
    {
        // Automated verification for headless/self-test validation
        if (_posBillingView == null)
            throw new InvalidOperationException("PosBillingView was not initialized.");

        CreateControl();
        PerformLayout();

        var logs = new List<string>
        {
            $"MainForm: Size={Size}, ClientSize={ClientSize}",
            $"_headerPanel: Bounds={_headerPanel.Bounds}, Visible={_headerPanel.Visible}",
            $"_viewContainer: Bounds={_viewContainer.Bounds}, Visible={_viewContainer.Visible}",
            $"_posBillingView: Bounds={_posBillingView.Bounds}, Visible={_posBillingView.Visible}"
        };

        void Dump(Control parent, string indent)
        {
            foreach (Control c in parent.Controls)
            {
                logs.Add($"{indent}{c.GetType().Name} ({c.Name}): Bounds={c.Bounds}, Visible={c.Visible}, Dock={c.Dock}");
                if (c.HasChildren) Dump(c, indent + "  ");
            }
        }
        Dump(_posBillingView, "  ");
        return logs;
    }
}
