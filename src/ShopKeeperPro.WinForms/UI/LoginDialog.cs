using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.WinForms.UI;

public class LoginDialog : Form
{
    private readonly UserRepository _userRepo = new();
    private readonly WinFormsSoundPlayer _soundPlayer = new();

    private ModernTextBox _txtUsername = null!;
    private ModernTextBox _txtPassword = null!;
    private ModernButton _btnEye = null!;
    private StatusBadge _badgeDb = null!;
    private RoundedPanel _panelAlert = null!;
    private Label _lblAlert = null!;
    private ModernButton _btnLogin = null!;
    private ModernButton _btnExit = null!;
    private FlowLayoutPanel _pillsPanel = null!;

    public User? AuthenticatedUser { get; private set; }

    public LoginDialog()
    {
        Text = "ShopKeeper Pro — Register Authentication";
        Size = new Size(500, 600);
        StartPosition = FormStartPosition.CenterScreen;
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
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface,
            Padding = new Padding(24, 20, 24, 20)
        };

        // 1. Header Branding & Database Status
        var lblLogo = new Label
        {
            Text = "🛒 SHOPKEEPER PRO",
            Font = UITheme.FontTitle,
            ForeColor = UITheme.CyanAccent,
            Location = new Point(24, 20),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Next-Gen Retail Point of Sale & Inventory System",
            Font = UITheme.FontSmall,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(26, 48),
            AutoSize = true
        };

        _badgeDb = new StatusBadge
        {
            Text = "MSSQL: CONNECTING...",
            BadgeColor = UITheme.Warning,
            Location = new Point(26, 74),
            Size = new Size(180, 24)
        };

        // 2. Quick Shift / Cashier Operator Selector
        var lblQuick = new Label
        {
            Text = "SELECT CASHIER / REGISTER OPERATOR:",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.PrimaryLight,
            Location = new Point(24, 114),
            AutoSize = true
        };

