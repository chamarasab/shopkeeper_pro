using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class ModernTextBox : UserControl
{
    private readonly TextBox _textBox;
    private int _borderRadius = 8;
    private Color _borderColor = UITheme.BorderColor;
    private Color _focusBorderColor = UITheme.Primary;
    private bool _isFocused = false;
    private string _placeholderText = string.Empty;

    public string PlaceholderText
    {
        get => _placeholderText;
        set { _placeholderText = value; Invalidate(); }
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? string.Empty;
    }

    public TextBox InnerTextBox => _textBox;

    public bool PasswordChar
    {
        get => _textBox.UseSystemPasswordChar;
        set => _textBox.UseSystemPasswordChar = value;
    }

    public int MaxLength
    {
        get => _textBox.MaxLength;
        set => _textBox.MaxLength = value;
    }

    public ModernTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = UITheme.InputBackground;
        Padding = new Padding(12, 10, 12, 10);
        Size = new Size(250, 42);

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = UITheme.InputBackground,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.FontBody,
            Dock = DockStyle.Fill
        };

        _textBox.Enter += (_, _) => { _isFocused = true; Invalidate(); };
        _textBox.Leave += (_, _) => { _isFocused = false; Invalidate(); };
        _textBox.TextChanged += (_, _) => Invalidate();

        Controls.Add(_textBox);
    }

    public void FocusInput()
    {
        _textBox.Focus();
        _textBox.SelectAll();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Paint parent background color onto all 4 corners to eliminate black sharp edges
        Color parentBg = UITheme.GetParentBackColor(this);
        using (var parentBrush = new SolidBrush(parentBg))
        {
            e.Graphics.FillRectangle(parentBrush, ClientRectangle);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var path = UITheme.CreateRoundedRectangle(rect, _borderRadius);
        using var bgBrush = new SolidBrush(BackColor);
        e.Graphics.FillPath(bgBrush, path);

        Color border = _isFocused ? _focusBorderColor : _borderColor;
        int thickness = _isFocused ? 2 : 1;
        using var pen = new Pen(border, thickness);
        e.Graphics.DrawPath(pen, path);

        // Draw Placeholder if text is empty
        if (string.IsNullOrEmpty(_textBox.Text) && !string.IsNullOrEmpty(_placeholderText) && !_isFocused)
        {
            var textRect = new Rectangle(12, 10, Width - 24, Height - 20);
            TextRenderer.DrawText(
                e.Graphics,
                _placeholderText,
                Font,
                textRect,
                UITheme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            );
        }
    }
}

public class StatusBadge : Label
{
    private Color _badgeColor = UITheme.Success;
    private int _borderRadius = 6;

    public Color BadgeColor
    {
        get => _badgeColor;
        set { _badgeColor = value; Invalidate(); }
    }

    public StatusBadge()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        AutoSize = false;
        Size = new Size(90, 24);
        Font = UITheme.FontSmallBold;
        ForeColor = Color.White;
        TextAlign = ContentAlignment.MiddleCenter;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Paint parent background color onto all 4 corners to eliminate black sharp edges
        Color parentBg = UITheme.GetParentBackColor(this);
        using (var parentBrush = new SolidBrush(parentBg))
        {
            e.Graphics.FillRectangle(parentBrush, ClientRectangle);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var path = UITheme.CreateRoundedRectangle(rect, _borderRadius);
        using var brush = new SolidBrush(_badgeColor);
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
        );
    }
}
