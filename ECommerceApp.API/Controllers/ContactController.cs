using Microsoft.AspNetCore.Mvc;
using ECommerceApp.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using ECommerceApp.Infrastructure.Email;

namespace ECommerceApp.API.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public ContactController(IEmailSender emailSender, IConfiguration config)
    {
        _emailSender = emailSender;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ContactRequest req)
    {
        // 1. Get Admin Emails from config
        var adminEmails = _config["AppConfig:ORDERS_ADMIN_EMAIL"];
        if (string.IsNullOrEmpty(adminEmails)) return BadRequest("Admin email not configured.");

        // 2. Build the Email Body
        var subject = $"[Contact Us] New Message from {req.Name}";
        var body = $@"
            <h2>New Contact Message</h2>
            <p><strong>From:</strong> {req.Name}</p>
            <p><strong>Email:</strong> {req.Email}</p>
            <p><strong>Phone:</strong> {req.Phone ?? "N/A"}</p>
            <hr />
            <h3>Message:</h3>
            <p>{req.Message}</p>
            <hr />
            <p style='color:gray; font-size:12px;'>Reply to this email to contact the user directly at {req.Email}</p>
        ";

        // 3. Send to all Admins
        var emails = adminEmails.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var email in emails)
        {
            await _emailSender.SendAsync(email.Trim(), subject, body);
        }

        return Ok(new { message = "Message sent successfully!" });
    }
}

public class ContactRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; } = string.Empty;
    [Required] public string Message { get; set; } = string.Empty;
} 