using CS66_WebApplication1.Models;
using Microsoft.EntityFrameworkCore;

namespace CS66_WebApplication1.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ===== ตารางข้อมูลหลัก =====
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Amphur> Amphurs { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; } // เหลือบรรทัดเดียวที่ถูกต้องแล้ว
        public DbSet<SparePart> SpareParts { get; set; }
        public DbSet<SparePartRequisition> SparePartRequisitions { get; set; }
        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Banner> Banners { get; set; }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var lastOrder = await Orders
                .Where(o => o.OrderNumber != null)
                .OrderByDescending(o => o.Order_Id)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastOrder?.OrderNumber != null && lastOrder.OrderNumber.StartsWith("ORD"))
            {
                if (int.TryParse(lastOrder.OrderNumber.Substring(3), out int lastNum))
                    nextNum = lastNum + 1;
            }
            else
            {
                var maxId = await Orders.AnyAsync() ? await Orders.MaxAsync(o => o.Order_Id) : 0;
                nextNum = maxId + 1;
            }

            return $"ORD{nextNum:D4}";
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Configuration เดิม =====
            modelBuilder.Entity<Category>().HasKey(c => c.Category_Id);

            modelBuilder.Entity<Province>().HasKey(p => p.Province_Id);
            modelBuilder.Entity<Province>().Property(p => p.Province_Id).HasColumnType("nchar(10)");
            modelBuilder.Entity<Province>().Property(p => p.Province_Name).HasColumnType("nchar(100)");

            modelBuilder.Entity<Amphur>().HasKey(a => a.Amphur_Id);
            modelBuilder.Entity<Amphur>().Property(a => a.Province_Id).HasColumnType("nchar(10)");
            modelBuilder.Entity<Amphur>().HasOne(a => a.Province).WithMany(p => p.Amphurs).HasForeignKey(a => a.Province_Id);

            modelBuilder.Entity<Customer>().HasKey(c => c.Customer_Id);

            // ===== ส่วนเชื่อมโยงของ Appointment =====
            modelBuilder.Entity<Appointment>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.Service_Id);
        }
    }
}