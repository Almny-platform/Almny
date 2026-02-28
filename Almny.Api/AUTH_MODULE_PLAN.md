# 🔐 Almny.Api — Authentication Module Implementation Plan

## 📋 Project Context

| Property | Value |
|---|---|
| **Framework** | .NET 10 / C# 14 |
| **Project Type** | ASP.NET Core Controller (single project) |
| **Identity** | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.3 |
| **Auth** | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.3) |
| **Validation** | FluentValidation 12.1.1 |
| **Mapping** | Mapster 7.4.0 |
| **ORM** | EF Core 10.0.3 + SQL Server | |
| **Rate Limiting** | Fixed-window (`Authentication`, `Api`, `Global` policies) |

### Existing Files (already in place)

| File | Status |
|---|---|
| `Entities/ApplicationUser.cs` | ✅ Exists (`IdentityUser` with `FullName`, `CreatedAt`) |
| `Entities/RefreshToken.cs` | ✅ Exists (Id, Token, UserId, ExpiresAt, CreatedAt, IsRevoked, User nav) |
| `Contracts/Authentication/LoginRequest.cs` | ✅ Exists (record: Email, Password) |
| `Contracts/Authentication/SignupRequest.cs` | ✅ Exists (record: FullName, Email, Password) |
| `Contracts/Authentication/ForgotPasswordRequest.cs` | ✅ Exists (record: Email) |
| `Dependencies.cs` | ✅ Exists (DI, JWT, OAuth, RateLimiting, Swagger — references types not yet created) |
| `Program.cs` | ✅ Exists (calls `MapAuthEndpoints()`) |
| `GlobalUsings.cs` | ✅ Exists (declares target namespaces) |
| `appsettings.json` | ✅ Exists (JWT, Mail, RateLimiting, OAuth sections) |
| `Migrations/` | ✅ Initial migration exists (references `Almny.Api.Data.ApplicationDbContext`) |

### Namespace Alignment Issue ⚠️

`Dependencies.cs` currently uses **old** namespaces (`Almny.Api.Data`, `Almny.Api.Models`, `Almny.Api.Configuration`).  
`GlobalUsings.cs` declares the **new/target** namespaces (`Almny.Api.Persistence`, `Almny.Api.Entities`, `Almny.Api.Configurations`).  
During implementation we must **update `Dependencies.cs` usings** to match the new folder/namespace structure.

---

## 🗂️ Full File Tree (to create)

