using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class SiteSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? GroupName { get; set; }
    }
}
