using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS66_WebApplication1.Models
{
    public class Amphur
    {
        [Key, Required, MaxLength(4)]
        public string Amphur_Id { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Amphur_Name { get; set; } = string.Empty;

        [Required]
        public string Province_Id { get; set; } = string.Empty;

        [ForeignKey(nameof(Province_Id))]
        public Province? Province { get; set; }

        // ให้ลบบรรทัด Province_Name เดิมออก หรือใส่ [NotMapped] แบบนี้:
        [NotMapped]
        public string Province_Name { get; set; } = string.Empty;
        public virtual ICollection<District> Districts { get; set; }
    }
}