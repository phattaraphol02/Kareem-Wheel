using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail) || string.IsNullOrWhiteSpace(_settings.SenderPassword))
            {
                _logger.LogWarning("ยังไม่ได้ตั้งค่า Email:SenderEmail / Email:SenderPassword ใน appsettings จึงข้ามการส่งอีเมลถึง {ToEmail}", toEmail);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SenderEmail, _settings.SenderPassword);
                await client.SendAsync(message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "ส่งอีเมลถึง {ToEmail} ไม่สำเร็จ", toEmail);
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
