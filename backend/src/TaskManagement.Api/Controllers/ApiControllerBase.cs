using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common;

namespace TaskManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result.Data);
        }

        return Problem(result.Error);
    }

    protected IActionResult FromResult(Result result)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        return Problem(result.Error);
    }

    private IActionResult Problem(string? error)
    {
        var message = error ?? "Request could not be completed.";
        var statusCode = message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : message.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
              message.Contains("access", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

        return StatusCode(statusCode, new { message });
    }
}
