using Almny.Api.Abstractions;

namespace Almny.Api.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "User was not found.");

    public static readonly Error DuplicateEmail =
        Error.Conflict("User.DuplicateEmail", "A user with this email already exists.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Invalid email or password.");

    public static readonly Error EmailNotConfirmed =
        Error.Unauthorized("User.EmailNotConfirmed", "Email has not been confirmed.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("User.InvalidRefreshToken", "The refresh token is invalid or expired.");

    public static readonly Error LockedOut =
        Error.Unauthorized("User.LockedOut", "This account has been locked out. Please try again later.");

    public static readonly Error InvalidResetToken =
        Error.Validation("User.InvalidResetToken", "The password reset token is invalid or expired.");

    public static readonly Error RegistrationFailed =
        Error.Validation("User.RegistrationFailed", "User registration failed.");

    public static readonly Error ResetPasswordFailed =
        Error.Validation("User.ResetPasswordFailed", "Password reset failed.");

    public static readonly Error EmailConfirmationFailed =
        Error.Validation("User.EmailConfirmationFailed", "Email confirmation failed.");
}
