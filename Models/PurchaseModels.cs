using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    // ตาราง Supplier (ผู้จำหน่ายสินค้าเข้าร้าน)
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อซัพพลายเออร์")]
        [MaxLength(500)]
        [Display(Name = "ชื่อซัพพลายเออร์")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "ประเภท")]
        public string SupplierType { get; set; } = "Company";

        [MaxLength(20)]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string? Phone { get; set; }

        [MaxLength(255)]
        [Display(Name = "อีเมล")]
        public string? Email { get; set; }

        [MaxLength(1000)]
        [Display(Name = "ที่อยู่")]
        public string? Address { get; set; }

        [Display(Name = "เปิดใช้งาน")]
        public bool IsActive { get; set; } = true;
    }

    // ตารางหัวใบสั่งซื้อ
    public class PurchaseOrder
    {
        [Key]
        public int PO_Id { get; set; }

        public int SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        // วันที่สั่งซื้อ — ผู้ใช้กำหนดเองได้ (ไม่บังคับเป็นวันนี้เสมอไป)
        [Display(Name = "วันที่สั่งซื้อ")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Pending = รอตรวจรับ, PartiallyReceived = ได้ไม่ครบ, Completed = รับครบแล้ว
        public string Status { get; set; } = "Pending";

        // ยอดรวมของใบสั่งซื้อ คำนวณตอนสร้าง (Quantity * CostPrice รวมทุกรายการ)
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ยอดรวม")]
        public decimal GrandTotal { get; set; }

        public DateTime? ReceivedAt { get; set; }
    }

    // ตารางรายการสินค้าในใบสั่งซื้อ
    // สั่งได้ 2 แบบ: สินค้าขาย (Product = ล้อ/ยาง) หรือ อะไหล่ซ่อม (SparePart)
    // ItemType บอกว่าแถวนี้คือแบบไหน — ผูก FK แค่ฝั่งเดียว อีกฝั่งเป็น null
    public class PurchaseOrderDetail
    {
        [Key]
        public int DetailId { get; set; }

        public int PO_Id { get; set; }
        [ForeignKey(nameof(PO_Id))]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        // "Product" = ล้อ/ยาง, "SparePart" = อะไหล่
        [MaxLength(20)]
        public string ItemType { get; set; } = "Product";

        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product? Product { get; set; }

        public int? SparePartId { get; set; }
        [ForeignKey(nameof(SparePartId))]
        public virtual SparePart? SparePart { get; set; }

        // จำนวนที่สั่งซื้อ
        [Display(Name = "จำนวนที่สั่ง")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "ราคาต้นทุน")]
        public decimal CostPrice { get; set; }

        // สภาพสินค้า: มือ1 หรือ มือ2 — อะไหล่บังคับเป็นมือ1เสมอ (ควบคุมที่ Controller)
        [MaxLength(10)]
        [Display(Name = "สภาพสินค้า")]
        public string Condition { get; set; } = "มือ1";

        // จำนวนที่ตรวจนับได้จริงตอนรับของ (null = ยังไม่ได้ตรวจรับ)
        [Display(Name = "จำนวนที่ได้รับจริง")]
        public int? ReceivedQty { get; set; }

        [NotMapped]
        public decimal LineTotal => Quantity * CostPrice;

        // ชื่อแสดงผล ไม่สนว่าเป็น Product หรือ SparePart
        [NotMapped]
        public string ItemName => ItemType == "SparePart"
            ? (SparePart != null ? SparePart.Name : "(อะไหล่ถูกลบแล้ว)")
            : (Product != null ? Product.Product_Name : "(สินค้าถูกลบแล้ว)");
    }
}
