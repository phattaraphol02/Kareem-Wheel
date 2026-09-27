using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class OrderDetail
    {
        [Key]
        public int OrderDetail_Id { get; set; }

        public int Order_Id { get; set; }

        public int Product_Id { get; set; }

        public int Qty { get; set; }

        public decimal Price { get; set; }

        public decimal SubTotal { get; set; }

        // =========================
        // RELATION
        // =========================

        [ForeignKey("Order_Id")]
        public Order? Order { get; set; }

        [ForeignKey("Product_Id")]
        public Product? Product { get; set; }
    }
}