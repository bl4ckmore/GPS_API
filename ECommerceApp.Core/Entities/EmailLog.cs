// ECommerceApp.Core.Entities/EmailLog.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Core.Entities;

public class EmailLog
{
    // The default primary key (ID)
    public Guid id { get; set; } = Guid.NewGuid();

    // Link back to the order that triggered this email
    public Guid OrderId { get; set; }

    // Who the email was sent to
    [MaxLength(256)]
    public string To { get; set; } = string.Empty;

    // Subject of the email
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    // When the log entry was created (i.e., when order was placed)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // When the email was actually sent
    public DateTime? SentAt { get; set; }

    // Status: PendingSend, Success, Failed
    [MaxLength(50)]
    public string Status { get; set; } = "PendingSend";

    // Error message if it failed
    public string? ErrorMessage { get; set; }

    // Log the content/template, but potentially truncated (we'll save up to 4000 chars)
    public string? BodyContent { get; set; }
}