using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Birim / Mahkeme")]
        [Required(ErrorMessage = "Birim / Mahkeme bilgisi zorunludur.")]
        public string Unit { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Dahili No")]
        public string? Phone { get; set; }

        [StringLength(50)]
        [Display(Name = "Oda No / Kat")]
        public string? RoomNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleçmiyor.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
