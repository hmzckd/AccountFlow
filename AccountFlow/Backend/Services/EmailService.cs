using AccountFlow.Backend.Models;
using AccountFlow.Backend.Options;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace AccountFlow.Backend.Services
{

    // Manages email transmission operations using SMTP configuration
    public class EmailService(IOptions<GmailOptions> gmailOptions) : IEmailService
    {
        private readonly GmailOptions _gmailOptions = gmailOptions.Value;


        // Asynchronously sends an email using the configured SMTP settings and credentials
        public async Task SendMailAsync(EmailDto request)
        {
            using MailMessage mail = new()
            {
                From = new MailAddress(_gmailOptions.Email),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = true
            };
            mail.To.Add(request.To);

            using var smtp = new System.Net.Mail.SmtpClient();
            smtp.Host = _gmailOptions.Host;
            smtp.Port = _gmailOptions.SmtpPort;
            smtp.Credentials = new System.Net.NetworkCredential(_gmailOptions.Email, _gmailOptions.Password);
            smtp.EnableSsl = true;

            await smtp.SendMailAsync(mail);
        }
    }
}
