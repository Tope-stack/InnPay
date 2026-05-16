using InnPay.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult FromResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return StatusCode(result.StatusCode, new ApiResponse<T>(true, result.Data));

        return StatusCode(result.StatusCode, new ApiResponse<T>(false, default, result.Error));
    }

    protected IActionResult FromResult(ServiceResult result)
    {
        if (result.IsSuccess)
            return StatusCode(result.StatusCode, new ApiResponse<object>(true, null));

        return StatusCode(result.StatusCode, new ApiResponse<object>(false, null, result.Error));
    }

    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : Guid.Empty;

    protected Guid CurrentAccountId =>
        Guid.TryParse(User.FindFirst("accountId")?.Value, out var id) ? id : Guid.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; }
    public T? Data { get; }
    public string? Error { get; }

    public ApiResponse(bool success, T? data, string? error = null)
    {
        Success = success;
        Data = data;
        Error = error;
    }
}
