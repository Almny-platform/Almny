using Almny.Api.Abstractions;
using Almny.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Almny.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authService.RegisterAsync(request, baseUrl);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("login")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpGet("confirm-email")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        var request = new ConfirmationEmailRequest(userId, code);
        var result = await _authService.ConfirmEmailAsync(request);

        var placeholders = result.IsSuccess
            ? new Dictionary<string, string>
            {
                ["Icon"] = "✅",
                ["StatusClass"] = "success",
                ["Title"] = "Email Confirmed",
                ["Message"] = "Your email has been confirmed successfully. You can now log in."
            }
            : new Dictionary<string, string>
            {
                ["Icon"] = "❌",
                ["StatusClass"] = "error",
                ["Title"] = "Confirmation Failed",
                ["Message"] = "The confirmation link is invalid or has expired. Please request a new one."
            };

        var html = EmailBodyBuilder.Build("confirm-email-result.html", placeholders);

        return Content(html, "text/html");
    }

    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authService.ResendConfirmationAsync(request, baseUrl);

        return result.IsSuccess
            ? Ok(new { Message = "If the email exists and is unconfirmed, a confirmation email has been sent." })
            : result.ToProblemDetails();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _authService.ForgotPasswordAsync(request, baseUrl);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPasswordForm()
    {
        var html = EmailBodyBuilder.Build("reset-password-form.html", []);

        return Content(html, "text/html");
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("Authentication")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("revoke-refresh-token")]
    [Authorize]
    [EnableRateLimiting("Api")]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken, userId);

        return result.IsSuccess
            ? Ok(new { Message = "Refresh token revoked." })
            : result.ToProblemDetails();
    }
}
