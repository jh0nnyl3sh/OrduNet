using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Models.ViewModels
{
    public class DirectoryViewModel
    {
        public string? SearchTerm { get; set; }
        public string? SelectedCategory { get; set; }
        public int? SelectedUnitId { get; set; }

        public List<Personnel> PersonnelList { get; set; } = new List<Personnel>();
        public List<Unit> Units { get; set; } = new List<Unit>();
        public List<string> Categories { get; set; } = new List<string>();

        public int TotalPersonnelCount { get; set; }
        public int TotalUnitCount { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PersonnelDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string InternalNumber { get; set; } = string.Empty;
        public string? InternalNumber2 { get; set; }
        public string? RoomNumber { get; set; }
        public string? Floor { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
    }
}
