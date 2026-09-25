using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class TicketCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Aktif mi?")]
        public bool IsActive { get; set; } = true;
    }
}
