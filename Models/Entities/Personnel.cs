using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrduNet.Web.Models.Entities
{
    public class Personnel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [Required(ErrorMessage = "Unvan zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Unvan")]
        public string Title { get; set; } = string.Empty; 
        // Örn: "Mahkeme Bağkanç", "Cumhuriyet Savcışi", "Yazı İşleri Müdürç", "Zabıt Kâtibi", "Mçbağir", "Bilgisayar çiletmeni"

        [Display(Name = "Bağlı Olduğu Birim")]
        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public virtual Unit? Unit { get; set; }

        [Required(ErrorMessage = "Dahili telefon numarası zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Dahili Numara")]
        public string InternalNumber { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "İkinci Dahili (Varsa)")]
        public string? InternalNumber2 { get; set; }

        [StringLength(50)]
        [Display(Name = "Oda Numarası")]
        public string? RoomNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Kat")]
        public string? Floor { get; set; } // Örn: "Zemin Kat", "1. Kat", "2. Kat", "3. Kat"

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "Kurumsal E-Posta")]
        public string? Email { get; set; }

        [StringLength(250)]
        [Display(Name = "Açıklama / Görev")]
        public string? Description { get; set; }

        [Display(Name = "Sıralama")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;
    }
}

