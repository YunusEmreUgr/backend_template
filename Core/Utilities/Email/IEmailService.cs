namespace Core.Utilities.Email
{
    /// <summary>
    /// E-posta gönderme servisi soyutlama interface'i.
    /// 
    /// Bu interface sayesinde farklı email provider'ları (SMTP, SendGrid, Mailgun, vb.)
    /// kolayca değiştirilebilir.
    /// 
    /// Servis kayıt: services.AddScoped&lt;IEmailService, SmtpEmailService&gt;();
    /// 
    /// Kullanım alanları:
    ///   - Email doğrulama linki gönderme
    ///   - Şifre sıfırlama linki gönderme
    ///   - Kullanıcı bildirimleri
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Basit HTML email gönderir.</summary>
        Task SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Template tabanlı email gönderir.
        /// templateName ile email şablonu seçilir, model ile değişkenler doldurulur.
        /// TODO: Razor/Scriban/Handlebars gibi template engine entegre edilebilir.
        /// </summary>
        Task SendEmailWithTemplateAsync(string to, string subject, string templateName, object model);
    }
}
