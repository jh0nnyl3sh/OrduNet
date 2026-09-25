
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;
using OrduNet.Web.Models.ViewModels;
using OrduNet.Web.Models.Constants;
using OrduNet.Web.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace OrduNet.Web.Controllers
{
    [Authorize]
    public class CafeteriaController : Controller
    {
        private readonly OrduNetDbContext _context;
        private readonly IExcelService _excelService;
        private readonly IAuditService _auditService;

        public CafeteriaController(OrduNetDbContext context, IExcelService excelService, IAuditService auditService)
        {
            _context = context;
            _excelService = excelService;
            _auditService = auditService;
        }

        private void LogAudit(string action, string entityName, string? entityId, string details)
        {
            _auditService.LogAudit(User.Identity?.Name ?? "Bilinmeyen", action, entityName, entityId, details, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        // 5.2. YEMEK LİSTESİ (KAFETERYA) & AYLIK EXCEL YÖNETİMİ
        // ==========================================
        public async Task<IActionResult> Index(int? year, int? month)
        {
            if (!User.HasModulePermission(SystemModules.Cafeteria))
                return RedirectToAction("AccessDenied", "Account");

            var targetDate = (year.HasValue && month.HasValue)
                ? new DateTime(year.Value, month.Value, 1)
                : DateTime.Today;

            var startOfMonth = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var menus = await _context.CafeteriaMenus
                .Where(m => m.Date >= startOfMonth && m.Date <= endOfMonth)
                .OrderBy(m => m.Date)
                .ToListAsync();

            ViewBag.SelectedYear = targetDate.Year;
            ViewBag.SelectedMonth = targetDate.Month;
            ViewBag.MonthName = targetDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            ViewBag.TotalMonthDays = menus.Count;

            return View(menus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCafeteriaExcel(IFormFile? file, int? redirectYear, int? redirectMonth)
        {
            if (!User.HasModulePermission(SystemModules.Cafeteria))
                return RedirectToAction("AccessDenied", "Account");

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen geçerli bir Excel dosyası (.xlsx veya .xls) seçiniz.";
                return RedirectToAction(nameof(CafeteriaMenu), new { year = redirectYear, month = redirectMonth });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".xlsm")
            {
                TempData["ErrorMessage"] = "Yalnızca Excel (.xlsx, .xls veya .xlsm) formatındaki dosyaları yükleyebilirsiniz.";
                return RedirectToAction(nameof(Index), new { year = redirectYear, month = redirectMonth });
            }

            var mimeType = file.ContentType;
            if (mimeType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" && mimeType != "application/vnd.ms-excel")
            {
                TempData["ErrorMessage"] = "Geğersiz dosya formatı. Lütfen gerçek bir Excel dosyası yükleyin.";
                return RedirectToAction(nameof(Index), new { year = redirectYear, month = redirectMonth });
            }

            using var stream = file.OpenReadStream();
            var (successCount, errors) = await _excelService.ImportCafeteriaMenuFromExcelAsync(stream);

            if (successCount > 0)
            {
                LogAudit("Yemek Listesi Yükleme", "CafeteriaMenu", null, $"{successCount} adet günlük menü Excel'den aktarıldı.");
                TempData["SuccessMessage"] = $"{successCount} günlük yemek listesi başarıyla yüklendi ve takvime işlendi.";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("<br/>", errors.Take(5));
            }

            return RedirectToAction(nameof(CafeteriaMenu), new { year = redirectYear, month = redirectMonth });
        }

        [HttpGet]
        public IActionResult DownloadCafeteriaTemplate(int? year, int? month)
        {
            if (!User.HasModulePermission(SystemModules.Cafeteria))
                return RedirectToAction("AccessDenied", "Account");

            var targetYear = year ?? DateTime.Today.Year;
            var targetMonth = month ?? DateTime.Today.Month;

            var bytes = _excelService.GenerateCafeteriaTemplate(targetYear, targetMonth);
            var trCulture = new System.Globalization.CultureInfo("tr-TR");
            var monthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM_yyyy", trCulture);
            var fileName = $"Yemek_Listesi_Sablonu_{monthName}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCafeteriaDay(int id, int? redirectYear, int? redirectMonth)
        {
            if (!User.HasModulePermission(SystemModules.Cafeteria))
                return RedirectToAction("AccessDenied", "Account");

            var menu = await _context.CafeteriaMenus.FindAsync(id);
            if (menu != null)
            {
                _context.CafeteriaMenus.Remove(menu);
                await _context.SaveChangesAsync();
                LogAudit("Menü Günü Silme", "CafeteriaMenu", id.ToString(), $"{menu.Date:dd.MM.yyyy} tarihli menü silindi.");
                TempData["SuccessMessage"] = $"{menu.Date:dd.MM.yyyy} tarihli menü kaydı silindi.";
            }

            return RedirectToAction(nameof(CafeteriaMenu), new { year = redirectYear, month = redirectMonth });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearMonthMenu(int year, int month)
        {
            if (!User.HasModulePermission(SystemModules.Cafeteria))
                return RedirectToAction("AccessDenied", "Account");

            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var monthMenus = await _context.CafeteriaMenus
                .Where(m => m.Date >= startOfMonth && m.Date <= endOfMonth)
                .ToListAsync();

            if (monthMenus.Any())
            {
                _context.CafeteriaMenus.RemoveRange(monthMenus);
                await _context.SaveChangesAsync();
                LogAudit("Aylık Menü Temizleme", "CafeteriaMenu", $"{year}-{month}", $"{year}/{month} ayına ait {monthMenus.Count} adet menü kaydı silindi.");
                TempData["SuccessMessage"] = $"{year}/{month} dönemine ait tüm menü kayıtları temizlendi.";
            }

            return RedirectToAction(nameof(CafeteriaMenu), new { year, month });
        }


        // ==========================================
        
    }
}

