namespace ECommerceApp.Infrastructure.Email;

public sealed class EmailOptions
{
    public string FromName { get; set; } = "GPS Hub";
    public string FromEmail { get; set; } = ""; // Changed from FromAddress to match JSON
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableLogging { get; set; } = true;
}