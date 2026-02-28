using Almny.Api.Abstractions;

namespace Almny.Api.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string baseUrl);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result> ConfirmEmailAsync(ConfirmationEmailRequest request);
    Task<Result<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, string baseUrl);
    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<Result> ResendConfirmationAsync(ResendConfirmationEmailRequest request, string baseUrl);
    Task<Result> RevokeRefreshTokenAsync(string refreshToken, string userId);
}
