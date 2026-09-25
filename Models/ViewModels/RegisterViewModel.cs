using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı (Sicil / TC) zorunludur.")]
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2} karakter uzunlugünda olmalıdır.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Tekrar")]
        [Compare("Password", ErrorMessage = "Şifreler eşle�miyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Birim / Mahkeme")]
        [Required(ErrorMessage = "Birim / Mahkeme bilgişi zorunludur.")]
        public string Unit { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Dahili No")]
        public string? Phone { get; set; }

        [StringLength(50)]
        [Display(Name = "Oda No / Kat")]
        public string? RoomNumber { get; set; }
    }
}

