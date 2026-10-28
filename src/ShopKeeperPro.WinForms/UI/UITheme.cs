using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public static class UITheme
{
    // Cohesive Modern Dark Palette
    public static readonly Color Background = Color.FromArgb(15, 23, 42);       // #0F172A Slate 900
    public static readonly Color HeaderBg = Color.FromArgb(11, 15, 25);        // #0B0F19 Darkest Slate
    public static readonly Color CardSurface = Color.FromArgb(30, 41, 59);     // #1E293B Slate 800
    public static readonly Color CardSurfaceHover = Color.FromArgb(51, 65, 85); // #334155 Slate 700
    public static readonly Color InputBackground = Color.FromArgb(15, 23, 42);  // #0F172A
    public static readonly Color BorderColor = Color.FromArgb(51, 65, 85);      // #334155 Slate 700
    public static readonly Color BorderLight = Color.FromArgb(71, 85, 105);     // #475569 Slate 600

    // Accents
    public static readonly Color Primary = Color.FromArgb(99, 102, 241);       // #6366F1 Indigo 500
    public static readonly Color PrimaryDark = Color.FromArgb(79, 70, 229);    // #4F46E5 Indigo 600
    public static readonly Color PrimaryLight = Color.FromArgb(129, 140, 248); // #818CF8 Indigo 400
    public static readonly Color Success = Color.FromArgb(16, 185, 129);       // #10B981 Emerald 500
    public static readonly Color SuccessDark = Color.FromArgb(5, 150, 105);    // #059669 Emerald 600
    public static readonly Color Warning = Color.FromArgb(245, 158, 11);       // #F59E0B Amber 500
    public static readonly Color Danger = Color.FromArgb(239, 68, 68);         // #EF4444 Rose 500
    public static readonly Color CyanAccent = Color.FromArgb(6, 182, 212);     // #06B6D4 Cyan 500

    // Typography Colors
    public static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);  // #F8FAFC
    public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);// #94A3B8
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);    // #64748B

    // Grid Row Alternating Colors
    public static readonly Color GridRowNormal = Color.FromArgb(30, 41, 59);
    public static readonly Color GridRowAlt = Color.FromArgb(24, 34, 50);
    public static readonly Color GridSelection = Color.FromArgb(79, 70, 229);

    // Standard High-DPI Scaleable Fonts
    public static Font FontGrandTotal => new("Segoe UI", 26f, FontStyle.Bold);
    public static Font FontTitle => new("Segoe UI", 16f, FontStyle.Bold);
    public static Font FontHeader => new("Segoe UI", 12f, FontStyle.Bold);
    public static Font FontBodyBold => new("Segoe UI", 10f, FontStyle.Bold);
    public static Font FontBody => new("Segoe UI", 10f, FontStyle.Regular);
    public static Font FontSmall => new("Segoe UI", 8.5f, FontStyle.Regular);
    public static Font FontSmallBold => new("Segoe UI", 8.5f, FontStyle.Bold);
    public static Font FontMonospace => new("Consolas", 10f, FontStyle.Regular);

    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        if (d <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void ApplyModernWindowStyle(Form form)
    {
        form.HandleCreated += (s, e) => ApplyDwmAttributes(form.Handle);
        if (form.IsHandleCreated)
        {
            ApplyDwmAttributes(form.Handle);
        }
    }

    private static void ApplyDwmAttributes(IntPtr handle)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            int useDarkMode = 1;
            // DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 11 build 22000+ is 20, Windows 10 1809-2004 was 19)
            DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int));
            DwmSetWindowAttribute(handle, 19, ref useDarkMode, sizeof(int));

            // DWMWA_WINDOW_CORNER_PREFERENCE: 2 = DWMWCP_ROUND (Windows 11)
            int cornerPref = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerPref, sizeof(int));
        }
    }

    public static Color GetParentBackColor(Control control)
    {
        Control? p = control.Parent;
        while (p != null)
        {
            if (p.BackColor != Color.Transparent && p.BackColor != Color.Empty && p.BackColor.A == 255)
            {
                return p.BackColor;
            }
            p = p.Parent;
        }
        return CardSurface;
    }
}
