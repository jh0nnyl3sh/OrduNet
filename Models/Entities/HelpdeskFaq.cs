using System.ComponentModel.DataAnnotations;

namespace OrduNet.Web.Models.Entities
{
    public class HelpdeskFaq
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [StringLength(50)]
        public string IconClass { get; set; } = "bi-question-circle";

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
