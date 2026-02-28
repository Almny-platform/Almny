using Microsoft.AspNetCore.Authorization;

namespace Almny.Api.Authentication.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class WithPermissionAttribute : AuthorizeAttribute
{
    public WithPermissionAttribute(string permission) : base(permission)
    {
    }
}
