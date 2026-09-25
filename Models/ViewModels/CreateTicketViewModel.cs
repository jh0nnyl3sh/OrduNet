using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.ViewModels
{
    public class CreateTicketViewModel
    {
        [Required(ErrorMessage = "Lütfen arıza konusunu kısaca belirtiniz.")]
        [StringLength(150)]
        [Display(Name = "Arıza Konusu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lütfen arıza detayını yazın�z.")]
        [StringLength(1000)]
        [Display(Name = "Arıza Açıklamas�")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategöri seçimi zorunludur.")]
        [Display(Name = "Arıza Kategörişi")]
        public string Category { get; set; } = "Donanım / PC";

        [Required(ErrorMessage = "Adın�z ve soyadın�z zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Talep Eden Personel")]
        public string RequesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bulunduğunuz birim / mahkeme zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Birim / Mahkeme")]
        public string RequesterUnit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Size ulağabilece�imiz dahili numara zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Dahili Numara")]
        public string RequesterPhone { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Oda Numarası / Kat")]
        public string? RoomNumber { get; set; }
    }
}

