namespace CS66_WebApplication1.Models
{
    // ใช้สำหรับรับค่าข้อมูลจากหน้าจอสร้างใบสั่งซื้อ
    public class CreatePOViewModel
    {
        public int SupplierId { get; set; }

        // วันที่สั่งซื้อ — ผู้ใช้กำหนดเอง
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public List<POItemDto> Items { get; set; } = new List<POItemDto>();
    }

    // ข้อมูลย่อยของแต่ละบรรทัดสินค้าที่สั่ง
    public class POItemDto
    {
        // "Product" (ล้อ/ยาง) หรือ "SparePart" (อะไหล่)
        public string ItemType { get; set; } = "Product";

        public int? ProductId { get; set; }
        public int? SparePartId { get; set; }

        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }

        // มือ1 / มือ2 — ฝั่ง Controller จะบังคับเป็นมือ1 เสมอถ้า ItemType = SparePart
        public string Condition { get; set; } = "มือ1";
    }
}
