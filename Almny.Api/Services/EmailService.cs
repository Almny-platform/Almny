using System.Net;
using System.Net.Mail;

namespace Almny.Api.Services;

public class EmailService : IEmailService
{
    private readonly MailConfig _mailConfig;

    public EmailService(IOptions<MailConfig> mailConfig)
    {
        _mailConfig = mailConfig.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_mailConfig.SenderEmail, _mailConfig.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        using var client = new SmtpClient(_mailConfig.SmtpServer, _mailConfig.Port)
        {
            Credentials = new NetworkCredential(_mailConfig.Username, _mailConfig.Password),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
    }
}
