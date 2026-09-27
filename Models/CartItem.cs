using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class CartItem
    {
        [Key]
        public int CartItem_Id { get; set; }

        public int Product_Id { get; set; }

        public string Product_Name { get; set; } = "";

        public string ImageFileName { get; set; } = "";

        public decimal Price { get; set; }

        public int Qty { get; set; }
    }
}