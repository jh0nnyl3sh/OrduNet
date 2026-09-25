using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class EmergencyAlert
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Duyuru başlığı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Duyuru mesajı zorunludur.")]
        [StringLength(500)]
        [Display(Name = "Mesaj")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Uyarı Seviyesi")]
        public string AlertLevel { get; set; } = "danger"; // "danger" (Kırmızı), "warning" (Sarı), "info" (Mavi)

        [Display(Name = "Yayında mı?")]
        public bool IsActive { get; set; } = false;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }
}
