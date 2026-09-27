using Microsoft.Extensions.Configuration;

namespace CS66_WebApplication1.Services
{
    // สร้างลิงก์ "สะดวกมา" / "ไม่สะดวกมา" ที่แนบไปกับอีเมลแจ้งเตือนนัดหมาย
    public class AppointmentEmailLinkBuilder
    {
        private readonly string _baseUrl;

        public AppointmentEmailLinkBuilder(IConfiguration configuration)
        {
            _baseUrl = (configuration["AppBaseUrl"] ?? "https://localhost:7079").TrimEnd('/');
        }

        public (string ConfirmUrl, string DeclineUrl) Build(string token)
        {
            return (
                $"{_baseUrl}/AppointmentResponse/Respond?token={token}&response=confirm",
                $"{_baseUrl}/AppointmentResponse/Respond?token={token}&response=decline"
            );
        }
    }
}
