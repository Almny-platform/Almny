namespace Almny.Api.Contracts.Authentication;

public record ConfirmationEmailRequest(
    string UserId,
    string Code
);
