using System.Threading.Tasks;

namespace CS66_WebApplication1.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
    }
}
