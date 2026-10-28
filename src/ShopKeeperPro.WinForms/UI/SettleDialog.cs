using System.Drawing;
using System.Windows.Forms;

namespace ShopKeeperPro.WinForms.UI;

public class SettleDialog : Form
{
    private readonly decimal _totalPayable;
    private readonly string _transactionNo;
    private ModernTextBox _txtTender = null!;
    private Label _lblChangeVal = null!;
    private ModernTextBox _txtCustomer = null!;
    private string _paymentMethod = "Cash";

    public decimal TenderedAmount { get; private set; }
    public decimal ChangeDue { get; private set; }
    public string PaymentMethod => _paymentMethod;
    public string? CustomerName => string.IsNullOrWhiteSpace(_txtCustomer.Text) ? null : _txtCustomer.Text.Trim();

    public SettleDialog(decimal totalPayable, string transactionNo, decimal? initialTender = null)
    {
        _totalPayable = totalPayable;
        _transactionNo = transactionNo;

        Text = "Payment Settlement & Checkout";
        Size = new Size(520, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UITheme.CardSurface;
        ForeColor = UITheme.TextPrimary;
        Font = UITheme.FontBody;

        UITheme.ApplyModernWindowStyle(this);

        InitializeComponents(initialTender);
    }

    private void InitializeComponents(decimal? initialTender)
    {
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UITheme.CardSurface
        };

        var lblTr = new Label
        {
            Text = $"TRANSACTION: {_transactionNo}",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 16),
            AutoSize = true
        };

        var lblPayableTitle = new Label
        {
            Text = "TOTAL PAYABLE",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.PrimaryLight,
            Location = new Point(20, 42),
            AutoSize = true
        };

        var lblPayableVal = new Label
        {
            Text = $"රු.{_totalPayable:N2}",
            Font = UITheme.FontGrandTotal,
            ForeColor = UITheme.CyanAccent,
            Location = new Point(18, 62),
            AutoSize = true
        };

