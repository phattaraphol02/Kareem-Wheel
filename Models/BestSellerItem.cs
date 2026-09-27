namespace CS66_WebApplication1.Models
{
    // สินค้า 1 รายการพร้อมยอดขายรวม ใช้ในหน้าจัดอันดับสินค้าขายดี
    public class BestSellerItem
    {
        public Product Product { get; set; } = null!;
        public int SoldQty { get; set; }
    }
}
