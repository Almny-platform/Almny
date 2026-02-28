using System.Security.Claims;

namespace Almny.Api.Extensions;

public static class UserExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);
}
