# 🧪 Auth Module Testing Guide

## Prerequisites

1. **Run the API**: `dotnet run` from `Almny.Api/`
2. **Open Swagger**: `https://localhost:7220/swagger`
3. **Gmail SMTP configured**: App Password stored in user-secrets

---

## Endpoint Overview

| # | Endpoint | Method | Auth | Rate Limit |
|---|----------|--------|------|------------|
| 1 | `/api/auth/register` | POST | ❌ | Authentication |
| 2 | `/api/auth/confirm-email` | GET | ❌ | Authentication |
| 3 | `/api/auth/resend-confirmation` | POST | ❌ | Authentication |
| 4 | `/api/auth/login` | POST | ❌ | Authentication |
| 5 | `/api/auth/refresh` | POST | ❌ | Authentication |
| 6 | `/api/auth/revoke-refresh-token` | POST | ✅ | Api |
| 7 | `/api/auth/forgot-password` | POST | ❌ | Authentication |
| 8 | `/api/auth/reset-password` | GET | ❌ | — |
| 9 | `/api/auth/reset-password` | POST | ❌ | Authentication |

---

## Test Flow (Step by Step)

### 1️⃣ Register a New User

**Endpoint:** `POST /api/auth/register`

```json
{
  "fullName": "Mohamed Jehaad",
  "email": "your-email@gmail.com",
  "password": "Test@12345",
  "confirmPassword": "Test@12345"
}
```

**✅ Expected (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1...",
  "expiresIn": 3600,
  "refreshToken": "abc123..."
}
```

📧 **Check your inbox** — you should receive a confirmation email.

> ⚠️ Save the `token` and `refreshToken` values for later steps.

**❌ Test Validation — missing fields:**
```json
{
  "fullName": "",
  "email": "invalid",
  "password": "123",
  "confirmPassword": "456"
}
```
Expected: `400 Bad Request` with validation errors.

**❌ Test Duplicate Email — register same email again:**

Expected: `409 Conflict` — `"A user with this email already exists."`

---

### 2️⃣ Confirm Email

**Action:** Click the **"Confirm Email"** button in the email you received.

**✅ Expected:** Browser shows a page with:
- ✅ icon
- "Email Confirmed"
- "Your email has been confirmed successfully. You can now log in."

**❌ Test Expired/Invalid Link:** Click the same link again.

Expected: Browser shows:
- ❌ icon
- "Confirmation Failed"
- "The confirmation link is invalid or has expired."

---

### 3️⃣ Login

**Endpoint:** `POST /api/auth/login`

```json
{
  "email": "your-email@gmail.com",
  "password": "Test@12345"
}
```

**✅ Expected (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1...",
  "expiresIn": 3600,
  "refreshToken": "abc123..."
}
```

> 📋 Save the `token` and `refreshToken` — needed for steps 4 and 5.

**❌ Test Wrong Password:**
```json
{
  "email": "your-email@gmail.com",
  "password": "WrongPassword123"
}
```
Expected: `401 Unauthorized` — `"Invalid email or password."`

**❌ Test Unconfirmed Email:** Register a new user, don't confirm, try to login.

Expected: `401 Unauthorized` — `"Email has not been confirmed."`

---

### 4️⃣ Refresh Token

**Endpoint:** `POST /api/auth/refresh`

```json
{
  "refreshToken": "<paste refreshToken from login>"
}
```

**✅ Expected (200):** New `token` + `refreshToken`.

> The old refresh token is now revoked. Use the new one going forward.

**❌ Test Reuse Old Token:** Send the same refresh token again.

Expected: `401 Unauthorized` — `"The refresh token is invalid or expired."`

---

### 5️⃣ Revoke Refresh Token

> 🔒 **Requires Authentication**

**Setup:** In Swagger, click the 🔒 **Authorize** button → paste: `Bearer <your-token-from-login>`

**Endpoint:** `POST /api/auth/revoke-refresh-token`

```json
{
  "refreshToken": "<paste current refreshToken>"
}
```

**✅ Expected (200):**
```json
{
  "message": "Refresh token revoked."
}
```

**❌ Test Without Auth:** Remove the Bearer token and try again.

Expected: `401 Unauthorized`

**❌ Test Already Revoked Token:** Send the same revoked token.

Expected: `401 Unauthorized` — `"The refresh token is invalid or expired."`

---

### 6️⃣ Resend Confirmation Email

> Use this if you registered but didn't receive or lost the confirmation email.

**Endpoint:** `POST /api/auth/resend-confirmation`

```json
{
  "email": "your-email@gmail.com"
}
```

**✅ Expected (200):**
```json
{
  "message": "If the email exists and is unconfirmed, a confirmation email has been sent."
}
```

📧 Check inbox for new confirmation email.

**Security Note:** Returns the same success message whether the email exists or not (prevents user enumeration).

---

### 7️⃣ Forgot Password

**Endpoint:** `POST /api/auth/forgot-password`

```json
{
  "email": "your-email@gmail.com"
}
```

**✅ Expected (200):**
```json
{
  "message": "If the email exists, a reset link has been sent."
}
```

📧 Check inbox for reset password email.

**Security Note:** Same response whether email exists or not (prevents user enumeration).

---

### 8️⃣ Reset Password

**Action:** Click the **"Reset Password"** button in the email.

**✅ Expected:** Browser opens a form page:
1. Enter new password
2. Confirm new password
3. Click "Reset Password"
4. ✅ Shows: "Password has been reset successfully!"

**❌ Test Mismatched Passwords:** Enter different passwords.

Expected: Client-side error — "Passwords do not match."

**❌ Test Reuse Link:** Click the email link again and try to reset.

Expected: "Failed to reset password." (token already consumed)

---

### 9️⃣ Login with New Password

**Endpoint:** `POST /api/auth/login`

```json
{
  "email": "your-email@gmail.com",
  "password": "YourNewPassword123!"
}
```

**✅ Expected (200):** New token + refresh token.

**❌ Test Old Password:**
```json
{
  "email": "your-email@gmail.com",
  "password": "Test@12345"
}
```
Expected: `401 Unauthorized` — old password no longer works.

---

## Rate Limiting Tests

All auth endpoints use the `Authentication` rate limit policy: **5 requests per 60 seconds**.

**To test:**
1. Call any auth endpoint 5 times rapidly
2. On the 6th call, expect: `429 Too Many Requests`

```json
{
  "error": "Too many requests. Please try again later.",
  "retryAfterSeconds": 60
}
```

---

## Password Validation Rules

All password fields must meet:

| Rule | Requirement |
|------|-------------|
| Minimum length | 8 characters |
| Uppercase | At least one `[A-Z]` |
| Lowercase | At least one `[a-z]` |
| Digit | At least one `[0-9]` |

**Valid examples:** `Test@12345`, `MyPass99!`, `SecureP4ss`

**Invalid examples:** `test1234` (no uppercase), `TESTPASS` (no digit/lowercase), `Short1` (too short)

---

## Quick Reference: Error Responses

| Scenario | Status | Error Code |
|----------|--------|------------|
| Validation failure | 400 | Field-specific errors |
| Invalid credentials | 401 | `User.InvalidCredentials` |
| Email not confirmed | 401 | `User.EmailNotConfirmed` |
| Invalid/expired refresh token | 401 | `User.InvalidRefreshToken` |
| Account locked out | 401 | `User.LockedOut` |
| Invalid reset token | 400 | `User.InvalidResetToken` |
| Duplicate email | 409 | `User.DuplicateEmail` |
| Rate limit exceeded | 429 | `Too many requests` |
