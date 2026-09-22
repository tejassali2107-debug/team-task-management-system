using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITeamService
{
    Task<Result<List<TeamDto>>> GetAllAsync();
    Task<Result<TeamDto>> GetByIdAsync(Guid id);
    Task<Result<TeamDto>> CreateAsync(CreateTeamRequest request);
    Task<Result<TeamDto>> UpdateAsync(Guid id, UpdateTeamRequest request);
    Task<Result> DeleteAsync(Guid id);
    Task<Result<TeamDto>> AddMemberAsync(Guid teamId, AddTeamMemberRequest request);
    Task<Result<TeamDto>> RemoveMemberAsync(Guid teamId, Guid userId);
}
