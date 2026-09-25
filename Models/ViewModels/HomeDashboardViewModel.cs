using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Models.ViewModels
{
    public class HomeDashboardViewModel
    {
        public List<Announcement> Announcements { get; set; } = new List<Announcement>();
        public List<DailyDuty> DailyDuties { get; set; } = new List<DailyDuty>();
        public CafeteriaMenu? TodayMenu { get; set; }
        public int TotalPersonnel { get; set; }
        public int TotalUnits { get; set; }
        public EmergencyAlert? ActiveEmergencyAlert { get; set; }
    }
}
