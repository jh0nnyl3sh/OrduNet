using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Bağlık zorunludur.")]
        [StringLength(250)]
        [Display(Name = "Bağlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Özet zorunludur.")]
        [StringLength(500)]
        [Display(Name = "Özet Metin")]
        public string Summary { get; set; } = string.Empty;

        [Display(Name = "Detay içerik")]
        public string? Content { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Kategöri")]
        public string Category { get; set; } = "DUYURU"; // DUYURU, HABER, ANLAçMA, ETKİNLİK

        [StringLength(50)]
        public string BadgeClass { get; set; } = "badge-primary"; // badge-danger, badge-primary, badge-success, badge-warning

        [StringLength(300)]
        [Display(Name = "Görsel Yolu")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Yayın Tarihi")]
        public DateTime PublishDate { get; set; } = DateTime.Now;

        [Display(Name = "Göruntülenme Sayişi")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Yayında mç?")]
        public bool IsActive { get; set; } = true;
    }
}

