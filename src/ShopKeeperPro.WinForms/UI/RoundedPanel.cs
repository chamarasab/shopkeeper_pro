using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class RoundedPanel : Panel
{
    private int _borderRadius = 10;
    private Color _borderColor = UITheme.BorderColor;
    private int _borderThickness = 1;

    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    public int BorderThickness
    {
        get => _borderThickness;
        set { _borderThickness = value; Invalidate(); }
    }

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = UITheme.CardSurface;
        Padding = new Padding(12);
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
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);

        if (_borderThickness > 0 && _borderColor != Color.Transparent)
        {
            using var pen = new Pen(_borderColor, _borderThickness);
            e.Graphics.DrawPath(pen, path);
        }

        base.OnPaint(e);
    }
}

public class ModernButton : Button
{
    private int _borderRadius = 8;
    private Color _normalColor = UITheme.Primary;
    private Color _hoverColor = UITheme.PrimaryDark;
    private Color _pressedColor = Color.FromArgb(67, 56, 202);
    private bool _isHovered = false;
    private bool _isPressed = false;

    public int BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Invalidate(); }
    }

    public Color NormalColor
    {
        get => _normalColor;
        set { _normalColor = value; BackColor = value; Invalidate(); }
    }

    public Color HoverColor
    {
        get => _hoverColor;
        set { _hoverColor = value; Invalidate(); }
    }

    public Color PressedColor
    {
        get => _pressedColor;
        set { _pressedColor = value; Invalidate(); }
    }

    public ModernButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = _normalColor;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBodyBold;
        Cursor = Cursors.Hand;
        Size = new Size(130, 42);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        // Paint parent background color onto all 4 corners to eliminate black sharp edges
        Color parentBg = UITheme.GetParentBackColor(this);
        using (var parentBrush = new SolidBrush(parentBg))
        {
            pevent.Graphics.FillRectangle(parentBrush, ClientRectangle);
        }

        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        Color currentColor = _isPressed ? _pressedColor : (_isHovered ? _hoverColor : _normalColor);
        if (!Enabled) currentColor = Color.FromArgb(51, 65, 85);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var path = UITheme.CreateRoundedRectangle(rect, _borderRadius);
        using var brush = new SolidBrush(currentColor);
        pevent.Graphics.FillPath(brush, path);

        Color textColor = Enabled ? ForeColor : Color.FromArgb(148, 163, 184);
        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            rect,
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
        );
    }
}
