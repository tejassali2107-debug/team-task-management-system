using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<Result<List<TaskDto>>> GetAllAsync(TaskFilterRequest filter);
    Task<Result<TaskDto>> GetByIdAsync(Guid id);
    Task<Result<TaskDto>> CreateAsync(CreateTaskRequest request);
    Task<Result<TaskDto>> UpdateAsync(Guid id, UpdateTaskRequest request);
    Task<Result<TaskDto>> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request);
    Task<Result> DeleteAsync(Guid id);
}
