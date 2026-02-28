using System.Security.Cryptography;
using System.Web;
using Almny.Api.Abstractions;
using Almny.Api.Authentication;
using MapsterMapper;

namespace Almny.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtProvider _jwtProvider;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    private const int RefreshTokenExpiryDays = 14;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtProvider jwtProvider,
        IEmailService emailService,
        IUserService userService,
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtProvider = jwtProvider;
        _emailService = emailService;
        _userService = userService;
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string baseUrl)
    {
        if (await _userService.ExistsByEmailAsync(request.Email))
            return Result.Failure<AuthResponse>(UserErrors.DuplicateEmail);

        var user = _mapper.Map<ApplicationUser>(request);
        user.CreatedAt = DateTime.UtcNow;

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Result.Failure<AuthResponse>(UserErrors.RegistrationFailed);

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        await SendConfirmationEmailAsync(user, baseUrl);

        return Result.Success(await GenerateAuthResponseAsync(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (await _userManager.IsLockedOutAsync(user))
            return Result.Failure<AuthResponse>(UserErrors.LockedOut);

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isValidPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

        await _userManager.ResetAccessFailedCountAsync(user);

        return Result.Success(await GenerateAuthResponseAsync(user));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken
                && !rt.IsRevoked
                && rt.ExpiresAt > DateTime.UtcNow);

        if (storedToken is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

        var user = await _userService.GetByIdAsync(storedToken.UserId);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.NotFound);

        storedToken.IsRevoked = true;
        await _dbContext.SaveChangesAsync();

        return Result.Success(await GenerateAuthResponseAsync(user));
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmationEmailRequest request)
    {
        var user = await _userService.GetByIdAsync(request.UserId);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var result = await _userManager.ConfirmEmailAsync(user, request.Code);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(UserErrors.EmailConfirmationFailed);
    }

    public async Task<Result<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, string baseUrl)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user is null)
            return Result.Success(new ForgotPasswordResponse("If the email exists, a reset link has been sent."));

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedCode = HttpUtility.UrlEncode(code);
        var resetLink = $"{baseUrl}/api/auth/reset-password?email={HttpUtility.UrlEncode(user.Email!)}&code={encodedCode}";

        var emailBody = EmailTemplateBuilder.BuildResetPasswordEmail(user.FullName, resetLink);

        try
        {
            await _emailService.SendEmailAsync(user.Email!, "Reset your password", emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}", user.Email);
            _logger.LogInformation("Reset link for {Email}: {Link}", user.Email, resetLink);
        }

        return Result.Success(new ForgotPasswordResponse("If the email exists, a reset link has been sent."));
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user is null)
            return Result.Failure<ResetPasswordResponse>(UserErrors.InvalidResetToken);

        var decodedCode = HttpUtility.UrlDecode(request.Code);
        var result = await _userManager.ResetPasswordAsync(user, decodedCode, request.NewPassword);

        if (!result.Succeeded)
            return Result.Failure<ResetPasswordResponse>(UserErrors.InvalidResetToken);

        return Result.Success(new ResetPasswordResponse("Password has been reset successfully."));
    }

    public async Task<Result> ResendConfirmationAsync(ResendConfirmationEmailRequest request, string baseUrl)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user is null || user.EmailConfirmed)
            return Result.Success();

        await SendConfirmationEmailAsync(user, baseUrl);

        return Result.Success();
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, string userId)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken
                && rt.UserId == userId
                && !rt.IsRevoked);

        if (storedToken is null)
            return Result.Failure(UserErrors.InvalidRefreshToken);

        storedToken.IsRevoked = true;
        await _dbContext.SaveChangesAsync();

        return Result.Success();
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = RolePermissions.GetPermissionsForRoles(roles);

        var (token, expiresIn) = _jwtProvider.GenerateToken(user, roles, permissions);
        var refreshToken = GenerateRefreshToken();

        await SaveRefreshTokenAsync(user.Id, refreshToken);

        return new AuthResponse(token, expiresIn, refreshToken);
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user, string baseUrl)
    {
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedCode = HttpUtility.UrlEncode(code);
        var confirmationLink = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&code={encodedCode}";

        var emailBody = EmailTemplateBuilder.BuildConfirmationEmail(user.FullName, confirmationLink);

        try
        {
            await _emailService.SendEmailAsync(user.Email!, "Confirm your email", emailBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send confirmation email to {Email}", user.Email);
            _logger.LogInformation("Confirmation link for {Email}: {Link}", user.Email, confirmationLink);
        }
    }

    private async Task SaveRefreshTokenAsync(string userId, string token)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
    }
}
