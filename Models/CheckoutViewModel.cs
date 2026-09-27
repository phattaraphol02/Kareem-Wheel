using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class CheckoutViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Address { get; set; }

        public decimal TotalPrice { get; set; }
    }
}