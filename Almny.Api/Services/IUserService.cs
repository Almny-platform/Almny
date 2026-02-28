namespace Almny.Api.Services;

public interface IUserService
{
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
}
