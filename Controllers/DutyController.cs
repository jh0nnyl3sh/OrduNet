using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;

namespace OrduNet.Web.Controllers
{
    public class DutyController : Controller
    {
        private readonly OrduNetDbContext _context;

        public DutyController(OrduNetDbContext context)
        {
            _context = context;
        }

        // GET: /Duty
        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _context.DailyDuties.AsQueryable();

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var duties = await query
                .OrderByDescending(d => d.DutyDate)
                .ThenBy(d => d.DutyType)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(duties);
        }

        // GET: /Duty/Menu
        public async Task<IActionResult> Menu(int? year, int? month)
        {
            var targetDate = (year.HasValue && month.HasValue) 
                ? new DateTime(year.Value, month.Value, 1) 
                : DateTime.Today;

            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var menus = await _context.CafeteriaMenus
                .Where(m => m.Date >= startOfMonth && m.Date <= endOfMonth)
                .OrderBy(m => m.Date)
                .ToListAsync();

            // Eğer seçilen ayda menü yoksa ve kullanıcı Özel bir ay seçmediyse veritabanındaki mevcut kayıtlarç göster
            if (!menus.Any() && !year.HasValue && !month.HasValue)
            {
                menus = await _context.CafeteriaMenus
                    .OrderBy(m => m.Date)
                    .ToListAsync();

                if (menus.Any())
                {
                    targetDate = menus.First().Date;
                }
            }

            ViewBag.SelectedYear = targetDate.Year;
            ViewBag.SelectedMonth = targetDate.Month;
            ViewBag.MonthName = targetDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

            return View(menus);
        }
    }
}
