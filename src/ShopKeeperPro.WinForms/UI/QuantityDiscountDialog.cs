using System.Drawing;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class QuantityDiscountDialog : Form
{
    private ModernTextBox _txtInput = null!;
    public string Value => _txtInput.Text.Trim();

    public QuantityDiscountDialog(string title, string prompt, string initialValue = "1")
    {
        Text = title;
        Size = new Size(360, 320);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardSurface;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBody;

        UITheme.ApplyModernWindowStyle(this);

        InitializeComponents(title, prompt, initialValue);
    }

    private void InitializeComponents(string title, string prompt, string initialValue)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface
        };

        var lblPrompt = new Label
        {
            Text = prompt,
            Font = UITheme.FontBodyBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(16, 16),
            AutoSize = true
        };

        _txtInput = new ModernTextBox
        {
            Location = new Point(16, 42),
            Size = new Size(300, 42),
            Text = initialValue
        };
        _txtInput.InnerTextBox.Font = UITheme.FontTitle;
        _txtInput.InnerTextBox.TextAlign = HorizontalAlignment.Center;

        // Quick mini numpad
        var numpad = new TableLayoutPanel
        {
            Location = new Point(16, 94),
            Size = new Size(300, 110),
            ColumnCount = 4,
            RowCount = 3,
            BackColor = Color.Transparent
        };

        for (int c = 0; c < 4; c++) numpad.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        for (int r = 0; r < 3; r++) numpad.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        string[,] keys = {
            { "1", "2", "3", "5" },
            { "4", "5", "6", "10" },
            { "7", "8", "9", "CLR" }
        };

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                string key = keys[row, col];
                var btn = new ModernButton
                {
                    Text = key,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    BorderRadius = 4,
                    Font = UITheme.FontBodyBold,
                    NormalColor = key == "CLR" ? UITheme.Danger : (key is "5" or "10" ? UITheme.PrimaryDark : UITheme.CardSurfaceHover),
                    HoverColor = UITheme.Primary
                };
                btn.Click += (_, _) =>
                {
                    if (key == "CLR") _txtInput.Text = string.Empty;
                    else if (key is "5" or "10") _txtInput.Text = key;
                    else _txtInput.Text += key;
                    _txtInput.FocusInput();
                };
                numpad.Controls.Add(btn, col, row);
            }
        }

        var btnOk = new ModernButton
        {
            Text = "[↵] OK",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Font = UITheme.FontBodyBold,
            Location = new Point(170, 216),
            Size = new Size(146, 40)
        };
        btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new ModernButton
        {
            Text = "[ESC] CANCEL",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Font = UITheme.FontBodyBold,
            Location = new Point(16, 216),
            Size = new Size(146, 40)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        panel.Controls.Add(lblPrompt);
        panel.Controls.Add(_txtInput);
        panel.Controls.Add(numpad);
        panel.Controls.Add(btnCancel);
        panel.Controls.Add(btnOk);

        Controls.Add(panel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Shown += (_, _) => _txtInput.FocusInput();
    }
}
