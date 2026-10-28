using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class ReceiptDialog : Form
{
    private readonly string _receiptText;
    private readonly RichTextBox _rtb;

    public ReceiptDialog(string receiptText)
    {
        _receiptText = receiptText;

        Text = "Thermal Receipt Preview";
        Size = new Size(420, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = UITheme.CardSurface;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBody;

        UITheme.ApplyModernWindowStyle(this);

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface,
            Padding = new Padding(12)
        };

        _rtb = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 34, 50),
            ForeColor = Color.FromArgb(241, 245, 249),
            Font = new Font("Consolas", 9.5f, FontStyle.Regular),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Text = _receiptText
        };

        var bottomBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };

        var btnClose = new ModernButton
        {
            Text = "CLOSE",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Primary,
            Size = new Size(100, 38)
        };
        btnClose.Click += (_, _) => Close();

        var btnPrint = new ModernButton
        {
            Text = "PRINT RECEIPT",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Size = new Size(130, 38)
        };
        btnPrint.Click += (_, _) => PrintReceipt();

        var btnCopy = new ModernButton
        {
            Text = "COPY",
            NormalColor = UITheme.PrimaryDark,
            HoverColor = UITheme.Primary,
            Size = new Size(80, 38)
        };
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(_receiptText);
            MessageBox.Show("Receipt copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        bottomBar.Controls.Add(btnClose);
        bottomBar.Controls.Add(btnPrint);
        bottomBar.Controls.Add(btnCopy);

        panel.Controls.Add(_rtb);
        panel.Controls.Add(bottomBar);
        Controls.Add(panel);
    }

    private void PrintReceipt()
    {
        try
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) =>
            {
                using var font = new Font("Consolas", 8.5f);
                ev.Graphics?.DrawString(_receiptText, font, Brushes.Black, new PointF(10, 10));
            };

            using var printDialog = new PrintDialog { Document = printDoc };
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Printer error: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
