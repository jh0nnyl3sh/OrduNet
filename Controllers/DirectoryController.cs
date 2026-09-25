using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Services;

namespace OrduNet.Web.Controllers
{
    public class DirectoryController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;

        public DirectoryController(OrduNetDbContext context, IExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        // GET: /Directory
        public async Task<IActionResult> Index(string? search, string? category, int? unitId, int page = 1)
        {
            var units = await _context.Units
                .Where(u => u.IsActive)
                .OrderBy(u => u.DisplayOrder)
                .ThenBy(u => u.Name)
                .ToListAsync();

            var categories = await _context.Units
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Category))
                .Select(u => u.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var query = _context.Personnels
                .Include(p => p.Unit)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(p =>
                    p.FirstName.Contains(s) ||
                    p.LastName.Contains(s) ||
                    p.Title.Contains(s) ||
                    p.InternalNumber.Contains(s) ||
                    (p.RoomNumber != null && p.RoomNumber.Contains(s)) ||
                    (p.Unit != null && p.Unit.Name.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
            {
                query = query.Where(p => p.Unit != null && p.Unit.Category == category);
            }

            if (unitId.HasValue && unitId.Value > 0)
            {
                query = query.Where(p => p.UnitId == unitId.Value);
            }

            int pageSize = 10; // Kullanıcı isteği üzerine sayfa başı 10 kişi gösteriyoruz
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var personnelList = await query
                .OrderBy(p => p.Unit != null ? p.Unit.Category : "")
                .ThenBy(p => p.Unit != null ? p.Unit.DisplayOrder : 0)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new DirectoryViewModel
            {
                SearchTerm = search,
                SelectedCategory = category ?? "Tümü",
                SelectedUnitId = unitId,
                PersonnelList = personnelList,
                Units = units,
                Categories = categories,
                TotalPersonnelCount = await _context.Personnels.CountAsync(p => p.IsActive),
                TotalUnitCount = units.Count,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: /Directory/Search (AJAX Instant Search API)
        [HttpGet]
        public async Task<IActionResult> Search(string? q, string? category, int? unitId)
        {
            var query = _context.Personnels
                .Include(p => p.Unit)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                query = query.Where(p =>
                    p.FirstName.Contains(s) ||
                    p.LastName.Contains(s) ||
                    p.Title.Contains(s) ||
                    p.InternalNumber.Contains(s) ||
                    (p.InternalNumber2 != null && p.InternalNumber2.Contains(s)) ||
                    (p.RoomNumber != null && p.RoomNumber.Contains(s)) ||
                    (p.Floor != null && p.Floor.Contains(s)) ||
                    (p.Description != null && p.Description.Contains(s)) ||
                    (p.Unit != null && p.Unit.Name.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
            {
                query = query.Where(p => p.Unit != null && p.Unit.Category == category);
            }

            if (unitId.HasValue && unitId.Value > 0)
            {
                query = query.Where(p => p.UnitId == unitId.Value);
            }

            var results = await query
                .OrderBy(p => p.Unit != null ? p.Unit.Category : "")
                .ThenBy(p => p.Unit != null ? p.Unit.DisplayOrder : 0)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.FirstName)
                .Select(p => new PersonnelDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Title = p.Title,
                    UnitName = p.Unit != null ? p.Unit.Name : "-",
                    Category = p.Unit != null ? p.Unit.Category : "-",
                    InternalNumber = p.InternalNumber,
                    InternalNumber2 = p.InternalNumber2,
                    RoomNumber = p.RoomNumber,
                    Floor = p.Floor,
                    Email = p.Email,
                    Description = p.Description
                })
                .ToListAsync();

            return Json(new { count = results.Count, data = results });
        }

        // GET: /Directory/Print (A4 Yazdırma Çıktısı)
        public async Task<IActionResult> Print(string? category)
        {
            var query = _context.Personnels
                .Include(p => p.Unit)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "Tümü")
            {
                query = query.Where(p => p.Unit != null && p.Unit.Category == category);
            }

            var list = await query
                .OrderBy(p => p.Unit != null ? p.Unit.Category : "")
                .ThenBy(p => p.Unit != null ? p.Unit.DisplayOrder : 0)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            ViewBag.SelectedCategory = category ?? "Tüm Adliye";
            return View(list);
        }

        // GET: /Directory/ExportExcel
        public async Task<IActionResult> ExportExcel()
        {
            var list = await _context.Personnels
                .Include(p => p.Unit)
                .Where(p => p.IsActive)
                .ToListAsync();

            var fileBytes = _excelService.ExportDirectoryToExcel(list);
            var fileName = $"Ordu_Adliyesi_Rehber_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
