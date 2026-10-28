Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$code = @'
using System;
using System.Drawing;
using System.Windows.Forms;
public class TestP : Panel {
    public bool PaintCalled = false;
    public bool BgCalled = false;
    public TestP(bool userPaint) {
        if (userPaint) {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        } else {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        }
    }
    protected override void OnPaint(PaintEventArgs e) { PaintCalled = true; base.OnPaint(e); }
    protected override void OnPaintBackground(PaintEventArgs e) { BgCalled = true; base.OnPaintBackground(e); }
}
'@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Windows.Forms,System.Drawing
$f = New-Object Windows.Forms.Form
$p1 = New-Object TestP($true)
$p2 = New-Object TestP($false)
$f.Controls.Add($p1)
$f.Controls.Add($p2)
$f.Show()
[Windows.Forms.Application]::DoEvents()
Write-Output "p1 (UserPaint=true): Paint=$($p1.PaintCalled), Bg=$($p1.BgCalled)"
Write-Output "p2 (UserPaint=false): Paint=$($p2.PaintCalled), Bg=$($p2.BgCalled)"
$f.Close()
