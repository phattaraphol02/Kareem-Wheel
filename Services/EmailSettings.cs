namespace CS66_WebApplication1.Services
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "";
        public string SenderPassword { get; set; } = "";
        public string SenderName { get; set; } = "";
    }
}
