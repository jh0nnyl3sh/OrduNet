using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Unvan")]
        public string? Title { get; set; }

        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "Kurumsal E-Posta")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "Birim / Mahkeme")]
        public string? Unit { get; set; }

        [StringLength(50)]
        [Display(Name = "Dahili No")]
        public string? Phone { get; set; }

        [StringLength(50)]
        [Display(Name = "Oda No / Kat")]
        public string? RoomNumber { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Super Yönetici mi?")]
        public bool IsSuperAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<UserPermission> Permissions { get; set; } = new List<UserPermission>();
    }
}

