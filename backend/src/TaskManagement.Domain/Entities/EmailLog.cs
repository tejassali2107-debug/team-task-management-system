using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Records every outbound notification email. Used as the mock email transport
/// when no real SMTP provider is configured, so notifications are still auditable.
/// </summary>
public class EmailLog : BaseEntity
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Sent { get; set; }
}
