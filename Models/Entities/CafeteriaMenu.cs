using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class CafeteriaMenu
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tarih")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [StringLength(50)]
        [Display(Name = "Gün")]
        public string DayName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Çorba")]
        public string Soup { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Ana Yemek")]
        public string MainDish { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Yardımcı Yemek")]
        public string SideDish { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Tatlı / Meyve / Salata")]
        public string DessertOrSalad { get; set; } = string.Empty;

        [Display(Name = "Toplam Kalori")]
        public int? Calories { get; set; }

        [Display(Name = "Tüm Yemek Kalemleri")]
        public string? FullMenuText { get; set; }
    }
}


