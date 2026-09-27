using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CS66_WebApplication1.Services
{
    // ส่งอีเมลแจ้งเตือนนัดหมายอัตโนมัติล่วงหน้า ก่อนไม่ต้องรอพนักงานกดปุ่มสะกิดลูกค้าเอง
    public class AppointmentReminderService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(IServiceScopeFactory scopeFactory, ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาดระหว่างส่งอีเมลแจ้งเตือนนัดหมายอัตโนมัติ");
                }

                try
                {
                    await AutoCancelUnconfirmedAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาดระหว่างยกเลิกคิวอัตโนมัติที่ลูกค้าไม่ยืนยัน");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // service กำลังหยุดทำงาน
                }
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var linkBuilder = scope.ServiceProvider.GetRequiredService<AppointmentEmailLinkBuilder>();

            var now = DateTime.Now;
            var windowEnd = now.Add(ReminderWindow);

            var dueAppointments = await context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => !a.IsReminderSent
                         && a.Appointment_Date >= now
                         && a.Appointment_Date <= windowEnd
                         && a.Status != "ยกเลิกแล้ว"
                         && a.Status != "เสร็จสิ้น"
                         && a.Status != "เสร็จแล้ว")
                .ToListAsync(stoppingToken);

            foreach (var appointment in dueAppointments)
            {
                if (appointment.Customer == null || string.IsNullOrWhiteSpace(appointment.Customer.Customer_Email))
                {
                    continue;
                }

                appointment.ConfirmToken = Guid.NewGuid().ToString("N");
                appointment.TokenExpiry = appointment.Appointment_Date.AddHours(-2);
                appointment.IsReminderSent = true;

                var (confirmUrl, declineUrl) = linkBuilder.Build(appointment.ConfirmToken);
                var html = AppointmentEmailTemplate.BuildReminderEmail(
                    appointment.Customer.Customer_Name,
                    appointment.Service?.Service_Name ?? "บริการทั่วไป",
                    appointment.Appointment_Date,
                    confirmUrl,
                    declineUrl);

                await emailSender.SendEmailAsync(
                    appointment.Customer.Customer_Email,
                    appointment.Customer.Customer_Name,
                    "แจ้งเตือนนัดหมาย - กรุณายืนยันคิวของท่าน",
                    html);
            }

            if (dueAppointments.Count > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("ส่งอีเมลแจ้งเตือนนัดหมายอัตโนมัติแล้ว {Count} รายการ", dueAppointments.Count);
            }
        }

        // 🌟 ยกเลิกคิวอัตโนมัติ: คิวที่ส่งอีเมลแจ้งเตือนไปแล้ว แต่ลูกค้าไม่กด "สะดวกมา/ไม่สะดวกมา" เลย
        // จนลิงก์หมดอายุ (2 ชม. ก่อนถึงเวลานัด) ถือว่าไม่ยืนยัน ให้ยกเลิกอัตโนมัติเพื่อคืนคิวให้คนอื่นจองแทน
        private async Task AutoCancelUnconfirmedAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.Now;

            var expiredUnconfirmed = await context.Appointments
                .Where(a => a.IsReminderSent
                         && !a.IsCustomerConfirmed
                         && a.ConfirmToken != null
                         && a.TokenExpiry != null
                         && a.TokenExpiry < now
                         && a.Status != "ยกเลิกแล้ว"
                         && a.Status != "เสร็จแล้ว"
                         && a.Status != "เสร็จสิ้น")
                .ToListAsync(stoppingToken);

            foreach (var appointment in expiredUnconfirmed)
            {
                appointment.Status = "ยกเลิกแล้ว";
                appointment.Employee_Id = null; // คืนคิวว่างให้ลูกค้าคนอื่นจองได้ทันที
                appointment.ConfirmToken = null;
                appointment.TokenExpiry = null;
            }

            if (expiredUnconfirmed.Count > 0)
            {
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("ยกเลิกคิวอัตโนมัติเพราะลูกค้าไม่ยืนยันภายในเวลาที่กำหนดแล้ว {Count} รายการ", expiredUnconfirmed.Count);
            }
        }
    }
}
