namespace ShopKeeperPro.Core.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public bool IsAdmin => string.Equals(Role, "Administrator", StringComparison.OrdinalIgnoreCase);
}