```
Almny.Api/
├── Abstractions/
│   ├── Error.cs
│   ├── Result.cs
│   └── ResultExtensions.cs
│
├── Authentication/
│   ├── IJwtProvider.cs
│   ├── JwtOptions.cs
│   ├── JwtProvider.cs
│   └── Filters/
│       ├── PermissionAuthorizationHandler.cs
│       ├── PermissionPolicyProvider.cs
│       ├── PermissionRequirement.cs
│       └── WithPermissionAttribute.cs
│
├── Configurations/                        (match GlobalUsings namespace)
│                
│   └── RateLimitingOptions.cs             (move/create)
│
├── Contracts/Authentication/
│   ├── AuthResponse.cs                    ★ NEW
│   ├── ConfirmationEmailRequest.cs        ★ NEW
│   ├── ForgotPasswordRequest.cs           ✅ EXISTS (rename to ForgetPasswordRequest)
│   ├── ForgotPasswordResponse.cs          ★ NEW
│   ├── ForgotPasswordValidator.cs         ★ NEW
│   ├── LoginRequest.cs                    ✅ EXISTS
│   ├── LoginRequestValidator.cs           ★ NEW
│   ├── RefreshTokenRequest.cs             ★ NEW
│   ├── RefreshTokenRequestValidator.cs    ★ NEW
│   ├── RegisterRequest.cs                 ★ NEW (replaces/supplements SignupRequest)
│   ├── RegisterRequestValidator.cs        ★ NEW
│   ├── ResendConfirmationEmailRequest.cs  ★ NEW
│   ├── ResetPasswordRequest.cs            ★ NEW
│   ├── ResetPasswordResponse.cs           ★ NEW
│   ├── ResetPasswordValidator.cs          ★ NEW
│   └── SignupRequest.cs                   ✅ EXISTS ( keep alongside RegisterRequest)
│
├── Controllers/
│   └── AuthController.cs                  ★ NEW  (or Endpoints/AuthEndpoints.cs — see decision below)
│
├── Entities/
│   ├── ApplicationUser.cs                 ✅ EXISTS
│   └── RefreshToken.cs                    ✅ EXISTS
│
├── Errors/
│   └── UserErrors.cs                      ★ NEW
│
├── Extensions/
│   └── UserExtensions.cs                  ★ NEW
│
├── Helpers/
│   └── EmailBodyBuilder.cs               ★ NEW
│
├── Mapping/
│   └── MappingConfigurations.cs           ★ NEW (Mapster IRegister)
│
├── Persistence/
│   ├── ApplicationDbContext.cs            ★ NEW (IdentityDbContext<ApplicationUser>)
│   └── EntitiesConfigurations/
│       └── UserConfigurations.cs          ★ NEW (IEntityTypeConfiguration<ApplicationUser>)
│
├── Services/
│   ├── IAuthService.cs                    ★ NEW
│   ├── AuthService.cs                     ★ NEW
│   ├── IEmailService.cs                   ★ NEW (interface — currently referenced but missing)
│   ├── EmailService.cs                    ★ NEW
│   ├── IUserService.cs                    ★ NEW (interface — currently referenced but missing)
│   ├── UserService.cs                     ★ NEW
│   └── EmailTemplates/
│       └── EmailTemplateBuilder.cs        ★ NEW
│
├── Templates/
│   ├── confirmation-email.html            ★ NEW
│   └── reset-password.html               ★ NEW
│
├── Dependencies.cs                        🔧 UPDATE (fix namespaces/usings)
├── GlobalUsings.cs                        🔧 UPDATE (add new namespaces if needed)
├── Program.cs                             🔧 UPDATE (if switching to Controllers)
└── appsettings.json                       ✅ NO CHANGE
```

---

## 🔨 Implementation Phases

### Phase 0 — Abstractions (foundation for Result pattern)

| # | File | Description |
|---|---|---|
| 0.1 | `Abstractions/Error.cs` | `record Error(string Code, string Description, ErrorType Type)` + `ErrorType` enum (Validation, NotFound, Conflict, Unauthorized, Forbidden). Static factory methods. |
| 0.2 | `Abstractions/Result.cs` | Generic `Result<T>` and non-generic `Result`. Properties: `IsSuccess`, `IsFailure`, `Value`, `Error`. Static `Success()` / `Failure()` factories. |
| 0.3 | `Abstractions/ResultExtensions.cs` | Extension methods on `Result<T>`: `Match()`, `ToProblemDetails()` (maps `ErrorType` → HTTP status codes). |

**Dependencies:** None — pure types.

---

### Phase 1 — Configurations (already referenced in `Dependencies.cs`)

| # | File | Description |
|---|---|---|
| 1.1 | `Configurations/MailConfig.cs` | POCO for `MailConfig` section (SenderEmail, SenderName, SmtpServer, Port, Username, Password). `const string SectionName = "MailConfig"`. |
| 1.2 | `Configurations/GoogleOAuthOptions.cs` | POCO (ClientId, ClientSecret). `SectionName = "Authentication:Google"`. |
| 1.3 | `Configurations/GitHubOAuthOptions.cs` | POCO (ClientId, ClientSecret). `SectionName = "Authentication:GitHub"`. |
| 1.4 | `Configurations/RateLimitingOptions.cs` | Nested POCOs for Global / Authentication / Api windows. `SectionName = "RateLimiting"`. |

**Dependencies:** None — pure config models.  
**Action on `Dependencies.cs`:** Replace old `using Almny.Api.Configuration;` → `using Almny.Api.Configurations;` (already matches `GlobalUsings.cs`).

---

### Phase 2 — Persistence

| # | File | Description |
|---|---|---|
| 2.1 | `Persistence/ApplicationDbContext.cs` | Inherits `IdentityDbContext<ApplicationUser>`. Registers `DbSet<RefreshToken>`. Calls `ApplyConfigurationsFromAssembly`. |
| 2.2 | `Persistence/EntitiesConfigurations/UserConfigurations.cs` | `IEntityTypeConfiguration<ApplicationUser>` — set max lengths, indexes, etc. |

