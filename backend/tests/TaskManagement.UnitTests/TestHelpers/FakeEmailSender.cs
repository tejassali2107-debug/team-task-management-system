using TaskManagement.Application.Interfaces;

namespace TaskManagement.UnitTests.TestHelpers;

public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string toEmail, string subject, string body)
    {
        SentEmails.Add((toEmail, subject, body));
        return Task.CompletedTask;
    }
}
