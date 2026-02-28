namespace Almny.Api.Contracts.Authentication;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword
);