**⚠️ Migration note:** Existing migration references `Almny.Api.Data.ApplicationDbContext`. After moving the DbContext to `Almny.Api.Persistence`, we will need to either:  
 Create a new migration  


---

### Phase 3 — Authentication (JWT infrastructure)

| # | File | Description |
|---|---|---|
| 3.1 | `Authentication/JwtOptions.cs` | POCO bound to `Jwt` config section (Key, Issuer, Audience, ExpiryInMinutes). |
| 3.2 | `Authentication/IJwtProvider.cs` | Interface: `(string Token, int ExpiresIn) GenerateToken(ApplicationUser user)`. |
| 3.3 | `Authentication/JwtProvider.cs` | Implementation using `System.IdentityModel.Tokens.Jwt`. Reads `IOptions<JwtOptions>`. Creates access token with claims (sub, email, fullName, jti). |

---

### Phase 4 — Permission-based Authorization Filters

| # | File | Description |
|---|---|---|
| 4.1 | `Authentication/Filters/WithPermissionAttribute.cs` | `[WithPermission("permission")]` — sets policy name = permission string. |
| 4.2 | `Authentication/Filters/PermissionRequirement.cs` | `IAuthorizationRequirement` holding a `Permission` string. |
| 4.3 | `Authentication/Filters/PermissionPolicyProvider.cs` | Dynamic `IAuthorizationPolicyProvider` that creates a policy per permission string on the fly. |
| 4.4 | `Authentication/Filters/PermissionAuthorizationHandler.cs` | `AuthorizationHandler<PermissionRequirement>` — checks the user's claims for the required permission. |

**Action on `Dependencies.cs`:** Register `IAuthorizationPolicyProvider` and `IAuthorizationHandler<PermissionRequirement>` as singletons/scoped.

---

### Phase 5 — Errors

| # | File | Description |
|---|---|---|
| 5.1 | `Errors/UserErrors.cs` | Static class returning `Error` instances: `NotFound`, `DuplicateEmail`, `InvalidCredentials`, `EmailNotConfirmed`, `InvalidRefreshToken`, `LockedOut`, `InvalidResetToken`, etc. |

---

### Phase 6 — Contracts / DTOs + Validators

| # | File | What it holds |
|---|---|---|
| 6.1 | `Contracts/Authentication/AuthResponse.cs` | `record AuthResponse(string Token, int ExpiresIn, string RefreshToken)` |
| 6.2 | `Contracts/Authentication/RegisterRequest.cs` | `record RegisterRequest(string FullName, string Email, string Password, string ConfirmPassword)` |
| 6.3 | `Contracts/Authentication/RegisterRequestValidator.cs` | FluentValidation rules (email format, password match, min length 8, uppercase, digit). |
| 6.4 | `Contracts/Authentication/LoginRequestValidator.cs` | Validates existing `LoginRequest` (non-empty email & password). |
| 6.5 | `Contracts/Authentication/RefreshTokenRequest.cs` | `record RefreshTokenRequest(string Token, string RefreshToken)` |
| 6.6 | `Contracts/Authentication/RefreshTokenRequestValidator.cs` | Non-empty Token & RefreshToken. |
| 6.7 | `Contracts/Authentication/ConfirmationEmailRequest.cs` | `record ConfirmationEmailRequest(string UserId, string Code)` |
| 6.8 | `Contracts/Authentication/ResendConfirmationEmailRequest.cs` | `record ResendConfirmationEmailRequest(string Email)` |
| 6.9 | `Contracts/Authentication/ForgotPasswordResponse.cs` | `record ForgotPasswordResponse(string Message)` |
| 6.10 | `Contracts/Authentication/ForgotPasswordValidator.cs` | Validates `ForgotPasswordRequest` (email format). |
| 6.11 | `Contracts/Authentication/ResetPasswordRequest.cs` | `record ResetPasswordRequest(string Email, string Code, string NewPassword)` |
| 6.12 | `Contracts/Authentication/ResetPasswordResponse.cs` | `record ResetPasswordResponse(string Message)` |
| 6.13 | `Contracts/Authentication/ResetPasswordValidator.cs` | Validates `ResetPasswordRequest`. |

