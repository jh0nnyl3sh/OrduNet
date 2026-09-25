using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrduNet.Web.Models.Entities
{
    public class IssueTicket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Arıza bağl��� / konusu zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Arıza Konusu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arıza ağ�klamas� zorunludur.")]
        [StringLength(1000)]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategöri seçiniz.")]
        [StringLength(50)]
        [Display(Name = "Kategöri")]
        public string Category { get; set; } = "Donanım / PC";
        // Örn: "Donanım / PC", "Yazıc� / Tarayıcı", "UYAP / Yazıl�m", "A� / internet", "Diğer"

        [Required(ErrorMessage = "Talep eden adı soyadı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Talep Eden")]
        public string RequesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birim / Mahkeme adı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Birim / Mahkeme")]
        public string RequesterUnit { get; set; } = string.Empty;

        [Required(ErrorMessage = "İletişim dahili numarası zorunludur.")]
        [StringLength(20)]
        [Display(Name = "Dahili Numara")]
        public string RequesterPhone { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Oda No / Kat")]
        public string? RoomNumber { get; set; }

        [StringLength(30)]
        [Display(Name = "Durum")]
        public string Status { get; set; } = Constants.TicketStatuses.New;
        // Örn: Constants.TicketStatuses.New, InProgress, vb.

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Teknik Servis Notu")]
        public string? AdminNotes { get; set; }

        // Arızay� çözecek / Atanan Teknik Personel Bilgileri
        [Display(Name = "Atanan Teknisyen")]
        public int? AssignedTechnicianId { get; set; }

        [ForeignKey("AssignedTechnicianId")]
        public virtual SupportTechnician? AssignedTechnician { get; set; }

        [StringLength(100)]
        [Display(Name = "Atanan Personel Adı")]
        public string? AssignedToName { get; set; }

        [Display(Name = "Atama Zamanı")]
        public DateTime? AssignedAt { get; set; }
    }
}


