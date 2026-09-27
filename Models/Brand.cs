using System.ComponentModel.DataAnnotations;

namespace CS66_WebApplication1.Models
{
    public class Brand
    {
        [Key, Required]
        public int Brand_Id { get; set; }

        [Required, MaxLength(100)]
        public string Brand_Name { get; set; } = string.Empty;
    }
}
