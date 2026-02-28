using Almny.Api.Helpers;

namespace Almny.Api.Services.EmailTemplates;

public class EmailTemplateBuilder
{
    public static string BuildConfirmationEmail(string fullName, string confirmationLink)
    {
        return EmailBodyBuilder.Build("confirmation-email.html", new Dictionary<string, string>
        {
            ["FullName"] = fullName,
            ["ConfirmationLink"] = confirmationLink
        });
    }

    public static string BuildResetPasswordEmail(string fullName, string resetLink)
    {
        return EmailBodyBuilder.Build("reset-password.html", new Dictionary<string, string>
        {
            ["FullName"] = fullName,
            ["ResetLink"] = resetLink
        });
    }
}
