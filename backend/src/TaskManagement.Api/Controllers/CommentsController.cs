using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[Route("api/tasks/{taskId:guid}/comments")]
[Authorize]
public class CommentsController : ApiControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetForTask(Guid taskId)
    {
        var result = await _commentService.GetForTaskAsync(taskId);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid taskId, [FromBody] CreateCommentRequest request)
    {
        var result = await _commentService.AddAsync(taskId, request);
        return FromResult(result);
    }
}
