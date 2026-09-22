using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
    Guid? TeamId { get; }
    bool IsAuthenticated { get; }
}