        // Payment Method Selector Tabs
        var lblMethod = new Label
        {
            Text = "PAYMENT METHOD",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 120),
            AutoSize = true
        };

        var methodPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 142),
            Size = new Size(460, 42),
            BackColor = Color.Transparent
        };

        var btnCash = CreateMethodButton("Cash", true);
        var btnCard = CreateMethodButton("Credit / Debit Card", false);
        var btnQr = CreateMethodButton("QR / Transfer", false);

        void SelectMethod(string method, ModernButton activeBtn)
        {
            _paymentMethod = method;
            foreach (Control c in methodPanel.Controls)
            {
                if (c is ModernButton mb)
                    mb.NormalColor = UITheme.CardSurfaceHover;
            }
            activeBtn.NormalColor = UITheme.Primary;
        }

        btnCash.Click += (_, _) => SelectMethod("Cash", btnCash);
        btnCard.Click += (_, _) =>
        {
            SelectMethod("Card", btnCard);
            _txtTender.Text = _totalPayable.ToString("0.00");
        };
        btnQr.Click += (_, _) =>
        {
            SelectMethod("QR / Transfer", btnQr);
            _txtTender.Text = _totalPayable.ToString("0.00");
        };

        methodPanel.Controls.Add(btnCash);
        methodPanel.Controls.Add(btnCard);
        methodPanel.Controls.Add(btnQr);

        // Tendered Input
        var lblTenderTitle = new Label
        {
            Text = "AMOUNT TENDERED (රු.)",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.PrimaryLight,
            Location = new Point(20, 194),
            AutoSize = true
        };

        _txtTender = new ModernTextBox
        {
            Location = new Point(20, 216),
            Size = new Size(460, 44),
            PlaceholderText = "Enter amount received from customer..."
        };
        _txtTender.InnerTextBox.Font = UITheme.FontHeader;
        _txtTender.InnerTextBox.TextChanged += (_, _) => RecalculateChange();

        // Quick Tender Preset Buttons
        var presetPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 268),
            Size = new Size(460, 36),
            BackColor = Color.Transparent
        };

        presetPanel.Controls.Add(CreatePresetButton("Exact Cash", _totalPayable));
        presetPanel.Controls.Add(CreatePresetButton("+රු.100", 100));
        presetPanel.Controls.Add(CreatePresetButton("+රු.500", 500));
        presetPanel.Controls.Add(CreatePresetButton("+රු.1,000", 1000));
        presetPanel.Controls.Add(CreatePresetButton("+රු.5,000", 5000));

        // Change Due Display Card
        var changeCard = new RoundedPanel
        {
            Location = new Point(20, 312),
            Size = new Size(460, 68),
            BorderRadius = 8,
            BackColor = UITheme.InputBackground,
            Padding = new Padding(12, 8, 12, 8)
        };

        var lblChangeTitle = new Label
        {
            Text = "CHANGE DUE TO CUSTOMER",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(10, 8),
            AutoSize = true
        };

        _lblChangeVal = new Label
        {
            Text = "රු.0.00",
            Font = UITheme.FontTitle,
            ForeColor = UITheme.Success,
            Location = new Point(8, 28),
            AutoSize = true
        };

        changeCard.Controls.Add(lblChangeTitle);
        changeCard.Controls.Add(_lblChangeVal);

        // Optional Customer Name / Phone
        var lblCustomer = new Label
        {
            Text = "CUSTOMER NAME / MOBILE (OPTIONAL)",
            Font = UITheme.FontSmallBold,
            ForeColor = UITheme.TextSecondary,
            Location = new Point(20, 390),
            AutoSize = true
        };

        _txtCustomer = new ModernTextBox
        {
            Location = new Point(20, 410),
            Size = new Size(460, 38),
            PlaceholderText = "Walk-in customer or mobile number..."
        };

        // Action Buttons
        var btnCancel = new ModernButton
        {
            Text = "[ESC] CANCEL",
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Danger,
            Font = UITheme.FontBodyBold,
            Location = new Point(20, 460),
            Size = new Size(160, 46)
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnConfirm = new ModernButton
        {
            Text = "[↵] CONFIRM & PRINT",
            NormalColor = UITheme.Success,
            HoverColor = UITheme.SuccessDark,
            Font = UITheme.FontBodyBold,
            Location = new Point(190, 460),
            Size = new Size(290, 46)
        };
        btnConfirm.Click += (_, _) => ConfirmCheckout();

        mainPanel.Controls.Add(lblTr);
        mainPanel.Controls.Add(lblPayableTitle);
        mainPanel.Controls.Add(lblPayableVal);
        mainPanel.Controls.Add(lblMethod);
        mainPanel.Controls.Add(methodPanel);
        mainPanel.Controls.Add(lblTenderTitle);
        mainPanel.Controls.Add(_txtTender);
        mainPanel.Controls.Add(presetPanel);
        mainPanel.Controls.Add(changeCard);
        mainPanel.Controls.Add(lblCustomer);
        mainPanel.Controls.Add(_txtCustomer);
        mainPanel.Controls.Add(btnCancel);
        mainPanel.Controls.Add(btnConfirm);

        Controls.Add(mainPanel);

        if (initialTender.HasValue && initialTender.Value > 0)
        {
            _txtTender.Text = initialTender.Value.ToString("0.00");
        }
        else
        {
            _txtTender.Text = _totalPayable.ToString("0.00");
        }

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;

        Shown += (_, _) => _txtTender.FocusInput();
    }

    private ModernButton CreateMethodButton(string text, bool isDefault)
    {
        return new ModernButton
        {
            Text = text,
            Size = new Size(146, 36),
            Margin = new Padding(2, 0, 4, 0),
            Font = UITheme.FontSmallBold,
            BorderRadius = 6,
            NormalColor = isDefault ? UITheme.Primary : UITheme.CardSurfaceHover,
            HoverColor = UITheme.PrimaryDark
        };
    }

    private ModernButton CreatePresetButton(string text, decimal amount)
    {
        var btn = new ModernButton
        {
            Text = text,
            Height = 32,
            AutoSize = true,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(0, 0, 4, 0),
            Font = UITheme.FontSmallBold,
            BorderRadius = 6,
            NormalColor = UITheme.CardSurfaceHover,
            HoverColor = UITheme.Primary
        };
        btn.Click += (_, _) =>
        {
            if (text == "Exact Cash")
            {
                _txtTender.Text = amount.ToString("0.00");
            }
            else
            {
                decimal current = decimal.TryParse(_txtTender.Text, out decimal val) ? val : 0m;
                _txtTender.Text = (current + amount).ToString("0.00");
            }
        };
        return btn;
    }

    private void RecalculateChange()
    {
        if (decimal.TryParse(_txtTender.Text, out decimal tender))
        {
            decimal change = tender - _totalPayable;
            if (change >= 0)
            {
                _lblChangeVal.Text = $"රු.{change:N2}";
                _lblChangeVal.ForeColor = UITheme.Success;
            }
            else
            {
                _lblChangeVal.Text = $"-රු.{Math.Abs(change):N2} (SHORT)";
                _lblChangeVal.ForeColor = UITheme.Danger;
            }
        }
        else
        {
            _lblChangeVal.Text = "රු.0.00";
            _lblChangeVal.ForeColor = UITheme.TextMuted;
        }
    }

    private void ConfirmCheckout()
    {
        if (!decimal.TryParse(_txtTender.Text, out decimal tender) || tender < _totalPayable)
        {
            MessageBox.Show($"Tendered amount must be at least රු.{_totalPayable:N2}.", "Insufficient Payment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtTender.FocusInput();
            return;
        }

        TenderedAmount = tender;
        ChangeDue = tender - _totalPayable;
        DialogResult = DialogResult.OK;
        Close();
    }
}
