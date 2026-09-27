using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Payment
    {
        [Key]
        public int Payment_Id { get; set; }

        public int Order_Id { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; }

        public string? SlipFileName { get; set; }

        public string? Status { get; set; }

        public DateTime? VerifiedAt { get; set; }

        // relation
        [ForeignKey("Order_Id")]
        public Order? Order { get; set; }
    }
}