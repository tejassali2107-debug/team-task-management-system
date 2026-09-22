using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[Route("api/teams")]
[Authorize]
public class TeamsController : ApiControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _teamService.GetAllAsync();
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _teamService.GetByIdAsync(id);
        return FromResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request)
    {
        var result = await _teamService.CreateAsync(request);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamRequest request)
    {
        var result = await _teamService.UpdateAsync(id, request);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _teamService.DeleteAsync(id);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddTeamMemberRequest request)
    {
        var result = await _teamService.AddMemberAsync(id, request);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var result = await _teamService.RemoveMemberAsync(id, userId);
        return FromResult(result);
    }
}