* keep alongside RegisterRequest

---

### Phase 7 — Mapping

| # | File | Description |
|---|---|---|
| 7.1 | `Mapping/MappingConfigurations.cs` | Implements Mapster `IRegister`. Maps `RegisterRequest` → `ApplicationUser`, `ApplicationUser` + tokens → `AuthResponse`, etc. |

---

### Phase 8 — Email Templates & Helpers

| # | File | Description |
|---|---|---|
| 8.1 | `Templates/confirmation-email.html` | HTML template with `{{FullName}}`, `{{ConfirmationLink}}` placeholders. |
| 8.2 | `Templates/reset-password.html` | HTML template with `{{FullName}}`, `{{ResetLink}}` placeholders. |
| 8.3 | `Helpers/EmailBodyBuilder.cs` | Reads template file, replaces placeholders, returns HTML string. |
| 8.4 | `Services/EmailTemplates/EmailTemplateBuilder.cs` | Higher-level builder: generates confirmation / reset-password email bodies using `EmailBodyBuilder` + config URLs. |

**Note:** Templates should be set as **Content / Copy to Output** in the `.csproj`.

---

### Phase 9 — Services (core business logic)

| # | File | Responsibilities |
|---|---|---|
| 9.1 | `Services/IEmailService.cs` | `Task SendEmailAsync(string to, string subject, string htmlBody)` |
| 9.2 | `Services/EmailService.cs` | SMTP implementation using `IOptions<MailConfig>`. |
| 9.3 | `Services/IUserService.cs` | User CRUD helpers (get by id/email, check existence). Wraps `UserManager<ApplicationUser>`. |
| 9.4 | `Services/UserService.cs` | Implementation. |
| 9.5 | `Services/IAuthService.cs` | `Task<Result<AuthResponse>> RegisterAsync(RegisterRequest)`, `LoginAsync`, `RefreshTokenAsync`, `ConfirmEmailAsync`, `ForgotPasswordAsync`, `ResetPasswordAsync`, `ResendConfirmationAsync`, `RevokeRefreshTokenAsync`. |
| 9.6 | `Services/AuthService.cs` | Implementation. Orchestrates `UserManager`, `IJwtProvider`, `IEmailService`, `IUserService`, `ApplicationDbContext` (for refresh tokens). Returns `Result<T>` / `Result`. |

---

### Phase 10 — Extensions

| # | File | Description |
|---|---|---|
| 10.1 | `Extensions/UserExtensions.cs` | Extension methods on `ApplicationUser` (e.g., `ToAuthResponse()`, claim helpers) and possibly `ClaimsPrincipal` extensions (`GetUserId()`, `GetEmail()`). |

---

### Phase 11 — Controller / Endpoints

| # | File | Description |
|---|---|---|
| 11.1 | `Controllers/AuthController.cs`  
-  MVC Controller** (`Controllers/AuthController.cs`) — requires adding `builder.Services.AddControllers()` + `app.MapControllers()`?

#### Auth Endpoints

| Method | Route | Description | Rate Limit Policy |
|---|---|---|---|
| POST | `/api/auth/register` | Register new user, send confirmation email | `Authentication` |
| POST | `/api/auth/login` | Login, return JWT + refresh token | `Authentication` |
| POST | `/api/auth/refresh` | Refresh access token | `Authentication` |
| POST | `/api/auth/confirm-email` | Confirm email with userId + code | `Authentication` |
| POST | `/api/auth/resend-confirmation` | Resend confirmation email | `Authentication` |
| POST | `/api/auth/forgot-password` | Send password reset email | `Authentication` |
| POST | `/api/auth/reset-password` | Reset password with code | `Authentication` |
| POST | `/api/auth/revoke-refresh-token` | Revoke a refresh token (authenticated) | `Api` |

---

### Phase 12 — Wiring & Cleanup

