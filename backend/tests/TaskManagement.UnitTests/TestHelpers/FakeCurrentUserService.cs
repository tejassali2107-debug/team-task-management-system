using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.UnitTests.TestHelpers;

public class FakeCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "test@taskflow.com";
    public UserRole Role { get; set; } = UserRole.Admin;
    public Guid? TeamId { get; set; }
    public bool IsAuthenticated { get; set; } = true;
}
