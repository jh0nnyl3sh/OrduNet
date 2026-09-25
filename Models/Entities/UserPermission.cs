using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrduNet.Web.Models.Entities
{
    public class UserPermission
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }

        [Required]
        [StringLength(50)]
        public string ModuleKey { get; set; } = string.Empty;
        // Modül Anahtarlar�: "Directory", "Announcements", "Cafeteria", "Duties", "IssueTracker", "UserManagement"

        public bool CanManage { get; set; } = true;
    }

    public static class SystemModules
    {
        public const string Directory = "Directory";
        public const string Announcements = "Announcements";
        public const string Cafeteria = "Cafeteria";
        public const string Duties = "Duties";
        public const string IssueTracker = "IssueTracker";
        public const string UserManagement = "UserManagement";

        public static readonly Dictionary<string, string> ModuleNames = new()
        {
            { Directory, "Telefon Rehberi Yönetimi" },
            { Announcements, "Duyurular & Haberler" },
            { Cafeteria, "Yemek Listesi Yönetimi" },
            { Duties, "Nöbet Çizelgesi Yönetimi" },
            { IssueTracker, "Arıza Takip (Helpdesk)" }
        };
    }
}

