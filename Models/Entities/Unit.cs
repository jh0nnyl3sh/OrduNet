using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class Unit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Birim adı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Birim / Mahkeme Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategöri zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kategöri")]
        public string Category { get; set; } = "Genel"; 
        // Örn: "Cumhuriyet Bağsavcılççç", "Ceza Mahkemeleri", "Hukuk Mahkemeleri", "İcra & İflas", "İdari Birimler", "Komisyon & Yönetim"

        [Display(Name = "Sıralama")]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Konum / Blok / Kat")]
        public string? Location { get; set; }

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Personnel> PersonnelList { get; set; } = new List<Personnel>();
    }
}

