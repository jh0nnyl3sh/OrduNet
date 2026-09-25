using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class DailyDuty
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nöbet Tarihi")]
        public DateTime DutyDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(100)]
        [Display(Name = "Nöbet Türü")]
        public string DutyType { get; set; } = string.Empty; 
        // Örn: "Nöbet�i A��r Ceza Mahkemesi", "Nöbet�i Asliye Ceza Mahkemesi", "Nöbet�i Sulh Ceza Hâkimlişi", "Nöbet�i Cumhuriyet Savcışi", "Nöbet�i Noter"

        [Required]
        [StringLength(150)]
        [Display(Name = "Görevli Birim / Hâkim / Savcı")]
        public string DutyOfficerOrUnit { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "İletişim / Dahili")]
        public string? ContactInfo { get; set; }

        [StringLength(100)]
        [Display(Name = "Yer / Kalem")]
        public string? Location { get; set; }
    }
}

