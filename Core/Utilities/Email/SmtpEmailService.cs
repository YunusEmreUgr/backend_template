using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Core.Utilities.Email
{
    /// <summary>
    /// IEmailService'in .NET SMTP tabanlı implementasyonu.
    /// 
    /// appsettings.json konfigürasyonu:
    /// "EmailSettings": {
    ///   "SmtpHost": "smtp.gmail.com",
    ///   "SmtpPort": "587",
    ///   "SmtpUsername": "noreply@example.com",
    ///   "SmtpPassword": "your_app_password",
    ///   "FromEmail": "noreply@example.com",
    ///   "FromName": "My App"
    /// }
    /// 
    /// Gmail için: App Password oluşturun (2FA açık olmalı)
    /// Alternatif: SendGrid, Mailgun, AWS SES entegrasyonu için bu sınıfı değiştirin.
    /// 
    /// Servis kayıt: services.AddScoped&lt;IEmailService, SmtpEmailService&gt;();
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Konfigürasyondan SMTP ayarlarını oku
            var smtpHost = _configuration["EmailSettings:SmtpHost"]
                ?? throw new InvalidOperationException("EmailSettings:SmtpHost ayarı eksik.");
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"]
                ?? throw new InvalidOperationException("EmailSettings:SmtpUsername ayarı eksik.");
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"]
                ?? throw new InvalidOperationException("EmailSettings:SmtpPassword ayarı eksik.");
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
            var fromName = _configuration["EmailSettings:FromName"] ?? "App";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true // HTML email gönder
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }

        /// <inheritdoc/>
        public async Task SendEmailWithTemplateAsync(string to, string subject, string templateName, object model)
        {
            // TODO: Gerçek template engine entegrasyonu (Razor, Scriban, vb.)
            // Şu an için basit placeholder implementasyonu
            var body = $"<h1>{subject}</h1><p>Template: {templateName}</p>";
            await SendEmailAsync(to, subject, body);
        }
    }
}
