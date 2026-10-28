using System.Text.RegularExpressions;

namespace ShopKeeperPro.Core.Services;

public record BarcodeScanResult(bool IsValid, string Barcode, int Quantity, string ErrorMessage);

public interface ISoundPlayer
{
    void PlaySuccess();
    void PlayAlert();
}

public class BarcodeScannerService
{
    private static readonly Regex BarcodePattern = new(@"^[A-Za-z0-9_\-\.]{3,64}$", RegexOptions.Compiled);
    private static readonly Regex MultiplierPattern = new(@"^(\d+)\s*\*\s*([A-Za-z0-9_\-\.]+)$", RegexOptions.Compiled);
    private readonly ISoundPlayer? _soundPlayer;

    public BarcodeScannerService(ISoundPlayer? soundPlayer = null)
    {
        _soundPlayer = soundPlayer;
    }

    public BarcodeScanResult ParseInput(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new BarcodeScanResult(false, string.Empty, 0, "Barcode input cannot be empty.");
        }

        string cleaned = rawInput.Trim().Replace("\r", "").Replace("\n", "");

        // Check for multiplier format like "3*123456789"
        var multiMatch = MultiplierPattern.Match(cleaned);
        if (multiMatch.Success)
        {
            int qty = int.Parse(multiMatch.Groups[1].Value);
            string barcode = multiMatch.Groups[2].Value;
            if (qty <= 0 || qty > 999)
            {
                return new BarcodeScanResult(false, barcode, 0, "Quantity multiplier must be between 1 and 999.");
            }
            if (!BarcodePattern.IsMatch(barcode))
            {
                return new BarcodeScanResult(false, barcode, 0, "Invalid barcode format inside multiplier.");
            }
            return new BarcodeScanResult(true, barcode, qty, string.Empty);
        }

        if (!BarcodePattern.IsMatch(cleaned))
        {
            return new BarcodeScanResult(false, cleaned, 0, "Invalid barcode format (3-64 alphanumeric characters).");
        }

        return new BarcodeScanResult(true, cleaned, 1, string.Empty);
    }

    public void NotifyScanSuccess()
    {
        _soundPlayer?.PlaySuccess();
    }

    public void NotifyScanAlert()
    {
        _soundPlayer?.PlayAlert();
    }
}
