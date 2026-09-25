using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class SupportTechnician
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Teknisyen adı soyadı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Unvan / Görev")]
        public string? Title { get; set; }

        [StringLength(50)]
        [Display(Name = "İletişim / Dahili")]
        public string? Phone { get; set; }

        [Display(Name = "Görevde mi?")]
        public bool IsAvailable { get; set; } = true; // true: Görevde, false: izinli

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true; // Sistemde aktif Personel mi

        [StringLength(500)]
        [Display(Name = "Uzmanlık Alanları")]
        public string? Specialties { get; set; } // Virgülle ayrçlmçç kategöri işimleri veya ID'leri

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

