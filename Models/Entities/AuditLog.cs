using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // "Ekleme", "Güncelleme", "Silme", "Giriş", "Yedekleme"

        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
