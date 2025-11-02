namespace ECommerceApp.Infrastructure.Email;

public sealed class EmailOptions
{
    public string FromName { get; set; } = "";
    public string FromAddress { get; set; } = "";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;

    public string User { get; set; } = "";
    public string Password { get; set; } = "";

    // For protocol log file (smtp-YYYYMMDD.log)
    public bool EnableLogging { get; set; } = false;
}