        _pillsPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 136),
            Size = new Size(440, 42),
            BackColor = Color.Transparent,
            WrapContents = false
        };

        var pillChamara = CreateOperatorPill("Chamara (Admin)", "chax", "123", true);
        var pillAlex = CreateOperatorPill("Alex (Cashier)", "cashier1", "1234", false);
        var pillAdmin = CreateOperatorPill("System Admin", "admin", "admin123", false);

        _pillsPanel.Controls.Add(pillChamara);
        _pillsPanel.Controls.Add(pillAlex);
        _pillsPanel.Controls.Add(pillAdmin);

        // 3. Username Field
        var lblUser = new Label
        {
            Text = "USERNAME / CASHIER ID:",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(24, 190),
            AutoSize = true
        };

        _txtUsername = new ModernTextBox
        {
            Location = new Point(24, 212),
            Size = new Size(440, 42),
            Text = "chax",
            PlaceholderText = "Enter username or cashier ID..."
        };

        // 4. Password Field + Show/Hide Toggle
        var lblPass = new Label
        {
            Text = "PASSWORD:",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(24, 266),
            AutoSize = true
        };

        _txtPassword = new ModernTextBox
        {
            Location = new Point(24, 288),
            Size = new Size(385, 42),
            Text = "123",
            PasswordChar = true,
            PlaceholderText = "Enter password..."
        };

        _btnEye = new ModernButton
        {
            Text = "👁",
            Location = new Point(415, 288),
            Size = new Size(48, 42),
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Primary,
            Font = new Font("Segoe UI", 12f, FontStyle.Regular),
            BorderRadius = 8
        };
        _btnEye.Click += (_, _) =>
        {
            _txtPassword.PasswordChar = !_txtPassword.PasswordChar;
            _btnEye.Text = _txtPassword.PasswordChar ? "👁" : "🔒";
            _txtPassword.FocusInput();
        };

        // 5. Inline Alert Banner (Errors / Info)
        _panelAlert = new RoundedPanel
        {
            Location = new Point(24, 342),
            Size = new Size(440, 38),
            BorderRadius = 8,
            BorderThickness = 1,
            BackColor = Color.FromArgb(69, 26, 26),
            BorderColor = UITheme.Danger,
            Visible = false
        };

        _lblAlert = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.Danger,
            Text = "⚠️ Invalid username or password."
        };
        _panelAlert.Controls.Add(_lblAlert);

        // 6. Action Buttons
        _btnLogin = new ModernButton
        {
            Text = "[↵] SIGN IN TO REGISTER",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Font = UITheme.FontHeader,
            Location = new Point(24, 396),
            Size = new Size(440, 48),
            BorderRadius = 8
        };
        _btnLogin.Click += async (_, _) => await AttemptLoginAsync();

        _btnExit = new ModernButton
        {
            Text = "[ESC] EXIT APPLICATION",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Font = UITheme.FontBodyBold,
            Location = new Point(24, 452),
            Size = new Size(440, 40),
            BorderRadius = 8
        };
        _btnExit.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        // 7. Footer Information
        var lblFooter = new Label
        {
            Text = "ShopKeeper Pro v1.0 • Connected to MSSQL shopkeeper_db",
            Font = UITheme.FontSmall,
            ForeColor = UITheme.TextMuted,
            Location = new Point(24, 508),
            Size = new Size(440, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        mainPanel.Controls.Add(lblLogo);
        mainPanel.Controls.Add(lblSub);
        mainPanel.Controls.Add(_badgeDb);
        mainPanel.Controls.Add(lblQuick);
        mainPanel.Controls.Add(_pillsPanel);
        mainPanel.Controls.Add(lblUser);
        mainPanel.Controls.Add(_txtUsername);
        mainPanel.Controls.Add(lblPass);
        mainPanel.Controls.Add(_txtPassword);
        mainPanel.Controls.Add(_btnEye);
        mainPanel.Controls.Add(_panelAlert);
        mainPanel.Controls.Add(_btnLogin);
        mainPanel.Controls.Add(_btnExit);
        mainPanel.Controls.Add(lblFooter);

        Controls.Add(mainPanel);

        AcceptButton = _btnLogin;
        CancelButton = _btnExit;

        Shown += async (_, _) =>
        {
            _txtPassword.FocusInput();
            await CheckDatabaseStatusAsync();
        };
    }

    private ModernButton CreateOperatorPill(string label, string username, string defaultPassword, bool isDefault)
    {
        var btn = new ModernButton
        {
            Text = label,
            AutoSize = true,
            Height = 32,
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(0, 0, 6, 0),
            Font = UITheme.FontSmallBold,
            BorderRadius = 6,
            NormalColor = isDefault ? UITheme.Primary : UITheme.CardSurfaceHover,
            HoverColor = UITheme.PrimaryDark
        };

        btn.Click += (_, _) =>
        {
            foreach (Control c in _pillsPanel.Controls)
            {
                if (c is ModernButton mb) mb.NormalColor = UITheme.CardSurfaceHover;
            }
            btn.NormalColor = UITheme.Primary;

            _txtUsername.Text = username;
            _txtPassword.Text = defaultPassword;
            _panelAlert.Visible = false;
            _txtPassword.FocusInput();
        };

        return btn;
    }

    private async Task CheckDatabaseStatusAsync()
    {
        try
        {
            var (ok, msg, ms) = await DbConfig.TestConnectionAsync();
            if (ok)
            {
                _badgeDb.Text = $"MSSQL: ONLINE ({ms}ms)";
                _badgeDb.BadgeColor = UITheme.Success;
            }
            else
            {
                _badgeDb.Text = "MSSQL: OFFLINE";
                _badgeDb.BadgeColor = UITheme.Danger;
                ShowAlert($"Database offline: {msg}", true);
            }
        }
        catch
        {
            _badgeDb.Text = "MSSQL: OFFLINE";
            _badgeDb.BadgeColor = UITheme.Danger;
        }
    }

    private void ShowAlert(string message, bool isError)
    {
        _lblAlert.Text = message;
        if (isError)
        {
            _panelAlert.BackColor = Color.FromArgb(69, 26, 26);
            _panelAlert.BorderColor = UITheme.Danger;
            _lblAlert.ForeColor = UITheme.Danger;
            _soundPlayer.PlayAlert();
        }
        else
        {
            _panelAlert.BackColor = Color.FromArgb(20, 45, 35);
            _panelAlert.BorderColor = UITheme.Success;
            _lblAlert.ForeColor = UITheme.Success;
        }
        _panelAlert.Visible = true;
    }

    private async Task AttemptLoginAsync()
    {
        string username = _txtUsername.Text.Trim();
        string password = _txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowAlert("⚠️ Please enter both username and password.", true);
            _txtUsername.FocusInput();
            return;
        }

        _btnLogin.Enabled = false;
        _btnLogin.Text = "AUTHENTICATING...";
        ShowAlert("⏳ Verifying credentials with SQL database...", false);

        try
        {
            var user = await _userRepo.AuthenticateAsync(username, password);
            if (user != null)
            {
                _soundPlayer.PlaySuccess();
                AuthenticatedUser = user;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowAlert("⚠️ Invalid username or password. Please try again.", true);
                _txtPassword.FocusInput();
            }
        }
        catch (Exception ex)
        {
            ShowAlert($"⚠️ Authentication error: {ex.Message}", true);
        }
        finally
        {
            _btnLogin.Enabled = true;
            _btnLogin.Text = "[↵] SIGN IN TO REGISTER";
        }
    }
}
