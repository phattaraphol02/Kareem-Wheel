using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Order_Id { get; set; }

        // ================================
        // CUSTOMER
        // ================================
        [Required]
        public int Customer_Id { get; set; }

        [ForeignKey(nameof(Customer_Id))]
        public Customer? Customer { get; set; }

        // ================================
        // ORDER INFO
        // ================================
        [MaxLength(20)]
        public string? OrderNumber { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // ================================
        // 🚚 NEW: DELIVERY & APPOINTMENT
        // ================================
        [Required]
        [MaxLength(20)]
        public string DeliveryMethod { get; set; } = "Delivery"; // "Delivery" หรือ "Pickup"

        public DateTime? AppointmentDate { get; set; } // วัน/เวลาที่ลูกค้าจะมารับสินค้า

        // ================================
        // 💰 PAYMENT
        // ================================
        [MaxLength(255)]
        public string? SlipImage { get; set; }

        public DateTime? PaidAt { get; set; }

        // ================================
        // SHIPPING
        // ================================
        [MaxLength(100)]
        public string? ShippingCompany { get; set; }

        [MaxLength(50)]
        public string? TrackingNo { get; set; }

        [Required]
        [MaxLength(50)]
        public string ShippingStatus { get; set; } = "Pending";

        public DateTime? ShippedAt { get; set; }

        // กันตัดสต็อกซ้ำ เมื่อออเดอร์ถูกยืนยันจากหลายทาง (ยืนยันสลิป / อนุมัติการชำระเงิน)
        public bool StockDeducted { get; set; } = false;

        // ================================
        // ORDER DETAILS
        // ================================
        public List<OrderDetail>? OrderDetails { get; set; }

        [NotMapped]
        public decimal TotalAmount
        {
            get
            {
                if (OrderDetails == null || OrderDetails.Count == 0)
                    return GrandTotal;
                return OrderDetails.Sum(x => x.Price * x.Qty);
            }
        }
    }
}