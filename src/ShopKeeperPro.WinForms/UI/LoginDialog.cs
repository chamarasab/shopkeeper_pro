using System.Drawing;
using System.Windows.Forms;
using ShopKeeperPro.Core.Data;
using ShopKeeperPro.Core.Models;

namespace ShopKeeperPro.WinForms.UI;

public class LoginDialog : Form
{
    private readonly UserRepository _userRepo = new();
    private ModernTextBox _txtUsername = null!;
    private ModernTextBox _txtPassword = null!;

    public User? AuthenticatedUser { get; private set; }

    public LoginDialog()
    {
        Text = "ShopKeeper Pro - Sign In";
        Size = new Size(400, 360);
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
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface,
            Padding = new Padding(16)
        };

        var lblLogo = new Label
        {
            Text = "SHOPKEEPER PRO",
            Font = UITheme.FontTitle,
            ForeColor = UITheme.CyanAccent,
            Location = new Point(16, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Point of Sale & Retail Management",
            Font = UITheme.FontSmall,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(18, 44),
            AutoSize = true
        };

        var lblUser = new Label
        {
            Text = "Username:",
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(16, 80),
            AutoSize = true
        };

        _txtUsername = new ModernTextBox
        {
            Location = new Point(16, 102),
            Size = new Size(336, 38),
            Text = "chax",
            PlaceholderText = "Enter username (e.g. chax, admin, max)..."
        };

        var lblPass = new Label
        {
            Text = "Password:",
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(16, 148),
            AutoSize = true
        };

        _txtPassword = new ModernTextBox
        {
            Location = new Point(16, 170),
            Size = new Size(336, 38),
            Text = "123",
            PasswordChar = true,
            PlaceholderText = "Enter password..."
        };

        var btnLogin = new ModernButton
        {
            Text = "[↵] SIGN IN",
            NormalColor = UITheme.Primary,
            HoverColor = UITheme.PrimaryDark,
            Font = UITheme.FontHeader,
            Location = new Point(16, 226),
            Size = new Size(336, 44)
        };
        btnLogin.Click += async (_, _) => await AttemptLoginAsync();

        var btnCancel = new ModernButton
        {
            Text = "[ESC] EXIT",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Font = UITheme.FontBodyBold,
            Location = new Point(16, 276),
            Size = new Size(336, 34)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.Add(lblLogo);
        panel.Controls.Add(lblSub);
        panel.Controls.Add(lblUser);
        panel.Controls.Add(_txtUsername);
        panel.Controls.Add(lblPass);
        panel.Controls.Add(_txtPassword);
        panel.Controls.Add(btnLogin);
        panel.Controls.Add(btnCancel);

        Controls.Add(panel);
        AcceptButton = btnLogin;
        CancelButton = btnCancel;

        Shown += (_, _) => _txtPassword.FocusInput();
    }

    private async Task AttemptLoginAsync()
    {
        string username = _txtUsername.Text.Trim();
        string password = _txtPassword.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Please enter username and password.", "Required Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = await _userRepo.AuthenticateAsync(username, password);
        if (user != null)
        {
            AuthenticatedUser = user;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Invalid username or password. Default users: chax (pwd: 123) or admin (pwd: admin123)", "Sign In Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPassword.FocusInput();
        }
    }
}