| # | Task | Description |
|---|---|---|
| 12.1 | Update `Dependencies.cs` | Fix `using` statements to new namespaces. Register Permission authorization handler/policy provider. Remove references to `Almny.Api.Data`, `Almny.Api.Models`, `Almny.Api.Configuration`. |
| 12.2 | Update `GlobalUsings.cs` | Add any new namespaces (`Almny.Api.Abstractions`, `Almny.Api.Authentication`, `Almny.Api.Extensions`, `Almny.Api.Helpers`, `Almny.Api.Mapping`). |
| 12.3 | Update `Program.cs` | If using Controllers (option B), add `AddControllers()` / `MapControllers()`. Otherwise no change needed. |
| 12.4 | Update `.csproj` | Add `<Content>` items for `Templates/*.html` with `CopyToOutputDirectory`. Remove empty `<Folder>` includes that are now populated. |
| 12.5 | Decide on `SignupRequest.cs` | Remove or keep alongside `RegisterRequest.cs`. |
| 12.6 | Migration | Decide whether to re-scaffold migration after moving `ApplicationDbContext` namespace. |

---

## ❓ Information Needed Before Implementation

| # | Question | Why it matters |
|---|---|---|
| 1 | **Minimal API endpoints vs. MVC Controller?** | `Program.cs` currently uses `MapAuthEndpoints()`. Creating `Controllers/AuthController.cs` would require switching to `AddControllers()` + `MapControllers()`. Which approach do you prefer? Controllers |
| 2 | **Keep `SignupRequest.cs` or replace with `RegisterRequest.cs`?** | Both represent registration — keeping both causes confusion. Replace with register request | 
| 3 | **Namespace for `ApplicationDbContext`?** | Existing migration uses `Almny.Api.Data`. `GlobalUsings.cs` declares `Almny.Api.Persistence`. Should we create a new migration after the move, or add a namespace alias? create new|
| 4 | **Repositories layer?** | `Dependencies.cs` registers `IUserRepository` / `UserRepository` but your plan doesn't mention a Repositories folder. Should `Persistence/Repositories/` be created, or will `UserService` directly use `UserManager` + `DbContext`? No Repository Pattern — Direct UserManager + DbContext
This project does not use a Persistence/Repositories/ folder or a repository abstraction layer. Instead, services inject and use both directly. |
| 5 | **Permission source?** | `PermissionAuthorizationHandler` needs to know *where* permissions come from (claims in JWT, database lookup, or a static enum). Which approach? 1.	Define permissions as constants (static class or enum)
2.	Map Roles → Permissions in a static dictionary at startup
3.	Embed resolved permissions as claims at token generation time in JwtProvider
4.	PermissionAuthorizationHandler reads only from User — zero DB hit |
| 6 | **Email confirmation / reset URLs?** | What base URL format should the confirmation & reset links use? (e.g., `https://almny.com/confirm?userId={}&code={}`) — or should this come from `appsettings.json`? appsettings.json |
| 7 | **Refresh token storage?** | `RefreshToken` entity exists. Should refresh tokens be stored in the `RefreshTokens` DB table (current design), or do you prefer a different approach (e.g., in-memory cache)?Keep the DB Table Approach |

---

## 📐 Dependency Graph (implementation order)

```
Phase 0: Abstractions ──────────────────────────────────────────────┐
Phase 1: Configurations ────────────────────────────────────────────┤
Phase 2: Persistence (DbContext, EntityConfigs) ────────────────────┤
Phase 3: Authentication/JWT (JwtOptions, IJwtProvider, JwtProvider) ┤
Phase 4: Auth Filters (Permission-based authz) ─────────────────────┤
Phase 5: Errors (UserErrors) ──────────► requires Phase 0           │
Phase 6: Contracts + Validators ────────► requires Phase 0          │
Phase 7: Mapping ───────────────────────► requires Phase 6          │
Phase 8: Email Templates & Helpers ─────────────────────────────────┤
Phase 9: Services ──────────────────────► requires Phases 0-8       │
Phase 10: Extensions ───────────────────► requires Phases 0,3,6     │
Phase 11: Controller/Endpoints ─────────► requires Phase 9          │
Phase 12: Wiring & Cleanup ────────────► requires ALL above         │
```

---
The architecture follows a Service Layer pattern — controllers call services, and services own all data access logic through UserManager and ApplicationDbContext. No IRepository<T> or IUnitOfWork abstractions exist
## ✅ Ready to implement once questions above are answered.
