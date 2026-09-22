using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ICommentService
{
    Task<Result<List<CommentDto>>> GetForTaskAsync(Guid taskId);
    Task<Result<CommentDto>> AddAsync(Guid taskId, CreateCommentRequest request);
}
