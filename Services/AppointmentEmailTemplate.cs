using System;
using System.Net;

namespace CS66_WebApplication1.Services
{
    public static class AppointmentEmailTemplate
    {
        public static string BuildReminderEmail(string customerName, string serviceName, DateTime appointmentDate, string confirmUrl, string declineUrl)
        {
            string safeName = WebUtility.HtmlEncode(customerName);
            string safeService = WebUtility.HtmlEncode(serviceName);

            return $@"
<!DOCTYPE html>
<html lang=""th"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>แจ้งเตือนนัดหมาย - KAREEM WHEEL</title>
    <!-- Import Fonts -->
    <link href=""https://fonts.googleapis.com/css2?family=Montserrat:wght@400;600;700;800;900&family=Kanit:wght@300;400;500;600&display=swap"" rel=""stylesheet"">
    
    <style>
        /* สำหรับ Responsive บนมือถือ */
        @media only screen and (max-width: 520px) {{
            .main-card {{ width: 100% !important; border-radius: 0 !important; }}
            .btn-cell {{ display: block !important; width: 100% !important; padding: 6px 0 !important; }}
            .btn-container {{ padding: 0 !important; }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #F9FAFB; font-family: 'Kanit', 'Montserrat', -apple-system, BlinkMacSystemFont, sans-serif; -webkit-font-smoothing: antialiased;"">
    <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #F9FAFB; padding: 40px 10px;"">
        <tr>
            <td align=""center"">
                <!-- Main Card Container -->
                <table class=""main-card"" role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 520px; background-color: #FFFFFF; border-radius: 16px; border: 1px solid #EAEAEA; overflow: hidden; box-shadow: 0 8px 20px -4px rgba(47, 46, 46, 0.08);"">
                    
                    <!-- Orange Top Accent Bar -->
                    <tr>
                        <td style=""background-color: #FF9259; height: 6px; font-size: 0; line-height: 0;"">&nbsp;</td>
                    </tr>

                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #2F2E2E; padding: 36px 28px; text-align: center;"">
                            <div style=""display: inline-block; background-color: #FFF2EC; border-radius: 20px; padding: 4px 16px; margin-bottom: 16px;"">
                                <span style=""color: #FF9259; font-family: 'Montserrat', sans-serif; font-size: 11px; font-weight: 800; letter-spacing: 1.5px; text-transform: uppercase;"">Appointment Reminder</span>
                            </div>
                            <h1 style=""color: #FFFFFF; font-family: 'Montserrat', sans-serif; margin: 0; font-size: 26px; font-weight: 900; letter-spacing: 1.5px; text-transform: uppercase;"">KAREEM WHEEL</h1>
                            <p style=""color: #D1D5DB; margin: 8px 0 0 0; font-size: 14px; font-weight: 400;"">แจ้งเตือนนัดหมายเข้ารับบริการ</p>
                        </td>
                    </tr>

                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 36px 28px 24px 28px;"">
                            <p style=""color: #2F2E2E; margin: 0 0 8px 0; font-size: 18px; font-weight: 600;"">สวัสดีคุณ {safeName}</p>
                            <p style=""color: #8C8A89; margin: 0 0 28px 0; font-size: 15px; line-height: 1.6; font-weight: 300;"">
                                ขอแจ้งเตือนกำหนดการนัดหมายเข้ารับบริการของคุณ โปรดตรวจสอบรายละเอียดและกด ยืนยัน/ปฏิเสธ การเดินทางด้านล่างครับ
                            </p>

                            <!-- Service Info Box (ใช้โทนสีส้มอ่อนที่ให้มา) -->
                            <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""background-color: #FFF2EC; border: 1px solid #FF9259; border-radius: 12px; margin-bottom: 28px;"">
                                <tr>
                                    <td style=""padding: 24px;"">
                                        <div style=""font-size: 12px; color: #FF7B39; font-weight: 600; margin-bottom: 4px;"">รายการบริการ</div>
                                        <div style=""font-size: 18px; font-weight: 600; color: #2F2E2E; margin-bottom: 20px;"">{safeService}</div>
                                        
                                        <!-- Split Info Grid -->
                                        <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" style=""border-top: 1px dashed #FF9259; padding-top: 16px;"">
                                            <tr>
                                                <td width=""50%"" style=""vertical-align: top;"">
                                                    <div style=""font-size: 12px; color: #8C8A89; font-weight: 500;"">วันที่</div>
                                                    <div style=""font-size: 16px; font-weight: 600; color: #2F2E2E; margin-top: 4px;"">{appointmentDate:dd MMM yyyy}</div>
                                                </td>
                                                <td width=""50%"" style=""vertical-align: top; border-left: 1px solid #FFD3C0; padding-left: 16px;"">
                                                    <div style=""font-size: 12px; color: #8C8A89; font-weight: 500;"">เวลา</div>
                                                    <div style=""font-size: 16px; font-weight: 600; color: #2F2E2E; margin-top: 4px;"">{appointmentDate:HH:mm} น.</div>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""color: #2F2E2E; margin: 0 0 20px 0; font-size: 14px; text-align: center; font-weight: 500;"">
                                กรุณากดเลือกสถานะเพื่อจัดเตรียมคิวบริการ
                            </p>

                            <!-- Action Buttons -->
                            <table role=""presentation"" width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"">
                                <tr>
                                    <td class=""btn-cell"" width=""50%"" style=""padding-right: 8px;"">
                                        <a href=""{confirmUrl}"" style=""background-color: #43C97A; color: #FFFFFF; text-decoration: none; padding: 14px 0; border-radius: 8px; font-weight: 500; font-size: 15px; display: block; width: 100%; text-align: center;"">✓ สะดวกมา</a>
                                    </td>
                                    <td class=""btn-cell"" width=""50%"" style=""padding-left: 8px;"">
                                        <a href=""{declineUrl}"" style=""background-color: #F9FAFB; color: #2F2E2E; border: 1px solid #D1D5DB; text-decoration: none; padding: 13px 0; border-radius: 8px; font-weight: 500; font-size: 15px; display: block; width: 100%; text-align: center;"">✕ ไม่สะดวกมา</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #F9FAFB; padding: 24px 28px; border-top: 1px solid #EAEAEA; text-align: center;"">
                            <p style=""color: #8C8A89; font-size: 13px; margin: 0; line-height: 1.6; font-weight: 300;"">
                                📌 <strong style=""color: #2F2E2E; font-weight: 500;"">หมายเหตุ:</strong> ลิงก์ยืนยันจะหมดอายุก่อนถึงเวลานัดหมาย 2 ชั่วโมง<br>
                                หากไม่ได้ตอบกลับ ทางร้านอาจติดต่อเพื่อยืนยันคิวอีกครั้ง
                            </p>
                        </td>
                    </tr>

                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}