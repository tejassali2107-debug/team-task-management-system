using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// Simulates outbound email delivery: logs the message and persists it to
/// EmailLogs so notification events are auditable without a real SMTP provider.
/// </summary>
public class MockEmailSender : IEmailSender
{
    private readonly AppDbContext _db;
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(AppDbContext db, ILogger<MockEmailSender> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("[MOCK EMAIL] To: {ToEmail} | Subject: {Subject} | Body: {Body}", toEmail, subject, body);

        _db.EmailLogs.Add(new EmailLog
        {
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            Sent = true
        });

        await _db.SaveChangesAsync();
    }
}
