namespace Almny.Api.Contracts.Authentication;

public record AuthResponse(
    string Token,
    int ExpiresIn,
    string RefreshToken
);
