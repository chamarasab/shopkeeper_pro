using System.Drawing;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class ModernDataGridView : DataGridView
{
    public ModernDataGridView()
    {
        DoubleBuffered = true;
        BackgroundColor = UITheme.CardSurface;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        GridColor = UITheme.BorderColor;

        EnableHeadersVisualStyles = false;
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UITheme.HeaderBg,
            ForeColor = UITheme.TextSecondary,
            Font = UITheme.FontBodyBold,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 6, 8, 6)
        };
        ColumnHeadersHeight = 40;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UITheme.GridRowNormal,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.FontBody,
            SelectionBackColor = UITheme.GridSelection,
            SelectionForeColor = Color.White,
            Padding = new Padding(6, 4, 6, 4),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = UITheme.GridRowAlt,
            ForeColor = UITheme.TextPrimary,
            Font = UITheme.FontBody,
            SelectionBackColor = UITheme.GridSelection,
            SelectionForeColor = Color.White,
            Padding = new Padding(6, 4, 6, 4)
        };

        RowTemplate.Height = 38;
        RowHeadersVisible = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        ReadOnly = true;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }
}
